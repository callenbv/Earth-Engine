/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         MeshRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Renders a 3D mesh using BasicEffect. Attach to prefabs.          
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Graphics;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("Graphics")]
    public class MeshRenderer : ObjectComponent
    {
        public override string Name => "Mesh Renderer";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// Mesh asset name (file name without extension).
        /// </summary>
        private string _meshName = string.Empty;
        public string Mesh
        {
            get => _meshName;
            set
            {
                if (_meshName != value)
                {
                    _meshName = value;
                    _buffersBuilt = false; // trigger rebuild on next draw
                }
            }
        }

        /// <summary>
        /// Material asset name (file name without extension).
        /// </summary>
        public string Material { get; set; } = string.Empty;

        /// <summary>
        /// 3D scale multiplier (applied on top of Transform.Scale).
        /// </summary>
        public Vector3 Scale3D { get; set; } = Vector3.One;





        private VertexBuffer? _vertexBuffer;
        private IndexBuffer? _indexBuffer;
        private int _indexCount = 0;
        private BasicEffect? _effect;
        private bool _buffersBuilt = false;
        private HashSet<string> _loggedMeshes = new HashSet<string>();

        public override void Initialize()
        {
            BuildBuffers();
        }

        public override void Destroy()
        {
            base.Destroy();
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _vertexBuffer = null;
            _indexBuffer = null;
            _effect?.Dispose();
            _effect = null;
        }

        private void BuildBuffers()
        {
            var gd = GraphicsDevice;
            if (gd == null) 
            {
                Console.WriteLine($"[MeshRenderer] GraphicsDevice is null for {Name}");
                return;
            }

            var meshData = MeshLibrary.GetMesh(Mesh);
            if (meshData == null)
            {
                Console.WriteLine($"[MeshRenderer] Mesh '{Mesh}' not found in MeshLibrary");
                return;
            }
            
            if (meshData.Positions == null || meshData.Indices == null)
            {
                Console.WriteLine($"[MeshRenderer] Mesh '{Mesh}' has null positions or indices");
                return;
            }

            Console.WriteLine($"[MeshRenderer] Building buffers for mesh '{Mesh}' with {meshData.Positions.Length / 3} vertices");

            int vertexCount = meshData.Positions.Length / 3;
            var vertices = new VertexPositionNormalTexture[vertexCount];

            // Fill vertices
            for (int i = 0; i < vertexCount; i++)
            {
                float px = meshData.Positions[i * 3 + 0];
                float py = meshData.Positions[i * 3 + 1];
                float pz = meshData.Positions[i * 3 + 2];

                float nx = 0f, ny = 1f, nz = 0f;
                if (meshData.Normals != null && meshData.Normals.Length >= (i * 3 + 3))
                {
                    nx = meshData.Normals[i * 3 + 0];
                    ny = meshData.Normals[i * 3 + 1];
                    nz = meshData.Normals[i * 3 + 2];
                }

                float u = 0f, v = 0f;
                if (meshData.UV0 != null && meshData.UV0.Length >= (i * 2 + 2))
                {
                    u = meshData.UV0[i * 2 + 0];
                    v = meshData.UV0[i * 2 + 1];
                }

                vertices[i] = new VertexPositionNormalTexture(new Vector3(px, py, pz), new Vector3(nx, ny, nz), new Vector2(u, v));
            }

            int[] indices = meshData.Indices;
            _indexCount = indices.Length;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _vertexBuffer = new VertexBuffer(gd, typeof(VertexPositionNormalTexture), vertexCount, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, _indexCount, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(vertices);
            _indexBuffer.SetData(indices);

            _effect?.Dispose();
            _effect = new BasicEffect(gd)
            {
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                TextureEnabled = false, // Will be set later based on material
                DiffuseColor = new Vector3(1f, 1f, 1f),
                AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f) // Some ambient light
            };

            // Try to load material if specified
            if (!string.IsNullOrEmpty(Material))
            {
                var materialData = MeshLibrary.GetMaterial(Material);
                if (materialData != null)
                {
                    if (materialData.AlbedoColor != null && materialData.AlbedoColor.Length >= 3)
                    {
                        _effect.DiffuseColor = new Vector3(
                            materialData.AlbedoColor[0], 
                            materialData.AlbedoColor[1], 
                            materialData.AlbedoColor[2]);
                    }

                    // TODO: Load and apply textures when texture system is ready
                    // if (!string.IsNullOrEmpty(materialData.AlbedoTexture))
                    // {
                    //     var texture = TextureLibrary.Instance.Get(Path.GetFileNameWithoutExtension(materialData.AlbedoTexture));
                    //     if (texture != null)
                    //     {
                    //         _effect.Texture = texture;
                    //         _effect.TextureEnabled = true;
                    //     }
                    // }
                }
                else
                {
                    Console.WriteLine($"[MeshRenderer] Material '{Material}' not found in MeshLibrary");
                }
            }

            _buffersBuilt = true;
        }

        public void Draw3D(GraphicsDevice gd, Matrix view, Matrix projection)
        {
            if (!_buffersBuilt) BuildBuffers();
            if (_vertexBuffer == null || _indexBuffer == null || _effect == null) 
            {
                if (!_buffersBuilt)
                    Console.WriteLine($"[MeshRenderer] Buffers not built for {Name}");
                return;
            }

            // Get Transform component for position, rotation, scale
            var transform = Owner?.GetComponent<Transform>();
            Vector3 position = transform?.Position ?? new Vector3(Position.X, Position.Y, 0f);
            float rotation2D = transform?.Rotation ?? 0f;
            Vector3 scale3D = transform?.Scale ?? Vector3.One;

            // World from Transform properties (pixels -> world units) + rotation + scale
            Vector3 pos = new Vector3(position.X / EngineContext.UnitsPerPixel, -position.Y / EngineContext.UnitsPerPixel, position.Z / EngineContext.UnitsPerPixel);
            Vector3 finalScale = new Vector3(scale3D.X * Scale3D.X, scale3D.Y * Scale3D.Y, scale3D.Z * Scale3D.Z);
            
            Matrix world = Matrix.CreateScale(finalScale) *
                           Matrix.CreateRotationZ(MathHelper.ToRadians(rotation2D)) *
                           Matrix.CreateTranslation(pos);

            _effect.World = world;
            _effect.View = view;
            _effect.Projection = projection;

            // Debug logging for mesh positioning (only log once per mesh)
            if (!_loggedMeshes.Contains(Mesh))
            {
                _loggedMeshes.Add(Mesh);
                var meshBounds = MeshLibrary.GetMesh(Mesh)?.Bounds;
                if (meshBounds.HasValue)
                {
                    var b = meshBounds.Value;
                }
            }
            
            // Configure lighting and material
            if (_effect.LightingEnabled)
            {
                _effect.EnableDefaultLighting();
                _effect.DirectionalLight0.Enabled = true;
                _effect.DirectionalLight0.DiffuseColor = Vector3.One;
                _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.5f));
                _effect.DirectionalLight0.SpecularColor = Vector3.One * 0.5f;
            }

            gd.SetVertexBuffer(_vertexBuffer);
            gd.Indices = _indexBuffer;

            // Set wireframe mode if globally enabled
            var previousRasterizer = gd.RasterizerState;
            bool globalWireframe = EngineContext.Wireframe;
            if (globalWireframe)
            {
                var wireframeState = new RasterizerState
                {
                    FillMode = FillMode.WireFrame,
                    CullMode = CullMode.None
                };
                gd.RasterizerState = wireframeState;
            }

            try
            {
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshRenderer] Error drawing mesh '{Mesh}': {ex.Message}");
            }
            finally
            {
                // Restore previous rasterizer state
                if (globalWireframe)
                {
                    gd.RasterizerState = previousRasterizer;
                }
            }
        }
    }
}


