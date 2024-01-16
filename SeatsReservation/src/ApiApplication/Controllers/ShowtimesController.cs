using ApiApplication.Commands;
using ApiApplication.Dto;
using ApiApplication.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;

namespace ApiApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ShowtimesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ShowtimesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Showtime>))]
        public async Task<ActionResult> GetAllShowtimes()
        {
            var responce = await _mediator.Send(new GetShowtimesQuery.Request());

            return Ok(responce);
        }

        [HttpGet("{id:int}", Name = "GetShowtimeById")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(200, Type = typeof(Showtime))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetShowtimeById(int id)
        {
            var responce = await _mediator.Send(new GetShowtimeByIdQuery.Request(id));

            if (responce == null)
            {
                return NotFound();
            }

            return Ok(responce);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateShowtime([FromBody] Showtime showtime)
        {
            var responce = await _mediator.Send(new CreateShowtimeCommand.Request(showtime));

            return CreatedAtRoute(nameof(GetShowtimeById), new { id = responce.Id }, responce);
        }

        [HttpGet("{id:int}/tickets/{tid:guid}", Name = nameof(GetTicketById))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(200, Type = typeof(TicketInfo))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTicketById(int id, Guid tid)
        {
            var responce = await _mediator.Send(new GetTicketByIdQuery.Request(id, tid));

            if (responce == null)
            {
                return NotFound();
            }

            return Ok(responce);
        }
    }
}
