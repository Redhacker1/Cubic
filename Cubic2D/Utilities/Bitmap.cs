using System.Drawing;
using System.IO;
using StbImageSharp;
using Cubic2D.Render;

namespace Cubic2D.Utilities;

/// <summary>
/// A bitmap stores a byte array of raw image data, and its size.
/// Unlike a <see cref="Texture2D"/>, this does not store anything on the GPU or its video memory.
/// While using a bitmap is not necessary, there are times you may want to store image data on the main memory instead
/// of the video memory.
/// </summary>
public class Bitmap
{
    public readonly byte[] Data;
    public readonly Size Size;

    internal readonly ColorComponents ColorComponents;
    
    public Bitmap(string path)
    {
        ImageResult result = ImageResult.FromStream(File.OpenRead(path));
        Data = result.Data;
        Size = new Size(result.Width, result.Height);
        ColorComponents = result.Comp;
    }

    public Bitmap(int width, int height, byte[] data)
    {
        Data = data;
        Size = new Size(width, height);
        ColorComponents = ColorComponents.RedGreenBlueAlpha;
    }
}