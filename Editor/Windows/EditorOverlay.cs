/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorOverlay.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Windows
{
    /// <summary>
    /// EditorOverlay provides an overlay for the editor, including a grid and camera controls.
    /// </summary>
    public class EditorOverlay
    {
        public static EditorOverlay Instance { get; private set; }
        public bool showGrid = true;
        public int gridSize;
        private Texture2D gridTexture;
        private GraphicsDevice graphicsDevice;
        public int PanSpeed = 3;

        /// <summary>
        /// Initialize the editor overlay with the graphics device.
        /// </summary>
        /// <param name="graphicsDevice_"></param>
        public EditorOverlay(GraphicsDevice graphicsDevice_)
        {
            Instance = this;
            graphicsDevice = graphicsDevice_;
            gridTexture = new Texture2D(graphicsDevice, 1, 1);
            gridTexture.SetData(new[] { Microsoft.Xna.Framework.Color.White });
        }

        /// <summary>
        /// Update the editor overlay (Scene controls, etc.)
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Handle camera panning and zooming
            if (EditorApp.Instance.gameFocused && !EngineContext.Running)
            {
                //// Panning
                //if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                //{
                //    Camera.Main.Position += new Vector2(0, -PanSpeed) / Camera.Main.Zoom;
                //}
                //if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                //{
                //    Camera.Main.Position += new Vector2(0, PanSpeed) / Camera.Main.Zoom;
                //}
                //if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                //{
                //    Camera.Main.Position += new Vector2(-PanSpeed, 0) / Camera.Main.Zoom;
                //}
                //if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                //{
                //    Camera.Main.Position += new Vector2(PanSpeed, 0) / Camera.Main.Zoom;
                //}
                //// Zooming
                //if (Input.ScrolledUp)
                //{
                //    Camera.Main.Zoom *= 1.1f; // Zoom in
                //}
                //if (Input.ScrolledDown)
                //{
                //    Camera.Main.Zoom *= 0.9f; // Zoom in
                //}
            }
        }

        /// <summary>
        /// Draw the editor overlay (grid, etc.)
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawEnd(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, Camera.Main.GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight));
            //DrawGrid(spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Draw the world grid over the editor preview
        /// </summary>
        /// <param name="spriteBatch"></param>
        private void DrawGrid(SpriteBatch spriteBatch)
        {
            if (showGrid)
            {
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets))
                {
                    SceneViewWindow.gridSize += 4;
                }
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets))
                {
                    SceneViewWindow.gridSize -= 4;
                }
                SceneViewWindow.gridSize = Math.Clamp(SceneViewWindow.gridSize, 1, 64); // Clamp grid size to a reasonable range
                gridSize = SceneViewWindow.gridSize;

                if (gridSize <= 1)
                    return;

                Microsoft.Xna.Framework.Color gridColor = new Microsoft.Xna.Framework.Color(255, 255, 255, 50);
                float zoom = Camera.Main.Zoom;
                Vector2 camPos = Camera.Main.Position; // Use the actual camera position

                float viewWidth = Camera.Main.ViewportWidth / zoom;
                float viewHeight = Camera.Main.ViewportHeight / zoom;

                float worldLeft = camPos.X - viewWidth / 2f;
                float worldRight = camPos.X + viewWidth / 2f;
                float worldTop = camPos.Y - viewHeight / 2f;
                float worldBottom = camPos.Y + viewHeight / 2f;
    
                // Clamp to nearest grid lines
                int startX = (int)Math.Floor(worldLeft / gridSize) * gridSize;
                int endX = (int)Math.Ceiling(worldRight / gridSize) * gridSize;
                int startY = (int)Math.Floor(worldTop / gridSize) * gridSize;
                int endY = (int)Math.Ceiling(worldBottom / gridSize) * gridSize;
                Vector2 origin = Vector2.Zero;

                // Vertical lines
                for (int x = startX; x <= endX; x += gridSize)
                {
                    spriteBatch.Draw(
                        gridTexture,
                        new Microsoft.Xna.Framework.Rectangle(x, startY, 1, endY - startY),
                        null,
                        gridColor,
                        0f,
                        origin,
                        SpriteEffects.None,
                        0f
                    );
                }

                // Horizontal lines
                for (int y = startY; y <= endY; y += gridSize)
                {
                    spriteBatch.Draw(
                        gridTexture,
                        new Microsoft.Xna.Framework.Rectangle(startX, y, endX - startX, 1),
                        null,
                        gridColor,
                        0f,
                        origin,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }
    }
}

