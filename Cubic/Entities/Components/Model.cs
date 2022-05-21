using System;
using System.Numerics;
using Cubic.Primitives;
using Cubic.Render;
using Cubic.Render.Lighting;
using Cubic.Scenes;
using Cubic.Utilities;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;
using Shader = Cubic.Render.Shader;

namespace Cubic.Entities.Components;

public class Model : Component
{
    public readonly VertexPositionTextureNormal[] Vertices;
    public readonly uint[] Indices;

    private uint _vao;
    private uint _vbo;
    private uint _ebo;

    private static Shader _shader;
    private static bool _shaderDisposed;

    public Material Material;

    public const string VertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormals;

out vec2 frag_texCoords;
out vec3 frag_normal;
out vec3 frag_position;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    frag_texCoords = aTexCoords;
    gl_Position = vec4(aPosition, 1.0) * uModel * uView * uProjection;
    frag_position = vec3(vec4(aPosition, 1.0) * uModel);
    frag_normal = aNormals * mat3(transpose(inverse(uModel)));
}";

    public const string FragmentShader = @"
#version 330 core

struct Material
{
    sampler2D albedo;
    sampler2D specular;
    vec4 color;
    int shininess;
};

struct DirectionalLight
{
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

in vec2 frag_texCoords;
in vec3 frag_normal;
in vec3 frag_position;

out vec4 out_color;

uniform DirectionalLight uSun;
uniform Material uMaterial;
uniform vec3 uCameraPos;

vec3 CalculateDirectional(DirectionalLight light, vec3 normal, vec3 viewDir);

void main()
{
    vec3 norm = normalize(frag_normal);
    vec3 viewDir = normalize(uCameraPos - frag_position);
    
    vec3 result = CalculateDirectional(uSun, norm, viewDir);
    out_color = vec4(result, 1.0) * uMaterial.color;
}

vec3 CalculateDirectional(DirectionalLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    
    float diff = max(dot(normal, lightDir), 0.0);
    
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uMaterial.shininess);

    vec3 ambient = light.ambient * vec3(texture(uMaterial.albedo, frag_texCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(uMaterial.albedo, frag_texCoords));
    vec3 specular = light.specular * spec * vec3(texture(uMaterial.specular, frag_texCoords));
    return (ambient + diffuse + specular);
}";

    static Model()
    {
        _shaderDisposed = true;
    }

    public Model(VertexPositionTextureNormal[] vertices, uint[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public Model(IPrimitive primitive, Material material)
    {
        Vertices = primitive.Vertices;
        Indices = primitive.Indices;
        Material = material;
    }

    protected internal override unsafe void Initialize()
    {
        base.Initialize();

        if (_shaderDisposed)
        {
            _shader = new Shader(VertexShader, FragmentShader);
            _shaderDisposed = false;
        }

        _vao = Gl.GenVertexArray();
        Gl.BindVertexArray(_vao);

        _vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (VertexPositionTextureNormal* vptn = Vertices)
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (Vertices.Length * sizeof(VertexPositionTextureNormal)), vptn, BufferUsageARB.StaticDraw);

        _ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* p = Indices)
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (Indices.Length * sizeof(uint)), p, BufferUsageARB.StaticDraw);
        
        Gl.UseProgram(_shader.Handle);
        
        RenderUtils.VertexAttribs(typeof(VertexPositionTextureNormal));

        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    protected internal override unsafe void Draw()
    {
        base.Draw();
        
        Gl.BindVertexArray(_vao);
        Gl.UseProgram(_shader.Handle);
        _shader.Set("uProjection", Camera.Main.ProjectionMatrix);
        _shader.Set("uView", Camera.Main.ViewMatrix);
        _shader.Set("uModel", Matrix4x4.CreateFromQuaternion(Transform.Rotation) * Matrix4x4.CreateTranslation(Transform.Position));
        
        _shader.Set("uCameraPos", Camera.Main.Transform.Position);
        _shader.Set("uMaterial.albedo", 0);
        _shader.Set("uMaterial.specular", 1);
        _shader.Set("uMaterial.color", Material.Color);
        _shader.Set("uMaterial.shininess", Material.Shininess);
        DirectionalLight sun = SceneManager.Active.World.Sun;
        Vector3 sunColor = sun.Color.Normalize().ToVector3();
        float sunDegX = CubicMath.ToRadians(sun.Direction.X);
        float sunDegY = CubicMath.ToRadians(-sun.Direction.Y);
        _shader.Set("uSun.direction",
            new Vector3(MathF.Cos(sunDegX) * MathF.Cos(sunDegY), MathF.Cos(sunDegX) * MathF.Sin(sunDegY),
                MathF.Sin(sunDegX)));
        _shader.Set("uSun.ambient", sunColor * sun.AmbientMultiplier);
        _shader.Set("uSun.diffuse", sunColor * sun.DiffuseMultiplier);
        _shader.Set("uSun.specular", sunColor * sun.SpecularMultiplier);

        Material.Albedo.Bind(TextureUnit.Texture0);
        Material.Specular.Bind(TextureUnit.Texture1);
        
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        Gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);
        Metrics.DrawCallsInternal++;
        
        Material.Albedo.Unbind();
        Material.Specular.Unbind();
    }

    protected internal override void Unload()
    {
        base.Unload();

        if (!_shaderDisposed)
        {
            _shaderDisposed = true;
            _shader.Dispose();
        }
    }
}