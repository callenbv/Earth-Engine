using Engine.Core.CustomMath;
using Engine.Core.Game;
using Microsoft.Xna.Framework;

namespace Engine.Core
{
    public class Camera
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Zoom { get; set; } = 1f;
        public float Rotation { get; set; } = 0f;
        public GameObject? Target { get; set; } = null;
        public float SmoothSpeed { get; set; } = 8f;
        public float UIScale { get; set; } = 1f;
        
        // Viewport settings
        public int ViewportWidth { get; set; } = 384;
        public int ViewportHeight { get; set; } = 216;
        public int TargetViewportWidth { get; set; } = 384;
        public int TargetViewportHeight { get; set; } = 216;

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Target != null)
            {
                Position = Vector2.Lerp(Position, Target.Position, SmoothSpeed * dt);
            }
        }

        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight)
        {
            // Calculate scale factor from base resolution to actual rendering resolution
            float scaleX = (float)viewportWidth / TargetViewportWidth;
            float scaleY = (float)viewportHeight / TargetViewportHeight;
            float scale = Math.Min(scaleX, scaleY);
            
            // Apply pixel snapping at the base resolution, then scale up
            Vector3 Translation = new Vector3(-Position.X, -Position.Y, 0f);

            // Create transform at base resolution, then scale to internal resolution
            Matrix baseTransform = Matrix.CreateTranslation(Translation) *
                                 Matrix.CreateRotationZ(Rotation) *
                                 Matrix.CreateScale(Zoom, Zoom, 1f) *
                                 Matrix.CreateTranslation(new Vector3(TargetViewportWidth * 0.5f, TargetViewportHeight * 0.5f, 0f));
            
            Matrix scaleTransform = Matrix.CreateScale(scale, scale, 1f);
            
            return baseTransform * scaleTransform;
        }

        public Matrix GetUIViewMatrix(int viewportWidth, int viewportHeight)
        {
            return Matrix.Identity;
        }

        public Vector2 ScreenToWorld(Point screenPos)
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

            return new Vector2(worldVec.X, worldVec.Y);
        }


        public void SetViewportSize(int viewportWidth, int viewportHeight)
        {
            // Don't update the actual viewport size - we always use target viewport size for rendering
            // This method is kept for compatibility but doesn't affect the camera calculations
        }

        public void SetTargetViewportSize(int targetWidth, int targetHeight)
        {
            TargetViewportWidth = targetWidth;
            TargetViewportHeight = targetHeight;
        }

        // Singleton for easy access
        private static Camera? _main;
        public static Camera Main => _main ??= new Camera();
    }
} 