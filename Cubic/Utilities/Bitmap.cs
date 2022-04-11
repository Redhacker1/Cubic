using System;
using System.Drawing;
using System.IO;
using Cubic.Render;
using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace Cubic.Utilities;

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
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path));
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

    public void Save(string path)
    {
        using Stream stream = File.OpenWrite(path);
        ImageWriter writer = new ImageWriter();
        if (path.EndsWith(".png"))
            writer.WritePng(Data, Size.Width, Size.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
                stream);
        else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            writer.WriteJpg(Data, Size.Width, Size.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream,
                100);
        else if (path.EndsWith(".bmp"))
            writer.WriteBmp(Data, Size.Width, Size.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
                stream);
        else
            throw new CubicException("Invalid file type.");
    }
}