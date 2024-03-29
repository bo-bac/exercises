﻿namespace WebApi.Middlewares.RequestThrottling.Internals;

internal class LimitedConcurrentQueue
{
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

    private readonly int _maxQueueLength;
    private readonly DropMode _dropMode;
    private readonly int _maxTimeInQueue;

    private readonly LinkedList<TaskCompletionSource<bool>> _queue = new();

    private static readonly Task<bool> _enqueueFailedTask = Task.FromResult(false);

    public LimitedConcurrentQueue(int maxQueueLength, DropMode dropMode, int maxTimeInQueue)
    {
        _maxQueueLength = maxQueueLength;
        _dropMode = dropMode;
        _maxTimeInQueue = maxTimeInQueue;
    }

    public enum DropMode
    {
        Tail = RequestThrottlingLimitExceededPolicy.UseQueueDropTail,
        Head = RequestThrottlingLimitExceededPolicy.UseQueueDropHead
    }

    public async Task<bool> EnqueueAsync(CancellationToken requestAbortedCancellationToken)
    {
        Task<bool> enqueueTask = _enqueueFailedTask;

        if (_maxQueueLength > 0)
        {
            CancellationToken enqueueCancellationToken = GetEnqueueCancellationToken(requestAbortedCancellationToken);

            try
            {
                await _queueSemaphore.WaitAsync(enqueueCancellationToken);
                try
                {
                    if (_queue.Count < _maxQueueLength)
                    {
                        enqueueTask = InternalEnqueueAsync(enqueueCancellationToken);
                    }
                    else if (_dropMode == DropMode.Head)
                    {
                        InternalDequeue(false);

                        enqueueTask = InternalEnqueueAsync(enqueueCancellationToken);
                    }
                }
                finally
                {
                    _queueSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            { }
        }

        return await enqueueTask;
    }

    public async Task<bool> DequeueAsync()
    {
        bool dequeued = false;

        await _queueSemaphore.WaitAsync();
        try
        {
            if (_queue.Count > 0)
            {
                InternalDequeue(true);
                dequeued = true;
            }
        }
        finally
        {
            _queueSemaphore.Release();
        }

        return dequeued;
    }

    private Task<bool> InternalEnqueueAsync(CancellationToken enqueueCancellationToken)
    {
        Task<bool> enqueueTask = _enqueueFailedTask;

        if (!enqueueCancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<bool> enqueueTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            enqueueCancellationToken.Register(CancelEnqueue, enqueueTaskCompletionSource);

            _queue.AddLast(enqueueTaskCompletionSource);
            enqueueTask = enqueueTaskCompletionSource.Task;
        }

        return enqueueTask;
    }

    private CancellationToken GetEnqueueCancellationToken(CancellationToken requestAbortedCancellationToken)
    {
        CancellationToken enqueueCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
            requestAbortedCancellationToken,
            GetTimeoutToken()
        ).Token;

        return enqueueCancellationToken;
    }

    private CancellationToken GetTimeoutToken()
    {
        CancellationToken timeoutToken = CancellationToken.None;

        if (_maxTimeInQueue != RequestThrottlingOptions.UNLIMITED)
        {
            CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();

            timeoutToken = timeoutTokenSource.Token;

            timeoutTokenSource.CancelAfter(_maxTimeInQueue);
        }

        return timeoutToken;
    }

    private void CancelEnqueue(object? state)
    {
        bool removed = false;

        TaskCompletionSource<bool> enqueueTaskCompletionSource = (TaskCompletionSource<bool>)state!;

        // This is blocking, but it looks like this callback can't be asynchronous.
        _queueSemaphore.Wait();
        try
        {
            removed = _queue.Remove(enqueueTaskCompletionSource);
        }
        finally
        {
            _queueSemaphore.Release();
        }

        if (removed)
        {
            enqueueTaskCompletionSource.SetResult(false);
        }
    }

    private void InternalDequeue(bool result)
    {
        TaskCompletionSource<bool>? enqueueTaskCompletionSource = _queue.First?.Value;
        if (enqueueTaskCompletionSource is not null)
        {
            _queue.RemoveFirst();
            enqueueTaskCompletionSource.SetResult(result);
        }
    }
}
