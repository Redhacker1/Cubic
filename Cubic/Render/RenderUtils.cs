using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;

namespace Cubic.Render;

internal static class RenderUtils
{
    public static unsafe void VertexAttribs(Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        uint location = 0;
        int offset = 0;
        uint totalSizeInBytes = 0;
        List<int> sizes = new List<int>();
        foreach (FieldInfo info in fields)
        {
            int size = Marshal.SizeOf(info.FieldType);
            sizes.Add(size);
            totalSizeInBytes += (uint) size;
        }

        foreach (int size in sizes)
        {
            Gl.EnableVertexAttribArray(location);
            Gl.VertexAttribPointer(location, size / 4, VertexAttribPointerType.Float, false, totalSizeInBytes, (void*) offset);
            offset += size;
            location += 1;
        }
    }
}