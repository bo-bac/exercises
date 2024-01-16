using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using MediatR;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiApplication.Queries
{
    public static class GetTicketByIdQuery
    {
        public sealed class Request : IRequest<TicketInfo>
        {
            public Request(int showtimeId, Guid ticketId) => (ShowtimeId, TicketId) = (showtimeId, ticketId);

            public int ShowtimeId { get; }
            public Guid TicketId { get; }
        }

        public sealed class Handler : IRequestHandler<Request, TicketInfo>
        {
            private readonly ITicketsRepository _repository;

            public Handler(ITicketsRepository repository) => _repository = repository;

            public async Task<TicketInfo> Handle(Request request, CancellationToken cancellationToken)
            {
                var entity = (await _repository.GetEnrichedAsync(request.ShowtimeId, cancellationToken))
                    ?.FirstOrDefault(t => t.Id == request.TicketId);

                return (entity != null)
                    ? new TicketInfo
                    {
                        Id = entity.Id,
                        ShowtimeId = entity.Showtime.Id,
                        Auditorium = entity.Showtime.AuditoriumId,
                        //Movie = entity.Showtime.Movie.Title,
                        Paid = entity.Paid,
                        Seats = entity.Seats
                                    .Select(s => new Seat 
                                    { 
                                        Row = s.Row,
                                        Number = s.SeatNumber 
                                    })
                                    .ToArray()
                    }
                    : null;
            }
        }
    }
}
