using System;
using System.IO;
using System.Numerics;
using Cubic.Entities;
using Cubic.Utilities;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Cubic.Render;

public class Skybox : IDisposable
{
    private readonly VertexPosition[] _vertices = new[]
    {
        new VertexPosition(new Vector3(-1, 1, -1)),
        new VertexPosition(new Vector3(1, 1, -1)),
        new VertexPosition(new Vector3(1, 1, 1)),
        new VertexPosition(new Vector3(-1, 1, 1)),

        new VertexPosition(new Vector3(-1, -1, 1)),
        new VertexPosition(new Vector3(1, -1, 1)),
        new VertexPosition(new Vector3(1, -1, -1)),
        new VertexPosition(new Vector3(-1, -1, -1)),

        new VertexPosition(new Vector3(-1, 1, -1)),
        new VertexPosition(new Vector3(-1, 1, 1)),
        new VertexPosition(new Vector3(-1, -1, 1)),
        new VertexPosition(new Vector3(-1, -1, -1)),

        new VertexPosition(new Vector3(1, 1, 1)),
        new VertexPosition(new Vector3(1, 1, -1)),
        new VertexPosition(new Vector3(1, -1, -1)),
        new VertexPosition(new Vector3(1, -1, 1)),

        new VertexPosition(new Vector3(1, 1, -1)),
        new VertexPosition(new Vector3(-1, 1, -1)),
        new VertexPosition(new Vector3(-1, -1, -1)),
        new VertexPosition(new Vector3(1, -1, -1)),

        new VertexPosition(new Vector3(-1, 1, 1)),
        new VertexPosition(new Vector3(1, 1, 1)),
        new VertexPosition(new Vector3(1, -1, 1)),
        new VertexPosition(new Vector3(-1, -1, 1))
    };
    
    private readonly uint[] _indices =
    {
        0, 1, 2, 0, 2, 3,
        4, 5, 6, 4, 6, 7,
        8, 9, 10, 8, 10, 11,
        12, 13, 14, 12, 14, 15,
        16, 17, 18, 16, 18, 19,
        20, 21, 22, 20, 22, 23
    };

    public const string VertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPosition;

out vec3 frag_texCoords;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * uView * uProjection;
    frag_texCoords = aPosition;
}";

    public const string FragmentShader = @"
#version 330 core

in vec3 frag_texCoords;

out vec4 out_color;

uniform samplerCube uSkybox;

void main()
{
    out_color = texture(uSkybox, frag_texCoords);
}";
    
    private int _vao;
    private int _vbo;
    private int _ebo;
    private Shader _shader;
    private CubeMap _cubeMap;

    public unsafe Skybox(CubeMap cubeMap)
    {
        _cubeMap = cubeMap;

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(VertexPosition), _vertices,
            BufferUsageHint.StaticDraw);

        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices,
            BufferUsageHint.StaticDraw);

        _shader = new Shader(VertexShader, FragmentShader);
        GL.UseProgram(_shader.Handle);
        
        RenderUtils.VertexAttribs(typeof(VertexPosition));
        
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    internal void Draw(Camera camera)
    {
        GL.CullFace(CullFaceMode.Front);
        GL.DepthMask(false);
        GL.UseProgram(_shader.Handle);
        Matrix4x4 view = camera.ViewMatrix;
        _shader.Set("uProjection", camera.ProjectionMatrix);
        // Convert the camera's 4x4 view matrix to a 3x3 rotation matrix - we only need rotation, not translation.
        _shader.Set("uView", camera.ViewMatrix.To3x3Matrix());
        
        GL.BindVertexArray(_vao);
        _cubeMap.Bind();
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        GL.BindVertexArray(0);
        GL.CullFace(CullFaceMode.Back);
        GL.DepthMask(true);
    }

    public void Dispose()
    {
        _cubeMap.Dispose();
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        _shader.Dispose();
    }
}