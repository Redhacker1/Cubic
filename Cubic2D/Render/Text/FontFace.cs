using System;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;

namespace Cubic2D.Render.Text;

internal struct FontFace : IDisposable
{
    public IntPtr NativePtr;
    
    public FontFace(string fontPath)
    {
        if (FT_New_Face(FontHelper.FreeType.Native, fontPath, 0, out NativePtr) != FT_Error.FT_Err_Ok)
            throw new CubicException("Font could not be loaded!");
    }

    public void Dispose()
    {
        FT_Done_Face(NativePtr);
    }
}