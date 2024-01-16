using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Commands
{
    public static class ReserveTicketCommand
    {
        public sealed class Request : IRequest<TicketInfo>
        {
            public int Showtime { get; set; }

            public ICollection<Seat> Seats { get; set; }
        }

        public sealed class Handler : IRequestHandler<Request, TicketInfo>
        {
            private const int RESERVATION_EXPIRATION_TIMEOUT = 10;

            private readonly IDistributedCache _cache;
            private readonly IShowtimesRepository _showtimesRepository;

            public Handler(
                IDistributedCache cache,
                IShowtimesRepository showtimesRepository)
            {
                _cache = cache;
                _showtimesRepository = showtimesRepository;
            }

            public async Task<TicketInfo> Handle(Request request, CancellationToken cancellationToken)
            {
                var showtime = await _showtimesRepository.GetWithMoviesByIdAsync(request.Showtime, cancellationToken);

                // save Reservation
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(RESERVATION_EXPIRATION_TIMEOUT)
                };

                var reservation = new TicketInfo
                {
                    Id = Guid.NewGuid(),
                    ShowtimeId = showtime.Id,
                    Auditorium = showtime.AuditoriumId,
                    Movie = showtime.Movie.Title,
                    Seats = request.Seats
                };

                var cacheData = JsonSerializer.SerializeToUtf8Bytes<TicketInfo>(reservation);
                await _cache.SetAsync(reservation.Id.ToString(), cacheData, options, cancellationToken);

                await Task.WhenAll(
                    from seat in request.Seats
                        let k = $"{request.Showtime}:{showtime.AuditoriumId}:{seat.Row}:{seat.Number}"
                    select _cache.SetStringAsync(k, string.Empty, options, cancellationToken));

                return reservation;
            }
        }
    }
}
