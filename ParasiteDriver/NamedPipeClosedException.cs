using System;

namespace ParasiteDriver
{
    public class NamedPipeClosedException : Exception
    {
        public NamedPipeClosedException() { }

        public NamedPipeClosedException(string message) : base(message) { }

        public NamedPipeClosedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
