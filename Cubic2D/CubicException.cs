using System;
using System.Runtime.Serialization;

namespace Cubic2D;

public class CubicException : Exception
{
    public CubicException() { }
    protected CubicException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public CubicException(string? message) : base(message) { }
    public CubicException(string? message, Exception? innerException) : base(message, innerException) { }
}