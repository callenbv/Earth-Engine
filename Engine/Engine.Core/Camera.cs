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

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Target != null)
            {
                Vector2 offset = new Vector2(Target.sprite.Width/2, Target.sprite.Height/2);
                Position = Vector2.Lerp(Position, Target.position+offset, SmoothSpeed * dt);
            }
        }

        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight)
        {
            return Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom, Zoom, 1f) *
                   Matrix.CreateTranslation(new Vector3(viewportWidth * 0.5f, viewportHeight * 0.5f, 0f));
        }

        public Vector2 ScreenToWorld(Point screenPos, int viewportWidth, int viewportHeight)
        {
            var viewMatrix = GetViewMatrix(viewportWidth, viewportHeight);
            var inverseViewMatrix = Matrix.Invert(viewMatrix);
            
            // Convert screen position to world position
            var screenVector = new Vector3(screenPos.X, screenPos.Y, 0f);
            var worldVector = Vector3.Transform(screenVector, inverseViewMatrix);
            
            return new Vector2(worldVector.X, worldVector.Y);
        }

        // Singleton for easy access
        private static Camera _main;
        public static Camera Main => _main ??= new Camera();
    }
} 