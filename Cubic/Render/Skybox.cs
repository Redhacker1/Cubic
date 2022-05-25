using System;
using System.IO;
using System.Numerics;
using Cubic.Entities;
using Cubic.Utilities;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;

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
in vec3 frag_texCoords;

out vec4 out_color;

uniform samplerCube uSkybox;

void main()
{
    out_color = texture(uSkybox, frag_texCoords);
}";
    
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private Shader _shader;
    private CubeMap _cubeMap;

    public unsafe Skybox(CubeMap cubeMap)
    {
        _cubeMap = cubeMap;

        _vao = Gl.GenVertexArray();
        Gl.BindVertexArray(_vao);

        _vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (VertexPosition* vp = _vertices)
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (_vertices.Length * sizeof(VertexPosition)), vp, BufferUsageARB.StaticDraw);

        _ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* p = _indices)
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (_indices.Length * sizeof(uint)), p, BufferUsageARB.StaticDraw);

        _shader = new Shader(VertexShader, FragmentShader);
        Gl.UseProgram(_shader.Handle);
        
        RenderUtils.VertexAttribs(typeof(VertexPosition));
        
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    internal unsafe void Draw(Camera camera)
    {
        Gl.CullFace(CullFaceMode.Front);
        Gl.DepthMask(false);
        Gl.UseProgram(_shader.Handle);
        Matrix4x4 view = camera.ViewMatrix;
        _shader.Set("uProjection", camera.ProjectionMatrix);
        // Convert the camera's 4x4 view matrix to a 3x3 rotation matrix - we only need rotation, not translation.
        _shader.Set("uView", camera.ViewMatrix.To3x3Matrix());
        
        Gl.BindVertexArray(_vao);
        _cubeMap.Bind();
        Gl.DrawElements(PrimitiveType.Triangles, (uint) _indices.Length, DrawElementsType.UnsignedInt, null);
        
        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        Gl.BindVertexArray(0);
        Gl.CullFace(CullFaceMode.Back);
        Gl.DepthMask(true);
    }

    public void Dispose()
    {
        _cubeMap.Dispose();
        Gl.DeleteVertexArray(_vao);
        Gl.DeleteBuffer(_vbo);
        Gl.DeleteBuffer(_ebo);
        _shader.Dispose();
    }
}