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
    public class ReservationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReservationsController(IMediator mediator) => _mediator = mediator;

        [HttpGet("{id:guid}", Name = nameof(GetReservationById))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(200, Type = typeof(TicketInfo))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetReservationById(Guid id)
        {
            var responce = await _mediator.Send(new GetReservationByIdQuery.Request(id));

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
        public async Task<IActionResult> ReserveTicket([FromBody] ReserveTicketCommand.Request request)
        {
            var responce = await _mediator.Send(request);

            return CreatedAtRoute(nameof(GetReservationById), new { id = responce.Id }, responce);
        }

        [HttpPost("{id:guid}/confirm")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BuyTicket(Guid id)
        {
            var responce = await _mediator.Send(new ConfirmReservationCommand.Request(id));

            return CreatedAtRoute(nameof(ShowtimesController.GetTicketById), new { id = responce.ShowtimeId, tid = responce.Id }, responce);
        }
    }
}
