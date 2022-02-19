using System;
using System.IO;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace Cubic2D.Render;

public class Shader : IDisposable
{
    internal int Handle;
    
    public Shader(string vertex, string fragment, ShaderLoadType loadType = ShaderLoadType.String)
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

        switch (loadType)
        {
            case ShaderLoadType.File:
                GL.ShaderSource(vertexShader, File.ReadAllText(vertex));
                GL.ShaderSource(fragmentShader, File.ReadAllText(fragment));
                break;
            case ShaderLoadType.String:
                GL.ShaderSource(vertexShader, vertex);
                GL.ShaderSource(fragmentShader, fragment);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null);
        }
        
        Compile(vertexShader);
        Compile(fragmentShader);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        Link(Handle);
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Set<T>(string uniformName, T value, bool transpose = true)
    {
        int location = GL.GetUniformLocation(Handle, uniformName);

        switch (value)
        {
            case int iValue:
                GL.Uniform1(location, iValue);
                break;
            case float fValue:
                GL.Uniform1(location, fValue);
                break;
            case Vector2 v2Value:
                GL.Uniform2(location, 1, ref v2Value.X);
                break;
            case Vector3 v3Value:
                GL.Uniform3(location, 1, ref v3Value.X);
                break;
            case Vector4 v4Value:
                GL.Uniform4(location, 1, ref v4Value.X);
                break;
            case Matrix4x4 m4Value:
                GL.UniformMatrix4(location, 1, transpose, ref m4Value.M11);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid type");
        }
    }

    private static void Compile(int shader)
    {
        GL.CompileShader(shader);
        
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status != 1)
            throw new CubicException($"Shader '{shader}' failed to compile: {GL.GetShaderInfoLog(shader)}");
    }

    private static void Link(int program)
    {
        GL.LinkProgram(program);
        
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status != 1)
            throw new CubicException($"Program '{program}' failed to link: {GL.GetProgramInfoLog(program)}");
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
        
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