using ApiApplication.Dto;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Queries
{
    public static class GetReservationByIdQuery
    {
        public sealed class Request : IRequest<TicketInfo>
        {
            public Request(Guid id) => Id = id;

            public Guid Id { get; }
        }

        public sealed class Handler : IRequestHandler<Request, TicketInfo>
        {
            private readonly IDistributedCache _cache;

            public Handler(IDistributedCache cache) => _cache = cache;

            public async Task<TicketInfo> Handle(Request request, CancellationToken cancellationToken)
            {
                // try read from cache
                var cacheData = await _cache.GetAsync(request.Id.ToString(), cancellationToken);
                if (cacheData == null)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<TicketInfo>(cacheData);
            }
        }
    }
}
