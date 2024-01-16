using FluentValidation;
using System;

namespace ApiApplication.Commands
{
    public sealed class CreateShowtimeCommandValidator : AbstractValidator<CreateShowtimeCommand.Request>
    {
        public CreateShowtimeCommandValidator()
        {
            RuleFor(x => x.Showtime).NotNull();
            RuleFor(x => x.Showtime.Id).Equal(0);
            RuleFor(x => x.Showtime.Auditorium).InclusiveBetween(1,3);
            RuleFor(x => x.Showtime.Movie).NotEmpty();
            RuleFor(x => x.Showtime.Session).NotEmpty().GreaterThan(DateTime.Now);
        }
    }
}
