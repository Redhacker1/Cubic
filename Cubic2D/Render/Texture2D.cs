using System;
using System.Drawing;
using System.IO;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using StbImageSharp;
using Veldrid;

namespace Cubic2D.Render;

public class Texture2D : IDisposable
{
    internal Texture Texture;

    public readonly Size Size;

    public Texture2D(CubicGame game, string path)
    {
        using (Stream stream = File.OpenRead(path))
        {
            ImageResult result = ImageResult.FromStream(stream);
            
            // Convert to RGBA format image if required, as Veldrid doesn't support RGB format images for some reason.
            byte[] data;
            if (result.Comp == ColorComponents.RedGreenBlue)
            {
                data = new byte[result.Width * result.Height * 4];
                int dataI = 0;
                byte[] rData = result.Data;
                for (int i = 0; i < rData.Length; i += 3)
                {
                    data[dataI++] = rData[i];
                    data[dataI++] = rData[i + 1];
                    data[dataI++] = rData[i + 2];
                    data[dataI++] = 255;
                }
            }
            else
                data = result.Data;
            
            GraphicsDevice device = game.Graphics.GraphicsDevice;
            Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) result.Width,
                (uint) result.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            device.UpdateTexture(Texture, data, 0, 0, 0, (uint) result.Width, (uint) result.Height, 1, 0, 0);
            Size = new Size(result.Width, result.Height);
        }
        
        // Add this to the list of created resources the scene has so it can be disposed later.
        SceneManager.Active.CreatedResources.Add(this);
    }

    public Texture2D(CubicGame game, int width, int height, byte[] data)
    {
        GraphicsDevice device = game.Graphics.GraphicsDevice;
        Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) width,
            (uint) height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        device.UpdateTexture(Texture, data, 0, 0, 0, (uint) width, (uint) height, 1, 0, 0);
        Size = new Size(width, height);
    }

    public Texture2D(CubicGame game, int width, int height)
    {
        GraphicsDevice device = game.Graphics.GraphicsDevice;
        Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) width,
            (uint) height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        Size = new Size(width, height);
    }

    public void SetData(Graphics graphics, IntPtr data, uint size, uint x, uint y, uint width, uint height)
    {
        graphics.GraphicsDevice.UpdateTexture(Texture, data, size, x, y, 0, width, height, 1, 0, 0);
    }
    
    public void Dispose()
    {
        Texture.Dispose();
#if DEBUG
        Console.WriteLine("Texture disposed");
#endif
    }

    public static Texture2D Blank { get; internal set; }
}