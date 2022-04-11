using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Cubic.Render;

internal static class RenderUtils
{
    public static void VertexAttribs(Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        int location = 0;
        int offset = 0;
        int totalSizeInBytes = 0;
        List<int> sizes = new List<int>();
        foreach (FieldInfo info in fields)
        {
            int size = Marshal.SizeOf(info.FieldType);
            sizes.Add(size);
            totalSizeInBytes += size;
        }

        foreach (int size in sizes)
        {
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, size / 4, VertexAttribPointerType.Float, false, totalSizeInBytes, offset);
            offset += size;
            location += 1;
        }
    }
}