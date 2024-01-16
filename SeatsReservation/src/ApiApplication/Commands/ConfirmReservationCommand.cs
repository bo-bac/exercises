using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using ApiApplication.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Commands
{
    public static class ConfirmReservationCommand
    {
        public sealed class Request : IRequest<TicketInfo>
        {
            public Request(Guid id) => Id = id;

            public Guid Id { get; }
        }

        public sealed class Handler : IRequestHandler<Request, TicketInfo>
        {
            private readonly IDistributedCache _cache;
            private readonly ITicketsRepository _ticketsRepository;
            private readonly IShowtimesRepository _showtimesRepository;

            public Handler(
                IDistributedCache cache,
                ITicketsRepository ticketsRepository,
                IShowtimesRepository showtimesRepository)
            {
                _cache = cache;
                _ticketsRepository = ticketsRepository;
                _showtimesRepository = showtimesRepository;
            }

            public async Task<TicketInfo> Handle(Request request, CancellationToken cancellationToken)
            {
                // try read from cache
                var cacheData = (await _cache.GetAsync(request.Id.ToString(), cancellationToken))
                    ?? throw new NotFoundException("Raservation has expired");

                var reservation = JsonSerializer.Deserialize<TicketInfo>(cacheData);
                var seats = reservation.Seats.Select(s => new SeatEntity
                {
                    AuditoriumId = reservation.Auditorium,
                    Row = s.Row,
                    SeatNumber = s.Number
                }).ToArray();
                var showtime = await _showtimesRepository.GetWithMoviesByIdAsync(reservation.ShowtimeId, cancellationToken);

                // confirm Reservation
                var entity = await _ticketsRepository.CreateAsync(showtime, seats, cancellationToken);
                entity = await _ticketsRepository.ConfirmPaymentAsync(entity, cancellationToken);                

                // clear cache
                await _cache.RemoveAsync(request.Id.ToString(), cancellationToken);

                await Task.WhenAll(
                    from seat in reservation.Seats
                    let k = $"{showtime.Id}:{showtime.AuditoriumId}:{seat.Row}:{seat.Number}"
                    select _cache.RemoveAsync(k, cancellationToken));

                reservation.Id = entity.Id;
                reservation.Paid = entity.Paid;
                return reservation;
            }
        }
    }
}
