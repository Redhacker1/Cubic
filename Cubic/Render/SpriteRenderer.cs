using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Cubic.Utilities;
using static Cubic.Render.Graphics;
using Silk.NET.OpenGL;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic.Render;

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

    public const string VertexShader = @"
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec4 aTint;
layout (location = 3) in float aRotation;
layout (location = 4) in vec2 aOrigin;
layout (location = 5) in vec2 aScale;

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

    gl_Position = vec4(vertexPos, 0.0, 1.0) * uProjectionView;
    vec2 texCoords = aTexCoords;
    texCoords.y *= -1;
    frag_texCoords = texCoords;
    frag_tint = aTint;
}";

    public const string FragmentShader = @"
in vec2 frag_texCoords;
in vec4 frag_tint;

out vec4 out_color;

uniform sampler2D uTexture;
uniform bool uUseTexture;

void main()
{
    vec4 tex = texture(uTexture, frag_texCoords);
    // Invert it here to account for frag shaders that don't implement uUseTexture
    out_color = (!uUseTexture ? tex : vec4(1.0, 1.0, 1.0, tex.a)) * frag_tint;
}";

    #endregion

    private Graphics _graphics;

    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private Shader _shader;
    private Shader _shaderToUse;

    private Matrix4x4 _projectionMatrix;

    private bool _begun;
    private uint _currentSprite;
    private uint _currentSpriteIndex;
    private Texture _currentTexture;

    private List<Sprite> _sprites;

    private SpriteVertex[] _spriteVertices;
    private uint[] _spriteIndices;
    
    private SpriteVertex[] _vertices;
    private uint[] _indices;

    private TextureSample _sample;
    private bool _useTextureState;

    public Size FramebufferSize { get; private set; }

    internal unsafe SpriteRenderer(Graphics graphics)
    {
        _graphics = graphics;

        _sprites = new List<Sprite>(64);

        _vertices = new SpriteVertex[NumVertices];
        _indices = new uint[NumIndices];

        _spriteVertices = new SpriteVertex[MaxSprites * NumVertices];
        _spriteIndices = new uint[MaxSprites * NumIndices];

        _vao = Gl.GenVertexArray();
        Gl.BindVertexArray(_vao);

        _vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, (int) (MaxSprites * VertexSizeInBytes), null,
            BufferUsageARB.DynamicDraw);

        _ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (int) (MaxSprites * IndexSizeInBytes), null,
            BufferUsageARB.DynamicDraw);

        _shader = new Shader(VertexShader, FragmentShader);
        Gl.UseProgram(_shader.Handle);

        RenderUtils.VertexAttribs(typeof(SpriteVertex));

        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, _graphics.Viewport.Width,
            _graphics.Viewport.Height, 0, -1f, 1f);

        FramebufferSize = _graphics.Viewport.Size;
        
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        _graphics.ViewportResized += GraphicsOnViewportResized;

        _useTextureState = false;
    }

    private void GraphicsOnViewportResized(Size size)
    {
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, _graphics.Viewport.Width,
            _graphics.Viewport.Height, 0, -1, 1);

        FramebufferSize = size;
    }

    /// <summary>
    /// Initialise the SpriteRenderer so it can start accepting draw calls, and begin a batch session.
    /// </summary>
    /// <param name="transform">The optional transformation (camera) matrix to use for this batch session.</param>
    /// <param name="sample">Which sample type this batch should use.</param>
    /// <exception cref="CubicException">Thrown if you try to call <see cref="Begin"/> before a batch session has ended.</exception>
    public void Begin(Matrix4x4? transform = null, TextureSample sample = TextureSample.Nearest, Shader shader = null)
    {
        if (_begun)
            throw new CubicException(
                "There is already an active batch session. You must end a batch session before you can start another one.");
        _begun = true;
        
        Matrix4x4 tMatrix = transform ?? Matrix4x4.Identity;
        _shaderToUse = shader ?? _shader;
        Gl.UseProgram(_shaderToUse.Handle);
        _shaderToUse.Set("uProjectionView", tMatrix * _projectionMatrix);

        _sample = sample;
    }

    /// <summary>
    /// Draw a sprite to the screen.
    /// </summary>
    /// <param name="texture">The texture that should be drawn.</param>
    /// <param name="position">The position, relative to the transformation matrix, the sprite should be drawn at. If no transformation matrix is provided, this will be relative to the screen-coordinates.</param>
    public void Draw(Texture texture, Vector2 position) => Draw(texture, position, null, Color.White, 0, Vector2.Zero,
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
    /// <param name="useTexture">If false, the texture will be drawn as the tint colour. The alpha values, however, of the texture will still be respected. Transparent parts of the texture will remain transparent (or translucent).</param>
    /// <exception cref="CubicException">Thrown if a draw call is issued when there is no current batch session.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <see cref="flip"/> value provided is invalid.</exception>
    public void Draw(Texture texture, Vector2 position, Rectangle? source, Color tint, float rotation, Vector2 origin,
        Vector2 scale, SpriteFlipMode flip, float depth = 0, bool useTexture = true)
    {
        if (!_begun)
            throw new CubicException("There is no active batch session. You must start a new batch session before you can issue draw calls.");
        _sprites.Add(new Sprite(texture, position, source, tint, rotation, origin, scale, flip, depth, _currentSprite, useTexture));
        _currentSprite++;
        Metrics.SpritesDrawnInternal++;
    }

    /// <summary>
    /// Draw a rectangle at the given position.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size (in pixels) that the rectangle should be.</param>
    /// <param name="color">The color of the rectangle.</param>
    /// <param name="rotation">The rectangle's rotation.</param>
    /// <param name="origin">The origin point of the rectangle. NOTE: This value is between 0 and 1, not the size of the rectangle.</param>
    public void DrawRectangle(Vector2 position, Vector2 size, Color color, float rotation, Vector2 origin)
    {
        Draw(Texture2D.Blank, position, null, color, rotation, origin, size, SpriteFlipMode.None);
    }

    /// <summary>
    /// Draw a "border" rectangle. This will draw 4 lines of the given thickness in a border shape.
    /// </summary>
    /// <param name="position">The position of the border.</param>
    /// <param name="size">The size (in pixels) of the border.</param>
    /// <param name="borderWidth">The border width (in pixels).</param>
    /// <param name="color">The color of the border.</param>
    /// <param name="rotation">The border's rotation.</param>
    /// <param name="origin">The origin point of the border. NOTE: This value is between 0 and 1, not the size of the border.</param>
    public void DrawBorder(Vector2 position, Vector2 size, int borderWidth, Color color, float rotation, Vector2 origin)
    {
        Draw(Texture2D.Blank, position, null, color, rotation, origin, new Vector2(size.X, borderWidth), SpriteFlipMode.None);
        Draw(Texture2D.Blank, new Vector2(position.X, position.Y + size.Y - borderWidth), null, color, rotation, origin, new Vector2(size.X, borderWidth), SpriteFlipMode.None);
        Draw(Texture2D.Blank, position, null, color, rotation, origin, new Vector2(borderWidth, size.Y), SpriteFlipMode.None);
        Draw(Texture2D.Blank, new Vector2(position.X + size.X - borderWidth, position.Y), null, color, rotation, origin, new Vector2(borderWidth, size.Y), SpriteFlipMode.None);
    }

    public void DrawBorderRectangle(Vector2 position, Vector2 size, int borderWidth, Color borderColor,
        Color rectangleColor, float rotation, Vector2 origin)
    {
        DrawRectangle(position, size, rectangleColor, rotation, origin);
        DrawBorder(position, size, borderWidth, borderColor, rotation, origin);
    }

    public void DrawLine(Vector2 a, Vector2 b, int thickness, Color color)
    {
        float length = Vector2.Distance(a, b);
        Vector2 diff = b - a;
        float rot = MathF.Atan2(diff.Y, diff.X);
        Draw(Texture2D.Blank, a, null, color, rot, new Vector2(0, 0.5f), new Vector2(length, thickness),
            SpriteFlipMode.None);
    }

    public void DrawPolygon(Vector2 positionOffset, Vector2[] positions, int thickness, Color color)
    {
        Vector2 lastPos = Vector2.Zero;
        foreach (Vector2 pos in positions)
        {
            DrawLine(lastPos + positionOffset, pos + positionOffset, thickness, color);
            lastPos = pos;
        }
    }
    
    private void DrawSprite(Sprite sprite)
    {
        if (sprite.Texture != _currentTexture)
            Flush();
        if (_currentSpriteIndex >= MaxSprites)
            Flush();
        if (sprite.UseTexture != _useTextureState)
        {
            Flush();
            _useTextureState = sprite.UseTexture;
            _shader.Set("uUseTexture", !sprite.UseTexture);
        }
        _currentTexture = sprite.Texture;

        Rectangle src = sprite.Source ?? new Rectangle(0, 0, sprite.Texture.Size.Width, sprite.Texture.Size.Height);

        float texOffsetX = src.X / (float) sprite.Texture.Size.Width;
        float texOffsetY = 1 - (src.Y + src.Height) / (float) sprite.Texture.Size.Height;
        float texOffsetW = texOffsetX + src.Width / (float) sprite.Texture.Size.Width;
        float texOffsetH = texOffsetY + src.Height / (float) sprite.Texture.Size.Height;

        switch (sprite.Flip)
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
                throw new ArgumentOutOfRangeException(nameof(sprite.Flip), sprite.Flip, null);
        }

        Vector4 normalizedTint = sprite.Tint.Normalize();

        sprite.Position -= sprite.Origin;
        sprite.Origin += sprite.Position;

        // We overwrite the elements of the arrays to reduce array allocations, making it more memory efficient.
        
        _vertices[0] = new SpriteVertex(new Vector2(sprite.Position.X + src.Width, sprite.Position.Y + src.Height), new Vector2(texOffsetW, texOffsetY), normalizedTint, sprite.Rotation, sprite.Origin, sprite.Scale);
        _vertices[1] = new SpriteVertex(new Vector2(sprite.Position.X + src.Width, sprite.Position.Y), new Vector2(texOffsetW, texOffsetH), normalizedTint, sprite.Rotation, sprite.Origin, sprite.Scale);
        _vertices[2] = new SpriteVertex(new Vector2(sprite.Position.X, sprite.Position.Y), new Vector2(texOffsetX, texOffsetH), normalizedTint, sprite.Rotation, sprite.Origin, sprite.Scale);
        _vertices[3] = new SpriteVertex(new Vector2(sprite.Position.X, sprite.Position.Y + src.Height), new Vector2(texOffsetX, texOffsetY), normalizedTint, sprite.Rotation, sprite.Origin, sprite.Scale);

        _indices[0] = NumVertices * _currentSpriteIndex;
        _indices[1] = 1 + NumVertices * _currentSpriteIndex;
        _indices[2] = 3 + NumVertices * _currentSpriteIndex;
        _indices[3] = 1 + NumVertices * _currentSpriteIndex;
        _indices[4] = 2 + NumVertices * _currentSpriteIndex;
        _indices[5] = 3 + NumVertices * _currentSpriteIndex;

        Array.Copy(_vertices, 0, _spriteVertices, _currentSpriteIndex * NumVertices, NumVertices);
        Array.Copy(_indices, 0, _spriteIndices, _currentSpriteIndex * NumIndices, NumIndices);

        _currentSpriteIndex++;
    }

    /// <summary>
    /// End the current batch session, and push all remaining sprites to the screen.
    /// </summary>
    /// <exception cref="CubicException">Thrown if you try to call <see cref="End"/> when there is no current batch session.</exception>
    public void End()
    {
        if (!_begun)
            throw new CubicException("There is no current batch session active, there is none to close.");

        _sprites.Sort((sprite, sprite1) =>
        {
            // The CompareTo method seems to produce random results based on what's in the list.
            // This means if two sprites have the same depth value, it's random what order they will be drawn in.
            // Any decent game should really have a different depth level for each sprite, but many won't, so therefore
            // if the depth values are the same we should sort by draw call (ID) instead.
            int sort = sprite1.Depth.CompareTo(sprite.Depth);
            if (sort == 0)
                sort = sprite.ID.CompareTo(sprite1.ID);
            return sort;
        });

        for (int i = 0; i < _currentSprite; i++)
            DrawSprite(_sprites[i]);

        Flush();
        _currentSprite = 0;

        _sprites.Clear();

        _begun = false;
    }

    private unsafe void Flush()
    {
        if (_currentSpriteIndex == 0)
            return;

        Gl.FrontFace(FrontFaceDirection.Ccw);
        Gl.Disable(EnableCap.DepthTest);
        
        Gl.BindVertexArray(_vao);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (SpriteVertex* p = _spriteVertices)
            Gl.BufferSubData(BufferTargetARB.ArrayBuffer, IntPtr.Zero, _currentSpriteIndex * VertexSizeInBytes, p);
        fixed (uint* p = _spriteIndices)
            Gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, IntPtr.Zero, _currentSpriteIndex * IndexSizeInBytes, p);
        
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, _currentTexture.Handle);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int) (_sample == TextureSample.Nearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int) (_sample == TextureSample.Nearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
        
        Gl.UseProgram(_shaderToUse.Handle);
        Gl.BindVertexArray(_vao);
        Gl.DrawElements(PrimitiveType.Triangles, _currentSpriteIndex * NumIndices, DrawElementsType.UnsignedInt, null);
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        Gl.FrontFace(FrontFaceDirection.CW);
        Gl.Enable(EnableCap.DepthTest);

        Metrics.DrawCallsInternal++;
        
        _currentSpriteIndex = 0;
    }

    public void Dispose()
    {
        Gl.DeleteVertexArray(_vao);
        Gl.DeleteBuffer(_vbo);
        Gl.DeleteBuffer(_ebo);
        _shader.Dispose();
        _graphics.ViewportResized -= GraphicsOnViewportResized;
    }

    private struct Sprite
    {
        public Texture Texture;
        public Vector2 Position;
        public Rectangle? Source;
        public Color Tint;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 Scale;
        public SpriteFlipMode Flip;
        public float Depth;
        public uint ID;
        public bool UseTexture;

        public Sprite(Texture texture, Vector2 position, Rectangle? source, Color tint, float rotation, Vector2 origin,
            Vector2 scale, SpriteFlipMode flip, float depth, uint id, bool useTexture)
        {
            Texture = texture;
            Position = position;
            Source = source;
            Tint = tint;
            Rotation = rotation;
            Origin = origin;
            Scale = scale;
            Flip = flip;
            Depth = depth;
            ID = id;
            UseTexture = useTexture;
        }
    }

    private struct SpriteVertex
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public Vector4 Tint;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 Scale;

        public SpriteVertex(Vector2 position, Vector2 texCoords, Vector4 tint, float rotation, Vector2 origin, Vector2 scale)
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
        public const uint SizeInBytes = 52;
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