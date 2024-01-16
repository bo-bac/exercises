using ApiApplication.Database.Repositories;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Commands
{
    //todo: yes it must be a good idea to extract complicated rules in separate validators
    public sealed class ReserveTicketCommandValidator : AbstractValidator<ReserveTicketCommand.Request>
    {
        private readonly IDistributedCache _cache;
        private readonly IShowtimesRepository _showtimesRepository;
        private readonly ITicketsRepository _ticketsRepository;

        public ReserveTicketCommandValidator(
            IDistributedCache cache, 
            IShowtimesRepository showtimesRepository,
            ITicketsRepository ticketsRepository)
        {
            _cache = cache;
            _showtimesRepository = showtimesRepository;
            _ticketsRepository = ticketsRepository;
            
            RuleFor(x => new { x.Showtime, x.Seats })
                .NotNull()
                .CustomAsync(async (reservation, context, cancellation) =>
                {
                    var showtime = await _showtimesRepository.GetWithTicketsByIdAsync(reservation.Showtime, cancellation);
                    if (showtime is null)
                    {
                        context.AddFailure("Mentioned Showtime does'n exist");
                    }

                    //if (showtime.SessionDate.AddMinutes(-5) <= DateTime.Now)
                    //{
                    //    context.AddFailure("Tickets saling stopes 5 minutes before session");
                    //}   

                    //todo: check if seats in auditorium dimension

                    //check seats was not reserved
                    var reserved = await Task.WhenAll(
                            from seat in reservation.Seats
                            let k = $"{showtime.Id}:{showtime.AuditoriumId}:{seat.Row}:{seat.Number}"
                            select _cache.GetStringAsync(k, cancellation));

                    if (reserved.Where(r => r != null).Any())
                    {
                        context.AddFailure("Mentioned seat/s already reserved.");
                    }

                    //check seats was not sold
                    var sold = (await _ticketsRepository.GetEnrichedAsync(reservation.Showtime, cancellation))
                        .SelectMany(s => s.Seats)
                        .Where(s => reservation.Seats.Any(r => r.Row == s.Row && r.Number == s.SeatNumber));

                    if (sold.Any())
                    {
                        context.AddFailure("Mentioned seat/s already sold.");
                    }
                });

            RuleFor(x => x.Seats)
                .NotEmpty()
                .Custom((seats, context) => {
                    if (seats.Count > 1)
                    {
                        if (seats.GroupBy(s => new { s.Row, s.Number }).Count() != seats.Count)
                        {
                            context.AddFailure("Seats must not contain duplicates");
                        }

                        //let's consider 'contiguous' in the same row
                        if (seats.GroupBy(s => s.Row).Count() != 1)
                        {
                            context.AddFailure("All seats must be in the same row");
                        }

                        if (!seats.All(s => seats.Any(ss => ss.Number - 1 == s.Number || ss.Number + 1 == s.Number)))
                        {
                            context.AddFailure("All seats must be contiguous");
                        }
                    }
                });
        }
    }
}
