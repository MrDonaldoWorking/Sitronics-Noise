using System;
public class FileCorruptedException : System.IO.IOException
{
    public FileCorruptedException(string message) : base(message)
    { }
}
