using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;

namespace Cubic.Render;

public class ImGuiRenderer : IDisposable
{
    private int _windowWidth;
    private int _windowHeight;

    private bool _frameBegun;

    private uint _vao;
    private uint _vbo;
    private uint _ebo;

    private uint _vboSize;
    private uint _eboSize;

    private Shader _shader;

    private Texture2D _fontTexture;

    public Vector2 Scale;

    private readonly List<char> _pressedChars;

    private Keys[] _keysList;

    private Dictionary<string, ImFontPtr> _fonts;

    internal ImGuiRenderer(Graphics graphics)
    {
        Scale = Vector2.One;
        _fonts = new Dictionary<string, ImFontPtr>();

        _windowWidth = graphics.Viewport.Width;
        _windowHeight = graphics.Viewport.Height;
        
        graphics.ViewportResized += WindowOnResize;
        Input.TextInput += PressChar;

        _pressedChars = new List<char>();
        _keysList = (Keys[])Enum.GetValues(typeof(Keys));

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);
        
        ImGui.NewFrame();
        _frameBegun = true;
    }

    private void WindowOnResize(Size size)
    {
        _windowWidth = size.Width;
        _windowHeight = size.Height;
    }

    private unsafe void CreateDeviceResources()
    {
        _vao = Gl.GenVertexArray();
        
        _vboSize = 10000;
        _eboSize = 2000;

        _vbo = Gl.GenBuffer();
        _ebo = Gl.GenBuffer();
        
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, _vboSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _ebo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, _eboSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        RecreateFontDeviceTexture();

        const string vertexSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec4 aColor;

out vec4 frag_color;
out vec2 frag_texCoords;

uniform mat4 uProjection;

void main()
{
gl_Position = uProjection * vec4(aPosition, 0, 1);
frag_color = aColor;
frag_texCoords = aTexCoords;
}";

        const string fragmentSource = @"
#version 330 core
in vec4 frag_color;
in vec2 frag_texCoords;

out vec4 out_color;

uniform sampler2D uTexture;

void main()
{
out_color = frag_color * texture(uTexture, frag_texCoords);
}";

        _shader = new Shader(vertexSource, fragmentSource);
        Gl.UseProgram(_shader.Handle);
        
        Gl.BindVertexArray(_vao);
        
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        uint stride = (uint) Unsafe.SizeOf<ImDrawVert>();

        uint vertexLocation = 0;
        Gl.EnableVertexAttribArray(vertexLocation);
        Gl.VertexAttribPointer(vertexLocation, 2, VertexAttribPointerType.Float, false, stride, (void*) 0);

        uint texCoordLocation = 1;
        Gl.EnableVertexAttribArray(texCoordLocation);
        Gl.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, stride, (void*) 8);

        uint colorLocation = 2;
        Gl.EnableVertexAttribArray(colorLocation);
        Gl.VertexAttribPointer(colorLocation, 4, VertexAttribPointerType.UnsignedByte, true, stride, (void*) 16);
        
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    private void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        _fontTexture = new Texture2D(width, height, false);
        _fontTexture.SetData(pixels, 0, 0, width, height);
        
        io.Fonts.SetTexID((IntPtr) _fontTexture.Handle);
        
        io.Fonts.ClearTexData();
    }

    internal void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    internal void Update(float deltaSeconds)
    {
        if (_frameBegun)
            ImGui.Render();

        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput();

        _frameBegun = true;
        
        ImGui.NewFrame();
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth / Scale.X, _windowHeight / Scale.Y);
        io.DisplayFramebufferScale = Scale;
        io.DeltaTime = deltaSeconds;
    }

    private void UpdateImGuiInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.MouseDown[0] = Input.MouseButtonDown(MouseButtons.Left);
        io.MouseDown[1] = Input.MouseButtonDown(MouseButtons.Right);
        io.MouseDown[2] = Input.MouseButtonDown(MouseButtons.Middle);

        io.MousePos = Input.MousePosition / Scale;

        io.MouseWheel = Input.ScrollWheelDelta.Y;
        io.MouseWheelH = Input.ScrollWheelDelta.X;

        foreach (Keys key in _keysList)
        {
            if ((int) key > 0)
                io.KeysDown[(int) key] = Input.KeyDown(key);
        }
        
        foreach (char c in _pressedChars)
            io.AddInputCharacter(c);
        _pressedChars.Clear();
        
        io.KeyCtrl = Input.KeyDown(Keys.LeftControl) || Input.KeyDown(Keys.RightControl);
        io.KeyAlt = Input.KeyDown(Keys.LeftAlt) || Input.KeyDown(Keys.RightAlt);
        io.KeyShift = Input.KeyDown(Keys.LeftShift) || Input.KeyDown(Keys.RightShift);
        io.KeySuper = Input.KeyDown(Keys.LeftSuper) || Input.KeyDown(Keys.RightSuper);
    }

    private void PressChar(char chr)
    {
        _pressedChars.Add(chr);
    }

    private static void SetKeyMappings()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
        io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
            return;
        
        uint totalVbSize = (uint) (drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVbSize > _vboSize)
        {
            _vboSize = (uint) Math.Max(_vboSize * 1.5f, totalVbSize);
            
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, _vboSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        uint totalIbSize = (uint) (drawData.TotalIdxCount * sizeof(ushort));
        if (totalIbSize > _eboSize)
        {
            _eboSize = (uint) Math.Max(_eboSize * 1.5f, totalIbSize);
            
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _ebo);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, _eboSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];
            
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            Gl.BufferSubData(BufferTargetARB.ArrayBuffer,
                (nint) (vertexOffsetInVertices * Unsafe.SizeOf<ImDrawVert>()), (nuint) (cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()), cmdList.VtxBuffer.Data.ToPointer());
            
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _ebo);
            Gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint) (indexOffsetInElements * sizeof(ushort)),
                (nuint) (cmdList.IdxBuffer.Size * sizeof(ushort)), cmdList.IdxBuffer.Data.ToPointer());

            vertexOffsetInVertices += (uint) cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint) cmdList.IdxBuffer.Size;
        }
        
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        ImGuiIOPtr io = ImGui.GetIO();

        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1, 1);
        Gl.UseProgram(_shader.Handle);
        _shader.Set("uProjection", mvp, false);
        _shader.Set("uTexture", 0);
        
        Gl.BindVertexArray(_vao);
        
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        bool wasBlendEnabled = Gl.IsEnabled(EnableCap.Blend);
        bool wasScissorEnabled = Gl.IsEnabled(EnableCap.ScissorTest);
        bool wasCullingEnabled = Gl.IsEnabled(EnableCap.CullFace);
        bool wasDepthTestEnabled = Gl.IsEnabled(EnableCap.DepthTest);
        
        Gl.Enable(EnableCap.Blend);
        Gl.Enable(EnableCap.ScissorTest);
        Gl.BlendEquation(GLEnum.FuncAdd);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.Disable(EnableCap.CullFace);
        Gl.Disable(EnableCap.DepthTest);

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];
            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException();
                
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, _fontTexture.Handle);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMinFilter.Linear);

                Vector4 clipRect = pcmd.ClipRect;
                Gl.Scissor((int) clipRect.X, _windowHeight - (int) clipRect.W, (uint) (clipRect.Z - clipRect.X),
                    (uint) (clipRect.W - clipRect.Y));

                Gl.DrawElementsBaseVertex(PrimitiveType.Triangles,  pcmd.ElemCount,
                    DrawElementsType.UnsignedShort, (void*) (idxOffset * sizeof(ushort)), vtxOffset);

                idxOffset += (int) pcmd.ElemCount;
            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
        
        if (!wasBlendEnabled)
            Gl.Disable(EnableCap.Blend);
        if (!wasScissorEnabled)
            Gl.Disable(EnableCap.ScissorTest);
        if (wasCullingEnabled)
            Gl.Enable(EnableCap.CullFace);
        if (wasDepthTestEnabled)
            Gl.Enable(EnableCap.DepthTest);
    }
    
    public void Dispose()
    {
        Gl.DeleteVertexArray(_vbo);
        Gl.DeleteBuffer(_vbo);
        Gl.DeleteBuffer(_ebo);
        _fontTexture.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }

    public void AddFont(string name, string path, int size)
    {
        if (_fonts.ContainsKey(name))
            return;
        _fonts.Add(name, ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size));
        RecreateFontDeviceTexture();
    }

    public void SetFont(string name)
    {
        ImGui.PushFont(_fonts[name]);
    }

    public void ResetFont()
    {
        ImGui.PopFont();
    }
}