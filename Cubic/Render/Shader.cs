using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using Cubic.Utilities;
using static Cubic.Render.Graphics;
using Silk.NET.OpenGL;

namespace Cubic.Render;

public class Shader : IDisposable
{
    internal uint Handle;
    
    public Shader(string vertex, string fragment, ShaderLoadType loadType = ShaderLoadType.String)
    {
        uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);

        switch (loadType)
        {
            case ShaderLoadType.File:
                Gl.ShaderSource(vertexShader, File.ReadAllText(vertex));
                Gl.ShaderSource(fragmentShader, File.ReadAllText(fragment));
                break;
            case ShaderLoadType.String:
                Gl.ShaderSource(vertexShader, vertex);
                Gl.ShaderSource(fragmentShader, fragment);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null);
        }
        
        Compile(vertexShader);
        Compile(fragmentShader);

        Handle = Gl.CreateProgram();
        Gl.AttachShader(Handle, vertexShader);
        Gl.AttachShader(Handle, fragmentShader);
        Link(Handle);
        Gl.DetachShader(Handle, vertexShader);
        Gl.DetachShader(Handle, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);
    }

    public unsafe void Set<T>(string uniformName, T value, bool transpose = true)
    {
        Gl.UseProgram(Handle);
        
        int location = Gl.GetUniformLocation(Handle, uniformName);

        switch (value)
        {
            case bool bValue:
                Gl.Uniform1(location, bValue ? 1 : 0);
                break;
            case int iValue:
                Gl.Uniform1(location, iValue);
                break;
            case float fValue:
                Gl.Uniform1(location, fValue);
                break;
            case Vector2 v2Value:
                Gl.Uniform2(location, 1, (float*) &v2Value);
                break;
            case Vector3 v3Value:
                Gl.Uniform3(location, 1, (float*) &v3Value);
                break;
            case Vector4 v4Value:
                Gl.Uniform4(location, 1, (float*) &v4Value);
                break;
            case Color cValue:
                Vector4 color = cValue.Normalize();
                Gl.Uniform4(location, 1, (float*) &color);
                break;
            case Matrix4x4 m4Value:
                Gl.UniformMatrix4(location, 1, transpose, (float*) &m4Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid type");
        }
    }

    private static void Compile(uint shader)
    {
        Gl.CompileShader(shader);
        
        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status != 1)
            throw new CubicException($"Shader '{shader}' failed to compile: {Gl.GetShaderInfoLog(shader)}");
    }

    private static void Link(uint program)
    {
        Gl.LinkProgram(program);
        
        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        if (status != 1)
            throw new CubicException($"Program '{program}' failed to link: {Gl.GetProgramInfoLog(program)}");
    }

    public void Dispose()
    {
        Gl.DeleteProgram(Handle);
        
#if DEBUG
        Console.WriteLine("Shader disposed");
#endif
    }
}

public enum ShaderLoadType
{
    File,
    String
}