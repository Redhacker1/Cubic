using System;
using Cubic.Render;

namespace Cubic.Windowing;

public class ContextCreationException : Exception
{
    public ContextCreationException(GraphicsApi api) : base($"The requested context ({api}) could not be created on this system. This most likely means the requested context is unsupported by the graphics driver.") { }
}