using System;
using System.Drawing;
using System.Numerics;
using System.Text;
using Cubic2D.Utilities;
using Veldrid;
using Veldrid.SPIRV;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic2D.Render;

public class SpriteRenderer : UnmanagedResource
{
    #region Constants
    
    public const uint MaxSprites = 512;

    // The size of a sprite's vertices in bytes. In this case, we have 4 vertices per sprite. We multiply that by
    // the size in bytes of a sprite vertex, which in this case is a precalculated constant.
    private const uint VertexSizeInBytes = 4 * SpriteVertex.SizeInBytes;
    
    // The size of a sprite's indices in bytes. In this case, we have 6 indices per sprite. We multiply that by the
    // size of a uint (4 bytes, so technically I could just put 24 here).
    private const uint IndexSizeInBytes = 6 * sizeof(uint);
    
    #endregion

    #region Shaders

    private const string VertexCode = @"
#version 450

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec4 aTint;
layout (location = 3) in float aRotation;
layout (location = 4) in vec2 aOrigin;
layout (location = 5) in vec2 aScale;

layout (location = 0) out vec2 frag_texCoords;
layout (location = 1) out vec4 frag_tint;

layout (set = 0, binding = 0) uniform ProjectionViewBuffer
{
    mat4 ProjectionView;
};

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

    gl_Position = ProjectionView * vec4(vPos, 1.0);
    vec2 texCoords = aTexCoords;
    texCoords.y *= -1;
    frag_texCoords = texCoords;
    frag_tint = aTint;
}";

    private const string FragmentCode = @"
#version 450

layout (location = 0) in vec2 frag_texCoords;
layout (location = 1) in vec4 frag_tint;

layout (location = 0) out vec4 out_color;

layout (set = 1, binding = 0) uniform texture2D Texture;
layout (set = 1, binding = 1) uniform sampler Sampler;

void main()
{
    out_color = texture(sampler2D(Texture, Sampler), frag_texCoords) * frag_tint;
}";

    #endregion

    private Graphics _graphics;
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Pipeline _pipeline;

    private ResourceSet _projectionViewSet;
    private ResourceLayout _textureLayout;
    private ResourceSet _textureSet;

    private DeviceBuffer _projectionViewBuffer;

    private Matrix4x4 _projectionMatrix;

    private bool _begun;
    private uint _currentSprite;
    private Texture2D _currentTexture;

    internal SpriteRenderer(Graphics graphics)
    {
        _graphics = graphics;

        _vertexBuffer = graphics.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * VertexSizeInBytes,
            BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = graphics.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * IndexSizeInBytes,
            BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        ShaderSetDescription shaderDescription = new ShaderSetDescription(
            new[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("aPosition", VertexElementFormat.Float3,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aTexCoords", VertexElementFormat.Float2,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aTint", VertexElementFormat.Float4,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aRotation", VertexElementFormat.Float1,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aOrigin", VertexElementFormat.Float2,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aScale", VertexElementFormat.Float2,
                        VertexElementSemantic.TextureCoordinate))
            },
            graphics.ResourceFactory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main")));

        ResourceLayout projectionViewLayout = graphics.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionViewBuffer",
                ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        _textureLayout = graphics.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _pipeline = graphics.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend, DepthStencilStateDescription.DepthOnlyGreaterEqual,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, false,
                false), PrimitiveTopology.TriangleList, shaderDescription, new[] {projectionViewLayout, _textureLayout},
            graphics.GraphicsDevice.SwapchainFramebuffer.OutputDescription));

        // The size here is 64 as that is the size of Matrix4x4
        _projectionViewBuffer =
            graphics.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, graphics.GraphicsDevice.SwapchainFramebuffer.Width,
            graphics.GraphicsDevice.SwapchainFramebuffer.Height, 0, -1, 1);

        _projectionViewSet =
            graphics.ResourceFactory.CreateResourceSet(new ResourceSetDescription(projectionViewLayout,
                _projectionViewBuffer));
    }
    
    /// <summary>
    /// Initialise the SpriteRenderer so it can start accepting draw calls, and begin a batch session.
    /// </summary>
    /// <param name="transform">The optional transformation (camera) matrix to use for this batch session.</param>
    /// <exception cref="CubicException">Thrown if you try to call <see cref="Begin"/> before a batch session has ended.</exception>
    public void Begin(Matrix4x4? transform = null)
    {
        if (_begun)
            throw new CubicException(
                "There is already an active batch session. You must end a batch session before you can start another one.");
        _begun = true;
        
        Matrix4x4 tMatrix = transform ?? Matrix4x4.Identity;
        _graphics.CL.UpdateBuffer(_projectionViewBuffer, 0, tMatrix * _projectionMatrix);
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

        SpriteVertex[] vertices = new SpriteVertex[]
        {
            new SpriteVertex(new Vector3(position.X + src.Width, position.Y + src.Height, depth), new Vector2(texOffsetW, texOffsetY), normalizedTint, rotation, origin, scale),
            new SpriteVertex(new Vector3(position.X + src.Width, position.Y, depth), new Vector2(texOffsetW, texOffsetH), normalizedTint, rotation, origin, scale),
            new SpriteVertex(new Vector3(position.X, position.Y, depth), new Vector2(texOffsetX, texOffsetH), normalizedTint, rotation, origin, scale),
            new SpriteVertex(new Vector3(position.X, position.Y + src.Height, depth), new Vector2(texOffsetX, texOffsetY), normalizedTint, rotation, origin, scale)
        };

        uint[] indices =
        {
            0 + 4 * _currentSprite, 1 + 4 * _currentSprite, 3 + 4 * _currentSprite,
            1 + 4 * _currentSprite, 2 + 4 * _currentSprite, 3 + 4 * _currentSprite
        };
        
        _graphics.CL.UpdateBuffer(_vertexBuffer, _currentSprite * VertexSizeInBytes, vertices);
        _graphics.CL.UpdateBuffer(_indexBuffer, _currentSprite * IndexSizeInBytes, indices);

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
        
        _textureSet?.Dispose();
        _textureSet = _graphics.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayout,
            _currentTexture.Texture, _graphics.GraphicsDevice.PointSampler));

        _graphics.CL.SetPipeline(_pipeline);
        _graphics.CL.SetGraphicsResourceSet(0, _projectionViewSet);
        _graphics.CL.SetGraphicsResourceSet(1, _textureSet);
        _graphics.CL.SetVertexBuffer(0, _vertexBuffer);
        _graphics.CL.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

        _graphics.CL.DrawIndexed(_currentSprite * 6);

        _currentSprite = 0;
    }
    
    internal override void Dispose()
    {
        _pipeline.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projectionViewSet.Dispose();
        _textureSet.Dispose();
        _projectionViewBuffer.Dispose();
        // TODO: Add more stuff to dispose, there is more of it.
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

        public const uint SizeInBytes = 56;
    }
}

public enum SpriteFlipMode
{
    None,
    FlipX,
    FlipY,
    FlipXY
}