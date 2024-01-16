using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using ApiApplication.Infrustructure.ProvidedApi;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Commands
{
    public static class CreateShowtimeCommand
    {
        public sealed class Request : IRequest<Showtime>
        {
            public Request(Showtime showtime) => Showtime = showtime;

            public Showtime Showtime { get; }
        }

        public sealed class Handler : IRequestHandler<Request, Showtime>
        {
            private readonly IProvidedApi _api;
            private readonly IShowtimesRepository _repository;

            public Handler(IProvidedApi api, IShowtimesRepository repository) => (_api, _repository) = (api, repository);

            public async Task<Showtime> Handle(Request request, CancellationToken cancellationToken)
            {
                var showtime = request.Showtime;

                var movies = await _api.GetAll(cancellationToken);

                var movie = movies.FirstOrDefault(s => s.Id == showtime.Movie)
                    ?? throw new ApplicationException("Movie not found");

                //var auditorium = _repository.GetAsync(showtime.Auditorium, cancellationToken);
                //auditorium

                // save Showtime
                var entity = new ShowtimeEntity
                {
                    SessionDate = showtime.Session,
                    Movie = new MovieEntity
                    {
                        Title = movie.Title,
                        ImdbId = movie.Id,
                        //todo: yes, parse is unsafe here
                        //todo: any DateTime conventions should be here (UTC or...)
                        ReleaseDate = new DateTime(int.Parse(movie.Year), 01, 01),
                        Stars = movie.Crew
                    },
                    AuditoriumId = showtime.Auditorium
                };

                entity = await _repository.CreateShowtime(entity, cancellationToken);

                showtime.Id = entity.Id;
                showtime.Movie = entity.Movie.Title;
                return showtime;
            }
        }
    }
}
