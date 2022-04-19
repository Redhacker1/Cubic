using System;
using System.Drawing;
using Cubic.Scenes;
using OpenTK.Graphics.OpenGL4;

namespace Cubic.Render;

public abstract class Texture : IDisposable
{
    internal int Handle;
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
        GL.DeleteTexture(Handle);
#if DEBUG
        Console.WriteLine("Texture disposed");
#endif
    }
}