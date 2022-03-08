using System;
using System.Drawing;
using Cubic2D.Scenes;
using OpenTK.Graphics.OpenGL4;

namespace Cubic2D.Render;

public abstract class Texture : IDisposable
{
    internal int Handle;
    public Size Size { get; protected set; }

    public Texture()
    {
        SceneManager.Active.CreatedResources.Add(this);
    }
    
    public void Dispose()
    {
        GL.DeleteTexture(Handle);
#if DEBUG
        Console.WriteLine("Texture disposed");
#endif
    }
}