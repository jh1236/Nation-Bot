using System.Runtime.Serialization;

namespace DiscordBot
{
    [Serializable]
    internal class OutOfMovesException : Exception
    {
        public OutOfMovesException()
        {
        }

        public OutOfMovesException(string? message) : base(message)
        {
        }

        public OutOfMovesException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected OutOfMovesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}