using System;

namespace ApiApplication.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        { 
        }
    }
}
