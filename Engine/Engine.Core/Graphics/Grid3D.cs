/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Grid3D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      3D grid renderer for the editor                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Core.Systems;

namespace Engine.Core.Graphics
{
    /// <summary>
    /// Renders a 3D grid in the world for editor visualization
    /// </summary>
    public class Grid3D
    {
        private static Grid3D? _instance;
        public static Grid3D Instance => _instance ??= new Grid3D();

        private BasicEffect? _effect;
        private VertexBuffer? _vertexBuffer;
        private IndexBuffer? _indexBuffer;
        private bool _buffersBuilt = false;
        
        public bool Visible { get; set; } = true;
        public float GridSize { get; set; } = 4f;
        public int GridExtent { get; set; } = 20; // How many grid lines in each direction from center
        public Color GridColor { get; set; } = new Color(255, 255, 255, 80); // Semi-transparent white

        /// <summary>
        /// Initialize the grid with graphics device
        /// </summary>
        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = false,
                FogEnabled = false
            };
            
            BuildGridBuffers(graphicsDevice);
        }

        /// <summary>
        /// Build vertex and index buffers for the grid
        /// </summary>
        private void BuildGridBuffers(GraphicsDevice graphicsDevice)
        {
            var vertices = new List<VertexPositionColor>();
            var indices = new List<int>();

            // Create horizontal lines (XZ plane)
            for (int i = -GridExtent; i <= GridExtent; i++)
            {
                float pos = i * GridSize;
                
                // Line parallel to X axis
                vertices.Add(new VertexPositionColor(new Vector3(-GridExtent * GridSize, 0, pos), GridColor));
                vertices.Add(new VertexPositionColor(new Vector3(GridExtent * GridSize, 0, pos), GridColor));
                
                // Line parallel to Z axis  
                vertices.Add(new VertexPositionColor(new Vector3(pos, 0, -GridExtent * GridSize), GridColor));
                vertices.Add(new VertexPositionColor(new Vector3(pos, 0, GridExtent * GridSize), GridColor));
            }

            // Create indices for lines
            for (int i = 0; i < vertices.Count; i += 2)
            {
                indices.Add(i);
                indices.Add(i + 1);
            }

            // Build buffers
            _vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(vertices.ToArray());

            _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            _indexBuffer.SetData(indices.ToArray());

            _buffersBuilt = true;
        }

        /// <summary>
        /// Render the 3D grid
        /// </summary>
        public void Draw(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            if (!Visible || !_buffersBuilt || _effect == null || _vertexBuffer == null || _indexBuffer == null)
                return;

            // Set render states for line rendering
            var originalRasterizerState = graphicsDevice.RasterizerState;
            var originalBlendState = graphicsDevice.BlendState;
            var originalDepthStencilState = graphicsDevice.DepthStencilState;

            try
            {
                graphicsDevice.RasterizerState = RasterizerState.CullNone;
                graphicsDevice.BlendState = BlendState.AlphaBlend;
                graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

                // Set matrices
                _effect.World = Matrix.Identity;
                _effect.View = view;
                _effect.Projection = projection;

                // Set vertex buffer and index buffer
                graphicsDevice.SetVertexBuffer(_vertexBuffer);
                graphicsDevice.Indices = _indexBuffer;

                // Draw the grid
                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _indexBuffer.IndexCount / 2);
                }
            }
            finally
            {
                // Restore render states
                graphicsDevice.RasterizerState = originalRasterizerState;
                graphicsDevice.BlendState = originalBlendState;
                graphicsDevice.DepthStencilState = originalDepthStencilState;
            }
        }

        /// <summary>
        /// Update grid size and rebuild buffers if needed
        /// </summary>
        public void UpdateGridSize(float newSize, GraphicsDevice graphicsDevice)
        {
            if (Math.Abs(GridSize - newSize) > 0.01f)
            {
                GridSize = newSize;
                if (_buffersBuilt)
                {
                    BuildGridBuffers(graphicsDevice);
                }
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _effect?.Dispose();
        }
    }
}
