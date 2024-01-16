using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Dto;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Queries
{
    //todo: yes it might not look ortodoxa (one class == one file) but for challenge simplisity sake why not
    public static class GetShowtimesQuery
    {
        public sealed class Request : IRequest<IEnumerable<Showtime>>
        {
        }

        public sealed class Handler : IRequestHandler<Request, IEnumerable<Showtime>>
        {
            private readonly IShowtimesRepository _repository;

            public Handler(IShowtimesRepository repository) => _repository = repository;

            public async Task<IEnumerable<Showtime>> Handle(Request request, CancellationToken cancellationToken) =>
                (await _repository.GetAllAsync(null, cancellationToken)).Select(entity => new Showtime
                {
                    Id = entity.Id,
                    Auditorium = entity.AuditoriumId,
                    Session = entity.SessionDate,
                    Movie = entity.Movie.Title
                });
        }
    }
}
