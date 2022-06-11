using System;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;

namespace Cubic.Render.Text;

internal struct FontFace : IDisposable
{
    public IntPtr NativePtr;
    
    public FontFace(string fontPath)
    {
        if (FT_New_Face(FontHelper.FreeType.Native, fontPath, 0, out NativePtr) != FT_Error.FT_Err_Ok)
            throw new CubicException("Font could not be loaded!");
    }

    public unsafe FontFace(byte[] data)
    {
        fixed (byte* p = data)
        {
            if (FT_New_Memory_Face(FontHelper.FreeType.Native, (IntPtr) p, data.Length, 0, out NativePtr) != FT_Error.FT_Err_Ok)
                throw new CubicException("Font could not be loaded!");
        }
    }

    public void Dispose()
    {
        FT_Done_Face(NativePtr);
    }
}