using System;
using System.Drawing;
using System.IO;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using StbImageSharp;
using Veldrid;

namespace Cubic2D.Render;

public class Texture2D : UnmanagedResource
{
    internal Texture Texture;

    public readonly Size Size;

    public Texture2D(string path)
    {
        using (Stream stream = File.OpenRead(path))
        {
            ImageResult result = ImageResult.FromStream(stream);
            
            GraphicsDevice device = CubicGame.Current.Graphics.GraphicsDevice;
            Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) result.Width,
                (uint) result.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            device.UpdateTexture(Texture, result.Data, 0, 0, 0, (uint) result.Width, (uint) result.Height, 1, 0, 0);
            Size = new Size(result.Width, result.Height);
        }
        
        // Add this to the list of created resources the scene has so it can be disposed later.
        SceneManager.Active.CreatedResources.Add(this);
    }

    public Texture2D(int width, int height, byte[] data)
    {
        GraphicsDevice device = CubicGame.Current.Graphics.GraphicsDevice;
        Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) width,
            (uint) height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        device.UpdateTexture(Texture, data, 0, 0, 0, (uint) width, (uint) height, 1, 0, 0);
        Size = new Size(width, height);
    }
    
    internal override void Dispose()
    {
        Texture.Dispose();
#if DEBUG
        Console.WriteLine("Texture disposed");
#endif
    }

    public static readonly Texture2D Blank = new Texture2D(1, 1, new byte[] {255, 255, 255, 255});
}