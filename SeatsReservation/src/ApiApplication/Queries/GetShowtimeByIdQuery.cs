using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Queries
{
    public static class GetShowtimeByIdQuery
    {
        public sealed class Request : IRequest<Showtime>
        {
            public Request(int id) => Id = id;

            public int Id { get; }
        }

        public sealed class Handler : IRequestHandler<Request, Showtime>
        {
            private readonly IShowtimesRepository _repository;

            public Handler(IShowtimesRepository repository) => _repository = repository;

            public async Task<Showtime> Handle(Request request, CancellationToken cancellationToken)
            {
                var entity = await _repository.GetWithMoviesByIdAsync(request.Id, cancellationToken);

                return (entity != null)
                    ? new Showtime
                    {
                        Id = entity.Id,
                        Auditorium = entity.AuditoriumId,
                        Session = entity.SessionDate,
                        Movie = entity.Movie.Title
                    }
                    : null;
            }
        }
    }
}
