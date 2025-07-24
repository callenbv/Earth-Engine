/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ImGuiRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaColor = Microsoft.Xna.Framework.Color;
using System.IO;

namespace EarthEngineEditor
{
    public class ImGuiRenderer : IDisposable
    {
        private readonly Game _game;
        private readonly GraphicsDevice _gd;
        private BasicEffect _effect;
        private RasterizerState _rasterizerState;
        private Texture2D _fontTexture;
        private IntPtr _fontTextureId;
        private int _vertexBufferSize = 5000;
        private int _indexBufferSize = 10000;
        private VertexBuffer? _vertexBuffer;
        private IndexBuffer? _indexBuffer;
        private Dictionary<IntPtr, Texture2D> _textures = new();
        private int _textureId = 1;
        private KeyboardState _lastKeyboard;
        private MouseState _lastMouse;
        private int _lastScrollWheelValue = 0;
        private static Dictionary<Texture2D, IntPtr> _bindings = new();

        // Keep font data pinned for ImGui lifetime
        private byte[]? _robotoFontData;
        private GCHandle _robotoFontHandle;
        public ImFontPtr _robotoFont;
        public ImFontPtr _fontAwesome;
        public static ImGuiRenderer Instance { get; private set; }

        public ImGuiRenderer(Game game)
        {
            _game = game;
            _gd = game.GraphicsDevice;
            Instance = this;
            ImGui.CreateContext();
            
            // Apply modern dark theme
            EditorTheme.ApplyDarkTheme();
            
            SetupDeviceResources();
            BuildFontAtlas();
            SetupTextInput();
        }

        private void SetupTextInput()
        {
            // Inside your game's window or input manager
            ImGuiIOPtr io = ImGui.GetIO();

            io.AddKeyEvent(ImGuiKey.A, Keyboard.GetState().IsKeyDown(XnaKeys.A));
            io.AddKeyEvent(ImGuiKey.B, Keyboard.GetState().IsKeyDown(XnaKeys.B));

            // Forward text input (hook into your GameWindow.TextInput event)
            _game.Window.TextInput += (sender, e) =>
            {
                if (e.Character != '\t') // ImGui handles tab internally
                    io.AddInputCharacter(e.Character);
            };
        }

        public IntPtr BindTexture(Texture2D texture)
        {
            if (_bindings.TryGetValue(texture, out var handle))
                return handle;

            handle = RegisterTexture(texture);
            _bindings[texture] = handle;
            return handle;
        }

        public static bool IconButton(string id, string icon, Microsoft.Xna.Framework.Color color, float padding = 16f, float spacing = 6f)
        {
            ImGui.PushID(id);
            float fontSize = ImGui.GetFontSize();
            var textSize = ImGui.CalcTextSize(icon)/2;

            // Make the button square
            float boxSize = MathF.Max(textSize.X, textSize.Y) + padding * 2;
            var buttonSize = new System.Numerics.Vector2(boxSize, boxSize);

            var pos = ImGui.GetCursorScreenPos();
            bool clicked = ImGui.InvisibleButton("btn", buttonSize);
            bool hovered = ImGui.IsItemHovered();
            bool held = ImGui.IsItemActive();

            var drawList = ImGui.GetWindowDrawList();
            if (hovered || held)
            {
                uint bg = ImGui.GetColorU32(
                    held ? ImGuiCol.ButtonActive :
                    hovered ? ImGuiCol.ButtonHovered :
                              ImGuiCol.Button
                );
                drawList.AddRectFilled(pos, pos + buttonSize, bg, 4f);
            }

            var textPos = new System.Numerics.Vector2(
                pos.X + (boxSize - textSize.X) * 0.5f,
                pos.Y + (boxSize - textSize.Y + 4) * 0.5f
            );

            // Convert XNA color to ImGui color
            var textColor = new System.Numerics.Vector4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f
            );
            uint colorU32 = ImGui.ColorConvertFloat4ToU32(textColor);

            drawList.AddText(textPos, colorU32, icon);

            ImGui.PopID();

            return clicked;
        }



        private void SetupDeviceResources()
        {
            _effect = new BasicEffect(_gd)
            {
                VertexColorEnabled = true,
                TextureEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, _gd.Viewport.Width, _gd.Viewport.Height, 0, -1, 1)
            };
            _rasterizerState = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };
            _vertexBuffer = new VertexBuffer(_gd, typeof(VertexPositionColorTexture), _vertexBufferSize, BufferUsage.None);
            _indexBuffer = new IndexBuffer(_gd, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
        }

        private unsafe void BuildFontAtlas()
        {
            var io = ImGui.GetIO();
            var fontAtlas = io.Fonts;
            fontAtlas.Clear();

            string iconFontPath = Path.GetFullPath("fa-solid-900.ttf");
            string robotoFontPath = Path.GetFullPath("Roboto-VariableFont_wdth.ttf");

            // Load Roboto font from memory
            byte[] robotoData = File.ReadAllBytes(robotoFontPath);
            GCHandle robotoHandle = GCHandle.Alloc(robotoData, GCHandleType.Pinned);
            IntPtr robotoPtr = robotoHandle.AddrOfPinnedObject();

            // Add Roboto as base font
            _robotoFont = fontAtlas.AddFontFromMemoryTTF(robotoPtr, robotoData.Length, 16.0f);

            // Load Font Awesome font from memory
            byte[] iconData = File.ReadAllBytes(iconFontPath);
            GCHandle iconHandle = GCHandle.Alloc(iconData, GCHandleType.Pinned);
            IntPtr iconPtr = iconHandle.AddrOfPinnedObject();

            // Setup font config for merging
            ImFontConfig iconConfig = new ImFontConfig
            {
                MergeMode = 1,
                PixelSnapH = 1,
                OversampleH = 1,
                OversampleV = 1,
                RasterizerMultiply = 1.0f,
                RasterizerDensity = 1.0f,
            };

            // Define icon range (Font Awesome typically uses 0xF000–0xF8FF)
            ushort[] iconRanges = new ushort[] { 0xF000, 0xF8FF, 0 };
            fixed (ushort* rangePtr = iconRanges)
            {
                fontAtlas.AddFontFromMemoryTTF(iconPtr, iconData.Length, 32.0f, &iconConfig, (IntPtr)rangePtr);
            }

            // Build font atlas and upload texture
            fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            byte[] data = new byte[width * height * bytesPerPixel];
            Marshal.Copy((IntPtr)pixels, data, 0, data.Length);

            _fontTexture = new Texture2D(_gd, width, height, false, SurfaceFormat.Color);
            _fontTexture.SetData(data);
            _fontTextureId = RegisterTexture(_fontTexture);
            fontAtlas.SetTexID(_fontTextureId);

            // Free memory
            robotoHandle.Free();
            iconHandle.Free();

            Console.WriteLine("Font atlas built with Roboto and Font Awesome.");
        }


        public IntPtr RegisterTexture(Texture2D texture)
        {
            var id = new IntPtr(_textureId++);
            _textures[id] = texture;
            return id;
        }

        public void BeforeLayout(GameTime gameTime)
        {
            UpdateInput();
            SetPerFrameImGuiData((float)gameTime.ElapsedGameTime.TotalSeconds);
            ImGui.NewFrame();
        }

        public void AfterLayout()
        {
            ImGui.Render();
            RenderDrawData(ImGui.GetDrawData());
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(_gd.PresentationParameters.BackBufferWidth, _gd.PresentationParameters.BackBufferHeight);
            io.DeltaTime = deltaSeconds;
            
            // Update projection matrix for the new display size
            _effect.Projection = Matrix.CreateOrthographicOffCenter(0, _gd.Viewport.Width, _gd.Viewport.Height, 0, -1, 1);
        }

        private void UpdateInput()
        {
            var io = ImGui.GetIO();
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();
            io.AddMousePosEvent(mouse.X, mouse.Y);
            io.AddMouseButtonEvent(0, mouse.LeftButton == XnaButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == XnaButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == XnaButtonState.Pressed);

            // Ensure correct mouse scrolling
            int scrollDelta = mouse.ScrollWheelValue - _lastScrollWheelValue;
            float scrollDeltaNormalized = scrollDelta / 120.0f;

            io.AddMouseWheelEvent(0, scrollDeltaNormalized);
            _lastScrollWheelValue = mouse.ScrollWheelValue;

            foreach (ImGuiKey key in Enum.GetValues(typeof(ImGuiKey)))
            {
                XnaKeys? xnaKey = ImGuiKeyToXnaKey(key);
                if (xnaKey.HasValue)
                {
                    bool isDown = keyboard.IsKeyDown(xnaKey.Value);
                    io.AddKeyEvent(key, isDown);
                }
            }
            io.KeyCtrl = keyboard.IsKeyDown(XnaKeys.LeftControl) || keyboard.IsKeyDown(XnaKeys.RightControl);
            io.KeyAlt = keyboard.IsKeyDown(XnaKeys.LeftAlt) || keyboard.IsKeyDown(XnaKeys.RightAlt);
            io.KeyShift = keyboard.IsKeyDown(XnaKeys.LeftShift) || keyboard.IsKeyDown(XnaKeys.RightShift);
            io.KeySuper = keyboard.IsKeyDown(XnaKeys.LeftWindows) || keyboard.IsKeyDown(XnaKeys.RightWindows);
            _lastKeyboard = keyboard;
            _lastMouse = mouse;
        }

        private XnaKeys? ImGuiKeyToXnaKey(ImGuiKey key)
        {
            // Map only the most common keys for demo purposes
            return key switch
            {
                ImGuiKey.Tab => XnaKeys.Tab,
                ImGuiKey.LeftArrow => XnaKeys.Left,
                ImGuiKey.RightArrow => XnaKeys.Right,
                ImGuiKey.UpArrow => XnaKeys.Up,
                ImGuiKey.DownArrow => XnaKeys.Down,
                ImGuiKey.PageUp => XnaKeys.PageUp,
                ImGuiKey.PageDown => XnaKeys.PageDown,
                ImGuiKey.Home => XnaKeys.Home,
                ImGuiKey.End => XnaKeys.End,
                ImGuiKey.Delete => XnaKeys.Delete,
                ImGuiKey.Backspace => XnaKeys.Back,
                ImGuiKey.Enter => XnaKeys.Enter,
                ImGuiKey.Escape => XnaKeys.Escape,
                ImGuiKey.A => XnaKeys.A,
                ImGuiKey.C => XnaKeys.C,
                ImGuiKey.V => XnaKeys.V,
                ImGuiKey.X => XnaKeys.X,
                ImGuiKey.Y => XnaKeys.Y,
                ImGuiKey.Z => XnaKeys.Z,
                _ => null
            };
        }

        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0)
                return;
            
            var lastViewport = _gd.Viewport;
            var lastScissor = _gd.ScissorRectangle;
            _gd.RasterizerState = _rasterizerState;
            _gd.BlendState = BlendState.NonPremultiplied;
            _gd.DepthStencilState = DepthStencilState.None;
            _gd.SamplerStates[0] = SamplerState.PointClamp;
            
            var io = ImGui.GetIO();
            drawData.ScaleClipRects(io.DisplayFramebufferScale);
            
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                int vtxCount = cmdList.VtxBuffer.Size;
                int idxCount = cmdList.IdxBuffer.Size;
                
                if (vtxCount > _vertexBufferSize)
                {
                    _vertexBuffer?.Dispose();
                    _vertexBufferSize = vtxCount + 5000;
                    _vertexBuffer = new VertexBuffer(_gd, typeof(VertexPositionColorTexture), _vertexBufferSize, BufferUsage.None);
                }
                if (idxCount > _indexBufferSize)
                {
                    _indexBuffer?.Dispose();
                    _indexBufferSize = idxCount + 10000;
                    _indexBuffer = new IndexBuffer(_gd, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
                }
                
                var vertices = new VertexPositionColorTexture[vtxCount];
                for (int i = 0; i < vtxCount; i++)
                {
                    var v = cmdList.VtxBuffer[i];
                    vertices[i].Position = new Microsoft.Xna.Framework.Vector3(v.pos.X, v.pos.Y, 0f);
                    vertices[i].Color = new XnaColor(
                        (byte)(v.col & 0xFF),
                        (byte)((v.col >> 8) & 0xFF),
                        (byte)((v.col >> 16) & 0xFF),
                        (byte)((v.col >> 24) & 0xFF));
                    vertices[i].TextureCoordinate = new Microsoft.Xna.Framework.Vector2(v.uv.X, v.uv.Y);
                }
                
                ushort[] indices = new ushort[idxCount];
                for (int i = 0; i < idxCount; i++)
                    indices[i] = (ushort)cmdList.IdxBuffer[i];
                
                _vertexBuffer?.SetData(vertices, 0, vtxCount);
                _indexBuffer?.SetData(indices, 0, idxCount);
                if (_vertexBuffer != null)
                    _gd.SetVertexBuffer(_vertexBuffer);
                if (_indexBuffer != null)
                    _gd.Indices = _indexBuffer;
                
                int idxOffset = 0;
                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                {
                    var pcmd = cmdList.CmdBuffer[cmdi];
                    if (!_textures.TryGetValue(pcmd.TextureId, out var tex))
                        tex = _fontTexture;
                    
                    _gd.ScissorRectangle = new Microsoft.Xna.Framework.Rectangle(
                        (int)pcmd.ClipRect.X,
                        (int)pcmd.ClipRect.Y,
                        (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
                    
                    _effect.Texture = tex;
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, idxOffset, (int)pcmd.ElemCount / 3);
                    }
                    idxOffset += (int)pcmd.ElemCount;
                }
            }
            
            _gd.Viewport = lastViewport;
            _gd.ScissorRectangle = lastScissor;
        }

        public void Dispose()
        {
            _fontTexture?.Dispose();
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _effect?.Dispose();
            if (_robotoFontHandle.IsAllocated) _robotoFontHandle.Free();
        }

        public bool HasCustomFont => _robotoFontData != null && _robotoFontData.Length > 0;
    }
} 
