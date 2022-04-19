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

    public readonly ColorSpace ColorSpace;
    
    public Bitmap(string path)
    {
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path));
        Data = result.Data;
        Size = new Size(result.Width, result.Height);
        ColorSpace = result.Comp switch
        {
            ColorComponents.RedGreenBlue => ColorSpace.RGB,
            ColorComponents.RedGreenBlueAlpha => ColorSpace.RGBA,
            _ => ColorSpace.Unsupported
        };
    }

    public Bitmap(int width, int height, byte[] data, ColorSpace colorSpace = ColorSpace.RGBA)
    {
        Data = data;
        Size = new Size(width, height);
        ColorSpace = colorSpace;
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

    public static Bitmap ConvertToColorSpace(Bitmap bitmap, ColorSpace colorSpace)
    {
        if (colorSpace == bitmap.ColorSpace)
            return bitmap;

        switch (colorSpace)
        {
            case ColorSpace.Unsupported:
                throw new CubicException("Not supported.");
                break;
            case ColorSpace.RGB:
                byte[] dataRGB = new byte[bitmap.Size.Width * bitmap.Size.Height * 3];
                for (int x = 0; x < bitmap.Size.Width; x++)
                {
                    for (int y = 0; y < bitmap.Size.Height; y++)
                    {
                        dataRGB[(y * bitmap.Size.Width + x) * 3] = bitmap.Data[(y * bitmap.Size.Width + x) * 4];
                        dataRGB[(y * bitmap.Size.Width + x) * 3 + 1] = bitmap.Data[(y * bitmap.Size.Width + x) * 4 + 1];
                        dataRGB[(y * bitmap.Size.Width + x) * 3 + 2] = bitmap.Data[(y * bitmap.Size.Width + x) * 4 + 2];
                    }
                }

                Bitmap newBitmapRGB = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, dataRGB, ColorSpace.RGB);
                return newBitmapRGB;
            case ColorSpace.RGBA:
                byte[] dataRGBA = new byte[bitmap.Size.Width * bitmap.Size.Height * 4];
                for (int x = 0; x < bitmap.Size.Width; x++)
                {
                    for (int y = 0; y < bitmap.Size.Height; y++)
                    {
                        dataRGBA[(y * bitmap.Size.Width + x) * 4] = bitmap.Data[(y * bitmap.Size.Width + x) * 3];
                        dataRGBA[(y * bitmap.Size.Width + x) * 4 + 1] = bitmap.Data[(y * bitmap.Size.Width + x) * 3 + 1];
                        dataRGBA[(y * bitmap.Size.Width + x) * 4 + 2] = bitmap.Data[(y * bitmap.Size.Width + x) * 3 + 2];
                        dataRGBA[(y * bitmap.Size.Width + x) * 4 + 3] = 255;
                    }
                }

                Bitmap newBitmapRGBA = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, dataRGBA, ColorSpace.RGBA);
                return newBitmapRGBA;
            default:
                throw new ArgumentOutOfRangeException(nameof(colorSpace), colorSpace, null);
        }
    }
}

public enum ColorSpace
{
    Unsupported,
    RGB,
    RGBA
}