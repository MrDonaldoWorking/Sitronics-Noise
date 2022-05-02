using System;
using System.Runtime.Serialization;

[Serializable]
internal class FileCorruptedException : Exception
{
    public FileCorruptedException()
    {
    }

    public FileCorruptedException(string message) : base(message)
    {
    }

    public FileCorruptedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected FileCorruptedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}