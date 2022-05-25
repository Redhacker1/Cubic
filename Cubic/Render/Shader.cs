using System;
using System.Collections.Generic;
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

    private Dictionary<string, int> _uniformLocations;

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

        _uniformLocations = new Dictionary<string, int>();
        Gl.GetProgram(Handle, ProgramPropertyARB.ActiveUniforms, out int numUniforms);
        for (uint i = 0; i < numUniforms; i++)
        {
            string name = Gl.GetActiveUniform(Handle, i, out _, out _);
            int location = Gl.GetUniformLocation(Handle, name);
            _uniformLocations.Add(name, location);
        }
    }

    public void Set(string uniformName, bool value)
    {
        Gl.Uniform1(_uniformLocations[uniformName], value ? 1 : 0);
    }
    
    public void Set(string uniformName, int value)
    {
        Gl.Uniform1(_uniformLocations[uniformName], value);
    }
    
    public void Set(string uniformName, float value)
    {
        Gl.Uniform1(_uniformLocations[uniformName], value);
    }

    public void Set(string uniformName, Vector2 value)
    {
        Gl.Uniform2(_uniformLocations[uniformName], ref value);
    }
    
    public void Set(string uniformName, Vector3 value)
    {
        Gl.Uniform3(_uniformLocations[uniformName], ref value);
    }
    
    public void Set(string uniformName, Vector4 value)
    {
        Gl.Uniform4(_uniformLocations[uniformName], ref value);
    }

    public void Set(string uniformName, Color color)
    {
        Vector4 normalized = color.Normalize();
        Gl.Uniform4(_uniformLocations[uniformName], ref normalized);
    }

    public unsafe void Set(string uniformName, Matrix4x4 value, bool transpose = true)
    {
        Gl.UniformMatrix4(_uniformLocations[uniformName], 1, transpose, (float*) &value);
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