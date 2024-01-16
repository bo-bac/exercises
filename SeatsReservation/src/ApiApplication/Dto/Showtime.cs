using System;

namespace ApiApplication.Dto
{
    public class Showtime
    {
        public int Id { get; set; }
        public DateTime Session { get; set; }
        public int Auditorium { get; set; }
        public string Movie { get; set; }
    }
}
