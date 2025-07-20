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

        // Keep font data pinned for ImGui lifetime
        private byte[]? _robotoFontData;
        private GCHandle _robotoFontHandle;
        public ImFontPtr _robotoFont;

        public ImGuiRenderer(Game game)
        {
            _game = game;
            _gd = game.GraphicsDevice;
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

            // Load Roboto font from file
            string fontPath = "Roboto-VariableFont_wdth.ttf";
            Console.WriteLine($"Looking for font at: {fontPath}");
            Console.WriteLine($"Font file exists: {File.Exists(fontPath)}");
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");

            if (File.Exists(fontPath))
            {
                try
                {
                    _robotoFontData = System.IO.File.ReadAllBytes(fontPath);
                    System.Diagnostics.Debug.WriteLine($"Font data loaded, size: {_robotoFontData.Length} bytes");
                    _robotoFontHandle = GCHandle.Alloc(_robotoFontData, GCHandleType.Pinned);
                    IntPtr fontPtr = _robotoFontHandle.AddrOfPinnedObject();
                    _robotoFont = fontAtlas.AddFontFromMemoryTTF(fontPtr, _robotoFontData.Length, 16.0f);
                    System.Diagnostics.Debug.WriteLine($"Font loaded successfully: {(IntPtr)_robotoFont.NativePtr != IntPtr.Zero}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading font: {ex.Message}");
                    fontAtlas.AddFontDefault();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Font file not found, using default font");
                // Fallback to default font
                fontAtlas.AddFontDefault();
            }
            
            fontAtlas.Build();
            fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            _fontTexture = new Texture2D(_gd, width, height, false, SurfaceFormat.Color);
            byte[] data = new byte[width * height * bytesPerPixel];
            Marshal.Copy((IntPtr)pixels, data, 0, data.Length);
            _fontTexture.SetData(data);
            _fontTextureId = RegisterTexture(_fontTexture);
            fontAtlas.SetTexID(_fontTextureId);
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