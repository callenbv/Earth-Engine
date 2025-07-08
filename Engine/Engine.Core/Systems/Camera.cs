using Engine.Core.Game;
using Engine.Core.Game.Rooms;
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
                Position = Vector2.Lerp(Position, Target.position, SmoothSpeed * dt);
            }
        }

        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight)
        {
            float snap = 1f / Zoom;
            Vector3 Translation = new Vector3(
            (float)MathF.Floor(-Position.X / snap) * snap,
            (float)MathF.Floor(-Position.Y / snap) * snap,
            0f);

            return Matrix.CreateTranslation(Translation) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom, Zoom, 1f) *
                   Matrix.CreateTranslation(new Vector3(viewportWidth * 0.5f, viewportHeight * 0.5f, 0f));
        }

        public Matrix GetViewMatrixPixel(int viewportWidth, int viewportHeight)
        {
            float snap = 1f / Zoom;
            Vector3 Translation = new Vector3(
            (float)MathF.Floor(-Position.X / snap) * snap,
            (float)MathF.Floor(-Position.Y / snap) * snap,
            0f);

            return Matrix.CreateTranslation(Translation) *
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

        public void SetViewportSize(int  viewportWidth, int viewportHeight)
        {

        }

        // Singleton for easy access
        private static Camera? _main;
        public static Camera Main => _main ??= new Camera();
    }
} 