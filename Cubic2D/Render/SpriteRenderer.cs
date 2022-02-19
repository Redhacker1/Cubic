using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Cubic2D.Utilities;
using OpenTK.Graphics.OpenGL4;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic2D.Render;

public class SpriteRenderer : IDisposable
{
    #region Constants

    public const uint MaxSprites = 512;

    private const uint NumVertices = 4;
    private const uint NumIndices = 6;
    
    // The size of a sprite's vertices in bytes. In this case, we have 4 vertices per sprite. We multiply that by
    // the size in bytes of a sprite vertex, which in this case is a precalculated constant.
    private const uint VertexSizeInBytes = NumVertices * SpriteVertex.SizeInBytes;
    
    // The size of a sprite's indices in bytes. In this case, we have 6 indices per sprite. We multiply that by the
    // size of a uint (4 bytes, so technically I could just put 24 here).
    private const uint IndexSizeInBytes = NumIndices * sizeof(uint);
    
    #endregion

    #region Shaders

    private const string VertexCode = @"
#version 330 core

in vec3 aPosition;
in vec2 aTexCoords;
in vec4 aTint;
in float aRotation;
in vec2 aOrigin;
in vec2 aScale;

out vec2 frag_texCoords;
out vec4 frag_tint;

uniform mat4 uProjectionView;

void main()
{
    float cosRot = cos(aRotation);
    float sinRot = sin(aRotation);
    mat2 rot = mat2(vec2(cosRot, sinRot), vec2(-sinRot, cosRot));

    vec2 vertexPos = aPosition.xy - aOrigin;
    vertexPos *= aScale;
    vertexPos = rot * vertexPos;
    vertexPos += aOrigin;
    vec3 vPos = vec3(vertexPos, aPosition.z);

    gl_Position = vec4(vPos, 1.0) * uProjectionView;
    vec2 texCoords = aTexCoords;
    texCoords.y *= -1;
    frag_texCoords = texCoords;
    frag_tint = aTint;
}";

    private const string FragmentCode = @"
#version 330 core

in vec2 frag_texCoords;
in vec4 frag_tint;

out vec4 out_color;

uniform sampler2D uTexture;

void main()
{
    out_color = texture(uTexture, frag_texCoords) * frag_tint;
}";

    #endregion

    private Graphics _graphics;

    private int _vao;
    private int _vbo;
    private int _ebo;
    private Shader _shader;

    private Matrix4x4 _projectionMatrix;

    private bool _begun;
    private uint _currentSprite;
    private Texture2D _currentTexture;

    private SpriteVertex[] _spriteVertices;
    private uint[] _spriteIndices;
    
    private SpriteVertex[] _vertices;
    private uint[] _indices;

    private TextureSample _sample;

    internal SpriteRenderer(Graphics graphics)
    {
        _graphics = graphics;

        _vertices = new SpriteVertex[NumVertices];
        _indices = new uint[NumIndices];

        _spriteVertices = new SpriteVertex[MaxSprites * NumVertices];
        _spriteIndices = new uint[MaxSprites * NumIndices];

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, (int) (MaxSprites * VertexSizeInBytes), IntPtr.Zero,
            BufferUsageHint.DynamicDraw);

        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, (int) (MaxSprites * IndexSizeInBytes), IntPtr.Zero,
            BufferUsageHint.DynamicDraw);

        _shader = new Shader(VertexCode, FragmentCode);
        GL.UseProgram(_shader.Handle);

        Type type = typeof(SpriteVertex);
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

        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, _graphics.FramebufferSize.Width,
            _graphics.FramebufferSize.Height, 0, -1f, 1f);
        
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        _graphics.ViewportResized += GraphicsOnViewportResized;
    }

    private void GraphicsOnViewportResized(Size size)
    {
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, _graphics.FramebufferSize.Width,
            _graphics.FramebufferSize.Height, 0, -1, 1);
    }

    /// <summary>
    /// Initialise the SpriteRenderer so it can start accepting draw calls, and begin a batch session.
    /// </summary>
    /// <param name="transform">The optional transformation (camera) matrix to use for this batch session.</param>
    /// <exception cref="CubicException">Thrown if you try to call <see cref="Begin"/> before a batch session has ended.</exception>
    public void Begin(Matrix4x4? transform = null, TextureSample sample = TextureSample.Point)
    {
        if (_begun)
            throw new CubicException(
                "There is already an active batch session. You must end a batch session before you can start another one.");
        _begun = true;
        
        Matrix4x4 tMatrix = transform ?? Matrix4x4.Identity;
        GL.UseProgram(_shader.Handle);
        _shader.Set("uProjectionView", tMatrix * _projectionMatrix);

        _sample = sample;
    }

    /// <summary>
    /// Draw a sprite to the screen.
    /// </summary>
    /// <param name="texture">The texture that should be drawn.</param>
    /// <param name="position">The position, relative to the transformation matrix, the sprite should be drawn at. If no transformation matrix is provided, this will be relative to the screen-coordinates.</param>
    public void Draw(Texture2D texture, Vector2 position) => Draw(texture, position, null, Color.White, 0, Vector2.Zero,
        Vector2.One, SpriteFlipMode.None);

    /// <summary>
    /// Draw a sprite to the screen.
    /// </summary>
    /// <param name="texture">The texture that should be drawn.</param>
    /// <param name="position">The position, relative to the transformation matrix, the sprite should be drawn at. If no transformation matrix is provided, this will be relative to the screen-coordinates.</param>
    /// <param name="source">An optional source rectangle, useful for spritesheets.</param>
    /// <param name="tint">The colour to tint the sprite by, use White for no tint.</param>
    /// <param name="rotation">The rotation of the sprite in radians.</param>
    /// <param name="origin">The origin point of the sprite. Rotations will be centered around this point.</param>
    /// <param name="scale">The scale of the sprite.</param>
    /// <param name="flip">The type of optional flip the sprite will display on screen.</param>
    /// <param name="depth">The depth the sprite will be drawn at. A sprite with a greater depth will be placed <b>behind</b> other sprites with lesser depths.</param>
    /// <exception cref="CubicException">Thrown if a draw call is issued when there is no current batch session.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <see cref="flip"/> value provided is invalid.</exception>
    public void Draw(Texture2D texture, Vector2 position, Rectangle? source, Color tint, float rotation, Vector2 origin, Vector2 scale, SpriteFlipMode flip, float depth = 0)
    {
        if (!_begun)
            throw new CubicException("There is no active batch session. You must start a new batch session before you can issue draw calls.");
        
        if (texture != _currentTexture)
            Flush();
        if (_currentSprite >= MaxSprites)
            Flush();
        _currentTexture = texture;

        Rectangle src = source ?? new Rectangle(0, 0, texture.Size.Width, texture.Size.Height);

        float texOffsetX = src.X / (float) texture.Size.Width;
        float texOffsetY = 1 - (src.Y + src.Height) / (float) texture.Size.Height;
        float texOffsetW = texOffsetX + src.Width / (float) texture.Size.Width;
        float texOffsetH = texOffsetY + src.Height / (float) texture.Size.Height;

        switch (flip)
        {
            case SpriteFlipMode.None:
                break;
            case SpriteFlipMode.FlipX:
                texOffsetW *= -1;
                break;
            case SpriteFlipMode.FlipY:
                texOffsetH *= -1;
                break;
            case SpriteFlipMode.FlipXY:
                texOffsetW *= -1;
                texOffsetH *= -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(flip), flip, null);
        }

        Vector4 normalizedTint = tint.Normalize();

        position -= origin;
        origin += position;

        // We overwrite the elements of the arrays to reduce array allocations, making it more memory efficient.
        
        _vertices[0] = new SpriteVertex(new Vector3(position.X + src.Width, position.Y + src.Height, depth), new Vector2(texOffsetW, texOffsetY), normalizedTint, rotation, origin, scale);
        _vertices[1] = new SpriteVertex(new Vector3(position.X + src.Width, position.Y, depth), new Vector2(texOffsetW, texOffsetH), normalizedTint, rotation, origin, scale);
        _vertices[2] = new SpriteVertex(new Vector3(position.X, position.Y, depth), new Vector2(texOffsetX, texOffsetH), normalizedTint, rotation, origin, scale);
        _vertices[3] = new SpriteVertex(new Vector3(position.X, position.Y + src.Height, depth), new Vector2(texOffsetX, texOffsetY), normalizedTint, rotation, origin, scale);

        _indices[0] = NumVertices * _currentSprite;
        _indices[1] = 1 + NumVertices * _currentSprite;
        _indices[2] = 3 + NumVertices * _currentSprite;
        _indices[3] = 1 + NumVertices * _currentSprite;
        _indices[4] = 2 + NumVertices * _currentSprite;
        _indices[5] = 3 + NumVertices * _currentSprite;
        
        // TODO: Implement proper sprite sorting instead of z-depth sorting. It is bonkers for transparency apparently.

        Array.Copy(_vertices, 0, _spriteVertices, _currentSprite * NumVertices, NumVertices);
        Array.Copy(_indices, 0, _spriteIndices, _currentSprite * NumIndices, NumIndices);

        _currentSprite++;
    }

    /// <summary>
    /// End the current batch session, and push all remaining sprites to the screen.
    /// </summary>
    /// <exception cref="CubicException">Thrown if you try to call <see cref="End"/> when there is no current batch session.</exception>
    public void End()
    {
        if (!_begun)
            throw new CubicException("There is no current batch session active, there is none to close.");
        
        Flush();
        _begun = false;
    }

    private void Flush()
    {
        if (_currentSprite == 0)
            return;
        
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (int) (MaxSprites * VertexSizeInBytes), _spriteVertices);
        GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, (int) (MaxSprites * IndexSizeInBytes), _spriteIndices);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _currentTexture.Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int) (_sample == TextureSample.Point ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int) (_sample == TextureSample.Point ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
        
        GL.UseProgram(_shader.Handle);
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, (int) (_currentSprite * NumIndices), DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        _currentSprite = 0;
    }
    
    public void Dispose()
    {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        _shader.Dispose();
        _graphics.ViewportResized -= GraphicsOnViewportResized;
    }

    private struct SpriteVertex
    {
        public Vector3 Position;
        public Vector2 TexCoords;
        public Vector4 Tint;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 Scale;

        public SpriteVertex(Vector3 position, Vector2 texCoords, Vector4 tint, float rotation, Vector2 origin, Vector2 scale)
        {
            Position = position;
            TexCoords = texCoords;
            Tint = tint;
            Rotation = rotation;
            Origin = origin;
            Scale = scale;
        }

        // Precalculated size in bytes. This is precalculated as it's used in the size constant above and you can't use
        // sizeof() structs in constant fields.
        public const uint SizeInBytes = 56;
    }
}

public enum SpriteFlipMode
{
    /// <summary>
    /// The sprite will not be flipped.
    /// </summary>
    None,
    /// <summary>
    /// Flip the sprite in the X-axis.
    /// </summary>
    FlipX,
    /// <summary>
    /// Flip the sprite in the Y-axis.
    /// </summary>
    FlipY,
    /// <summary>
    /// Flip the sprite in both the X and Y axis.
    /// </summary>
    FlipXY
}