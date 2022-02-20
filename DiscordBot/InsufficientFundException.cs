using System.Runtime.Serialization;

[Serializable]
internal class InsufficientFundException : Exception
{
    public InsufficientFundException()
    {
    }

    public InsufficientFundException(string? message) : base(message)
    {
    }

    public InsufficientFundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected InsufficientFundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}