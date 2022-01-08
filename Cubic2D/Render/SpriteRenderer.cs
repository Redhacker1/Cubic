using System.Drawing;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic2D.Render;

public class SpriteRenderer : UnmanagedResource
{
    #region Constants
    
    private const uint MaxSprites = 512;

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

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;

layout (location = 0) out vec2 frag_texCoords;

layout (set = 0, binding = 0) uniform ProjectionViewBuffer
{
    mat4 ProjectionView;
}

void main()
{
    gl_Position = ProjectionView * vec4(aPosition, 0.0, 1.0);
    vec2 texCoords = aTexCoords;
    texCoords.y *= -1;
    frag_texCoords = texCoords;
}";

    private const string FragmentCode = @"
#version 450

layout (location = 0) in vec2 frag_texCoords;

layout (location = 0) out vec4 out_color;

layout (set = 1, binding = 0) uniform texture2D Texture;
layout (set = 1, binding = 1) uniform sampler Sampler;

void main()
{
    out_color = texture(sampler2D(Texture, Sampler), frag_texCoords);
}";

    #endregion

    private Graphics _graphics;
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Pipeline _pipeline;

    private ResourceSet _projectionViewSet;
    private ResourceSet _textureSet;

    private DeviceBuffer _projectionViewBuffer;

    private Matrix4x4 _projectionMatrix;

    private bool _begun;
    private uint _currentSprite;
    private Texture2D _currentTexture;

    public SpriteRenderer(Graphics graphics)
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
                    new VertexElementDescription("aPosition", VertexElementFormat.Float2,
                        VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("aTexCoords", VertexElementFormat.Float2,
                        VertexElementSemantic.TextureCoordinate))
            },
            graphics.ResourceFactory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main")));

        ResourceLayout projectionViewLayout = graphics.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionViewBuffer",
                ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        ResourceLayout textureLayout = graphics.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _pipeline = graphics.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend, DepthStencilStateDescription.Disabled,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, false,
                false), PrimitiveTopology.TriangleList, shaderDescription, new[] {projectionViewLayout, textureLayout},
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

    public void Begin(Matrix4x4? transform = null)
    {
        if (_begun)
            throw new CubicException(
                "Sprite renderer has already begun. Please open a new issue on GitHub with this error.");
        _begun = true;
        
        Matrix4x4 tMatrix = transform ?? Matrix4x4.Identity;
        _graphics.GraphicsDevice.UpdateBuffer(_projectionViewBuffer, 0, _projectionMatrix * tMatrix);
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? source, Vector2 scale)
    {
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

        SpriteVertex[] vertices = new SpriteVertex[]
        {
            new SpriteVertex(new Vector2(position.X + src.Width, position.Y + src.Height), new Vector2(texOffsetW, texOffsetH)),
            new SpriteVertex(new Vector2(position.X + src.Width, position.Y), new Vector2(texOffsetW, texOffsetH)),
            new SpriteVertex(new Vector2(position.X, position.Y), new Vector2(texOffsetH, texOffsetH)),
            new SpriteVertex(new Vector2(position.X, position.Y + src.Height), new Vector2(texOffsetX, texOffsetY))
        };
        
        
    }

    public void End()
    {
        Flush();
        
    }

    private void Flush()
    {
        if (_currentSprite == 0)
            return;
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
        public Vector2 Position;
        public Vector2 TexCoords;

        public SpriteVertex(Vector2 position, Vector2 texCoords)
        {
            Position = position;
            TexCoords = texCoords;
        }

        public const uint SizeInBytes = 8;
    }
}