/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Camera.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    /// <summary>
    /// Represents the camera in the game, handling position, zoom, rotation, and target tracking.
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// The position of the camera in world space.
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// The zoom level of the camera, where 1f is normal size.
        /// </summary>
        public float Zoom { get; set; } = 1f;

        /// <summary>
        /// The rotation of the camera in radians. 0f means no rotation.
        /// </summary>
        public float Rotation { get; set; } = 0f;

        /// <summary>
        /// The target GameObject that the camera will follow. If null, the camera does not follow any object.
        /// </summary>
        public GameObject? Target { get; set; } = null;

        public GraphicsDevice graphicsDevice { get; set; } = null;

        /// <summary>
        /// The speed at which the camera smoothly follows the target. Higher values result in faster following.
        /// </summary>
        public float SmoothSpeed { get; set; } = 8f;
        public float UIScale { get; set; } = 1f;
        public int ViewportWidth { get; set; } = 320;
        public int ViewportHeight { get; set; } = 180;
        public int TargetViewportHeight { get; set; } = 180;
        public int TargetViewportWidth { get; set; } = 320;
        public int UIWidth { get; set; } = 320;
        public int UIHeight { get; set; } = 180;
        public Vector2 EditorPositon = Vector2.Zero;
        public float UIEditorZoom = 0.5f;

        private static Camera? _main;
        public static Camera Main => _main ??= new Camera();

        /// <summary>
        /// Reset the camera settings
        /// </summary>
        public void Reset()
        {
            Target = null;
            Zoom = 1f;
            Rotation = 0f;
        }

        /// <summary>
        /// Update the camera position based on the target GameObject.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            Math.Clamp(Zoom, 0.1f, 4f);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Target != null)
            {
                Position = Vector2.Lerp(Position, Target.Position, SmoothSpeed * dt);
            }
        }

        /// <summary>
        /// Get the view matrix for rendering, taking into account zoom, rotation, and position.
        /// </summary>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <returns></returns>
        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight)
        {
            // Calculate scale factor from base resolution to actual rendering resolution
            float scaleX = (float)viewportWidth / TargetViewportWidth;
            float scaleY = (float)viewportHeight / TargetViewportHeight;
            float scale = Math.Min(scaleX, scaleY);
            
            // Apply pixel snapping at the base resolution, then scale up
            float snappedX = (float)Math.Round(Position.X * scale) / scale;
            float snappedY = (float)Math.Round(Position.Y * scale) / scale;
            Vector3 Translation = new Vector3(-snappedX, -snappedY, 0f);

            // Create transform at base resolution, then scale to internal resolution
            Matrix baseTransform = Matrix.CreateTranslation(Translation) *
                                 Matrix.CreateRotationZ(Rotation) *
                                 Matrix.CreateScale(Zoom, Zoom, 1f) *
                                 Matrix.CreateTranslation(new Vector3(TargetViewportWidth * 0.5f, TargetViewportHeight * 0.5f, 0f));
            
            Matrix scaleTransform = Matrix.CreateScale(scale, scale, 1f);
            return baseTransform * scaleTransform;
        }

        /// <summary>
        /// Get the UI view matrix, which is typically an identity matrix since UI is not affected by camera transformations.
        /// </summary>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <returns></returns>
        public Matrix GetUIViewMatrix(int viewportWidth, int viewportHeight, bool forEditor = false)
        {
            forEditor = !EngineContext.Running;
            if (!forEditor)
            {
                return GetUIScreenFitMatrix(ViewportWidth, ViewportHeight); // In-game UI
            }

            return GetViewMatrix(viewportWidth,viewportHeight);
        }

        /// <summary>
        /// Get a matrix that fits the UI to the screen size, maintaining aspect ratio.
        /// </summary>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <returns></returns>
        public Matrix GetUIScreenFitMatrix(int viewportWidth, int viewportHeight)
        {
            float scaleX = (float)viewportWidth / UIWidth;
            float scaleY = (float)viewportHeight / UIHeight;
            float scale = Math.Min(scaleX, scaleY);

            float offsetX = (viewportWidth - UIWidth * scale) * 0.5f;
            float offsetY = (viewportHeight - UIHeight * scale) * 0.5f;

            return Matrix.CreateTranslation(offsetX, offsetY, 0f) *
                   Matrix.CreateScale(scale, scale, 1f);
        }

        /// <summary>
        /// Convert a screen position to world coordinates.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public Vector3 ScreenToWorld(Point screenPos)
        {
            int screenW = ViewportWidth;
            int screenH = ViewportHeight;

            // 1. Match the Draw() code: get scale and offset
            float scaleX = (float)screenW / EngineContext.InternalWidth;
            float scaleY = (float)screenH / EngineContext.InternalHeight;
            float scale = Math.Min(scaleX, scaleY);

            float offsetX = (screenW - EngineContext.InternalWidth * scale) * 0.5f;
            float offsetY = (screenH - EngineContext.InternalHeight * scale) * 0.5f;

            // 2. Convert mouse screen pos → internal render target space
            float internalX = (screenPos.X - offsetX) / scale;
            float internalY = (screenPos.Y - offsetY) / scale;

            // 3. Convert internal point → world space
            Vector3 internalVec = new Vector3(internalX, internalY, 0f);
            Matrix view = GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight);
            Matrix inverse = Matrix.Invert(view);
            Vector3 worldVec = Vector3.Transform(internalVec, inverse);

            return worldVec; // Return full Vector3 instead of just X,Y
        }

        /// <summary>
        /// Set the viewport size for rendering.
        /// </summary>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        public void SetViewportSize(int viewportWidth, int viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }

        /// <summary>
        /// Draw the camera view preview. Used for knowing the UI speed
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawUI(SpriteBatch spriteBatch)
        {
            if (EngineContext.Running)
                return;

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, GetUIViewMatrix(ViewportWidth, ViewportHeight, true));

            Texture2D pixel = GraphicsLibrary.PixelTexture;
            Rectangle border = new Rectangle(0, 0, UIWidth, UIHeight);
            Color color = Color.White;
            int thickness = 2; // Thickness of the outline

            // Top
            //spriteBatch.Draw(pixel, new Rectangle(border.X, border.Y, border.Width, thickness), color);
            //// Bottom,
            //spriteBatch.Draw(pixel, new Rectangle(border.X, border.Y + border.Height - thickness, border.Width, thickness), color);
            //// Left
            //spriteBatch.Draw(pixel, new Rectangle(border.X, border.Y, thickness, border.Height), color);
            //// Right
            //spriteBatch.Draw(pixel, new Rectangle(border.X + border.Width - thickness, border.Y, thickness, border.Height), color);

            spriteBatch.End();
        }
    }
} 
