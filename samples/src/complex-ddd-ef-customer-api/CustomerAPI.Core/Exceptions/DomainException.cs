using System;

namespace CustomerAPI.Core.Exceptions
{
    /// <summary>
    /// Represents a domain rule violation raised by an aggregate or domain service.
    /// </summary>
    public sealed class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
