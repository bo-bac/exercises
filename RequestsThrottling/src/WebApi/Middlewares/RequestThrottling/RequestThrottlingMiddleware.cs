using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using WebApi.Middlewares.RequestThrottling.Internals;

namespace WebApi.Middlewares.RequestThrottling;

public sealed class RequestThrottlingMiddleware
{
    private readonly Guid _id = Guid.NewGuid();
    private long _handledCount;

    private int _concurrentRequestsCount;

    private readonly RequestDelegate _next;
    private readonly RequestThrottlingOptions _options;

    private readonly LimitedConcurrentQueue? _queue;

    public RequestThrottlingMiddleware(RequestDelegate next, IOptions<RequestThrottlingOptions> options)
    {
        _next = next;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.LimitExceededPolicy != RequestThrottlingLimitExceededPolicy.Drop)
        {
            _queue = new LimitedConcurrentQueue(_options.MaxQueueLength, (LimitedConcurrentQueue.DropMode)_options.LimitExceededPolicy, _options.MaxTimeInQueue);
        }
    }

    public async Task Invoke(HttpContext context)
    {
        Debug.WriteLine($"Thread {Environment.CurrentManagedThreadId}:Middleware instance {_id}:Current concurrancy {_concurrentRequestsCount}");


        if (CheckLimitExceeded() && !await TryWaitInQueueAsync(context.RequestAborted))
        {
            if (!context.RequestAborted.IsCancellationRequested)
            {
                IHttpResponseFeature responseFeature = context.Features.Get<IHttpResponseFeature>()!;

                responseFeature.StatusCode = StatusCodes.Status429TooManyRequests;
                responseFeature.ReasonPhrase = "Too many requests. Please try again later.";
            }
        }
        else
        {
            try
            {
                await _next(context);
            }
            finally
            {
                if (await ShouldDecrementConcurrentRequestsCountAsync())
                {
                    Interlocked.Decrement(ref _concurrentRequestsCount);
                }
            }
        }


        Debug.WriteLine($"Handled by {_id}:{Interlocked.Increment(ref _handledCount)}");
    }

    private bool CheckLimitExceeded()
    {
        bool limitExceeded;

        if (_options.Limit == RequestThrottlingOptions.UNLIMITED)
        {
            limitExceeded = false;
        }
        else
        {
            int initialCount, incrementedCount;
            do
            {
                limitExceeded = true;

                initialCount = _concurrentRequestsCount;
                if (initialCount >= _options.Limit)
                {
                    break;
                }

                limitExceeded = false;
                incrementedCount = initialCount + 1;
            }
            while (initialCount != Interlocked.CompareExchange(ref _concurrentRequestsCount, incrementedCount, initialCount));
        }

        return limitExceeded;
    }

    private async Task<bool> TryWaitInQueueAsync(CancellationToken requestAbortedCancellationToken)
    {
        return _queue != null && await _queue.EnqueueAsync(requestAbortedCancellationToken);
    }

    private async Task<bool> ShouldDecrementConcurrentRequestsCountAsync()
    {
        return _options.Limit != RequestThrottlingOptions.UNLIMITED
            && (_queue == null || !await _queue.DequeueAsync());
    }
}
