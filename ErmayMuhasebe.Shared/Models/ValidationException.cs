using System;

namespace ErmayMuhasebe.Models
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}
