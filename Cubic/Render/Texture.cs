using System;
using System.Drawing;
using Cubic.Scenes;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;

namespace Cubic.Render;

public abstract class Texture : IDisposable
{
    internal uint Handle;
    public Size Size { get; protected set; }

    public Texture(bool autoDispose)
    {
        if (autoDispose)
             SceneManager.Active.CreatedResources.Add(this);
    }

    internal abstract void Bind(TextureUnit textureUnit = TextureUnit.Texture0);

    internal abstract void Unbind();
    
    public virtual void Dispose()
    {
        Gl.DeleteTexture(Handle);
#if DEBUG
        Console.WriteLine("Texture disposed");
#endif
    }
}