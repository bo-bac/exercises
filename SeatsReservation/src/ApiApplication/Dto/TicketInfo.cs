using System.Collections.Generic;
using System;

namespace ApiApplication.Dto
{
    public class TicketInfo
    {
        public Guid Id { get; set; }

        public int ShowtimeId { get; set; }

        public int Auditorium { get; set; }

        public string Movie { get; set; }

        public ICollection<Seat> Seats { get; set; }

        public bool Paid { get; set; }
    }
}
