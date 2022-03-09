using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cubic2D.Windowing;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;

namespace Cubic2D.Render;

internal class ImGuiRenderer : IDisposable
{
    private int _windowWidth;
    private int _windowHeight;

    private bool _frameBegun;

    private int _vao;
    private int _vbo;
    private int _ebo;

    private int _vboSize;
    private int _eboSize;

    private Shader _shader;

    private Texture2D _fontTexture;

    public Vector2 ScaleFactor;

    private readonly List<char> _pressedChars;

    private Keys[] _keysList;

    public ImGuiRenderer(Graphics graphics)
    {
        ScaleFactor = Vector2.One;

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

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources()
    {
        _vao = GL.GenVertexArray();
        
        _vboSize = 10000;
        _eboSize = 2000;

        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vboSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ArrayBuffer, _eboSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

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
        GL.UseProgram(_shader.Handle);
        
        GL.BindVertexArray(_vao);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

        int stride = Unsafe.SizeOf<ImDrawVert>();

        int vertexLocation = 0;
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 2, VertexAttribPointerType.Float, false, stride, 0);

        int texCoordLocation = 1;
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, stride, 8);

        int colorLocation = 2;
        GL.EnableVertexAttribArray(colorLocation);
        GL.VertexAttribPointer(colorLocation, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);
        
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        _fontTexture = new Texture2D(width, height, false);
        _fontTexture.SetData(pixels, 0, 0, (uint) width, (uint) height);
        
        io.Fonts.SetTexID((IntPtr) _fontTexture.Handle);
        
        io.Fonts.ClearTexData();
    }

    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    public void Update(float deltaSeconds)
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
        io.DisplaySize = new Vector2(_windowWidth / ScaleFactor.X, _windowHeight / ScaleFactor.Y);
        io.DisplayFramebufferScale = ScaleFactor;
        io.DeltaTime = deltaSeconds;
        Console.WriteLine($"{_windowWidth}, {_windowHeight}");
    }

    private void UpdateImGuiInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.MouseDown[0] = Input.MouseButtonDown(MouseButtons.Left);
        io.MouseDown[1] = Input.MouseButtonDown(MouseButtons.Right);
        io.MouseDown[2] = Input.MouseButtonDown(MouseButtons.Middle);

        io.MousePos = Input.MousePosition / ScaleFactor;

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

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
            return;
        
        uint totalVbSize = (uint) (drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVbSize > _vboSize)
        {
            _vboSize = (int) Math.Max(_vboSize * 1.5f, totalVbSize);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vboSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        uint totalIbSize = (uint) (drawData.TotalIdxCount * sizeof(ushort));
        if (totalIbSize > _eboSize)
        {
            _eboSize = (int) Math.Max(_eboSize * 1.5f, totalIbSize);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ArrayBuffer, _eboSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer,
                (IntPtr) (vertexOffsetInVertices * Unsafe.SizeOf<ImDrawVert>()),
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _ebo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr) (indexOffsetInElements * sizeof(ushort)),
                cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data);

            vertexOffsetInVertices += (uint) cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint) cmdList.IdxBuffer.Size;
        }
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        ImGuiIOPtr io = ImGui.GetIO();

        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1, 1);
        GL.UseProgram(_shader.Handle);
        _shader.Set("uProjection", mvp, false);
        _shader.Set("uTexture", 0);
        
        GL.BindVertexArray(_vao);
        
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        bool wasBlendEnabled = GL.IsEnabled(EnableCap.Blend);
        bool wasScissorEnabled = GL.IsEnabled(EnableCap.ScissorTest);
        bool wasCullingEnabled = GL.IsEnabled(EnableCap.CullFace);
        bool wasDepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
        
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

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
                
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _fontTexture.Handle);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMinFilter.Linear);

                Vector4 clipRect = pcmd.ClipRect;
                GL.Scissor((int) clipRect.X, _windowHeight - (int) clipRect.W, (int) (clipRect.Z - clipRect.X),
                    (int) (clipRect.W - clipRect.Y));

                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int) pcmd.ElemCount,
                    DrawElementsType.UnsignedShort, (IntPtr) (idxOffset * sizeof(ushort)), vtxOffset);

                idxOffset += (int) pcmd.ElemCount;
            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
        
        if (!wasBlendEnabled)
            GL.Disable(EnableCap.Blend);
        if (!wasScissorEnabled)
            GL.Disable(EnableCap.ScissorTest);
        if (wasCullingEnabled)
            GL.Enable(EnableCap.CullFace);
        if (wasDepthTestEnabled)
            GL.Enable(EnableCap.DepthTest);
    }
    
    public void Dispose()
    {
        GL.DeleteVertexArray(_vbo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        _fontTexture.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
}