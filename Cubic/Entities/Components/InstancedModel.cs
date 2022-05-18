using System;
using System.Collections.Generic;
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

public class InstancedModel : Component
{
    private List<ModelGroup> _instances;
    private Shader _shader;

    public InstancedModel()
    {
        _instances = new List<ModelGroup>();
    }
    
    public unsafe ModelGroup CreateModelGroup(IPrimitive primitive, Material material)
    {
        ModelGroup group = new ModelGroup()
        {
            Vao = Gl.GenVertexArray(),
            Vbo = Gl.GenBuffer(),
            Ebo = Gl.GenBuffer(),
            IndicesLength = primitive.Indices.Length,
            Material = material,
            ModelMatrices = new List<Matrix4x4>()
        };
        
        Gl.BindVertexArray(group.Vao);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, group.Vbo);
        fixed (VertexPositionTextureNormal* vptn = primitive.Vertices)
        {
            Gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint) (primitive.Vertices.Length * sizeof(VertexPositionTextureNormal)), vptn,
                BufferUsageARB.StaticDraw);
        }
        
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, group.Ebo);
        fixed (uint* p = primitive.Indices)
        {
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (primitive.Indices.Length * sizeof(uint)), p,
                BufferUsageARB.StaticDraw);
        }

        _shader = new Shader(Model.VertexShader, Model.FragmentShader);
        
        Gl.UseProgram(_shader.Handle);
        RenderUtils.VertexAttribs(typeof(VertexPositionTextureNormal));
        
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        Gl.UseProgram(0);
        
        _instances.Add(group);

        return group;
    }

    protected internal override unsafe void Draw()
    {
        base.Draw();
        
        // I AM AWARE THIS IS NOT INSTANCING!!!!
        // THIS IS MERELY TO GET A TEST WORKING

        foreach (ModelGroup modelGroup in _instances)
        {
            Gl.BindVertexArray(modelGroup.Vao);
            Gl.UseProgram(_shader.Handle);
            _shader.Set("uProjection", Camera.Main.ProjectionMatrix);
            _shader.Set("uView", Camera.Main.ViewMatrix);
            _shader.Set("uCameraPos", Camera.Main.Transform.Position);
            _shader.Set("uMaterial.albedo", 0);
            _shader.Set("uMaterial.specular", 1);
            _shader.Set("uMaterial.color", modelGroup.Material.Color);
            _shader.Set("uMaterial.shininess", modelGroup.Material.Shininess);
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

            modelGroup.Material.Albedo.Bind(TextureUnit.Texture0);
            modelGroup.Material.Specular.Bind(TextureUnit.Texture1);

            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.LinearMipmapLinear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Linear);

            foreach (Matrix4x4 mat in modelGroup.ModelMatrices)
            {
                _shader.Set("uModel", mat * Matrix4x4.CreateFromQuaternion(Transform.Rotation) * Matrix4x4.CreateTranslation(Transform.Position));
                Gl.DrawElements(PrimitiveType.Triangles, (uint) modelGroup.IndicesLength, DrawElementsType.UnsignedInt,
                    null);
            }

            modelGroup.Material.Albedo.Unbind();
            modelGroup.Material.Specular.Unbind();
        }
    }

    protected internal override void Unload()
    {
        base.Unload();

        foreach (ModelGroup group in _instances)
        {
            Gl.DeleteVertexArray(group.Vao);
            Gl.DeleteBuffer(group.Vbo);
            Gl.DeleteBuffer(group.Ebo);
        }
    }
}

public struct ModelGroup
{ 
    internal uint Vao;
    internal uint Vbo;
    internal uint Ebo;
    internal int IndicesLength;
    public Material Material;
    public List<Matrix4x4> ModelMatrices;

    public void AddMatrix(Matrix4x4 matrix)
    {
        ModelMatrices.Add(matrix);
    }

    public void RemoveMatrix(int index)
    {
        ModelMatrices.RemoveAt(index);
    }
}