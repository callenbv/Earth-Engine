/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorCamera.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Simple editor camera for scene navigation - not component based
/// -----------------------------------------------------------------------------

using Engine.Core;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Editor.Windows
{
    /// <summary>
    /// Define if camera is in 3D or 2D
    /// </summary>
    public enum CameraMode
    {
        Camera2D,
        Camera3D
    }

    /// <summary>
    /// Simple editor camera controller for scene navigation
    /// </summary>
    public static class EditorCamera
    {
        private static float _yaw = 0f;
        private static float _pitch = 0f;
        private static bool _initialized = false;
        private static bool _wasRightMouseDown = false;
        private static Microsoft.Xna.Framework.Point _pivotStartPosition = Microsoft.Xna.Framework.Point.Zero;
        
        public static float MoveSpeed = 500f;
        public static float MouseSensitivity = 0.3f;
        public static CameraMode Mode = CameraMode.Camera2D;
        
        /// <summary>
        /// Update editor camera (call this from EditorApp.Update)
        /// </summary>
        public static void Update(GameTime gameTime, bool canMove)
        {
            switch (Mode)
            {
                case CameraMode.Camera2D:
                    UpdateIn2D(gameTime);
                    break;
                case CameraMode.Camera3D:
                    UpdateIn3D(gameTime, canMove);
                    break;
            }
        }

        /// <summary>
        /// For 2D editor camera
        /// </summary>
        /// <param name="gameTime"></param>
        public static void UpdateIn2D(GameTime gameTime)
        {

        }

        /// <summary>
        /// For 3D editor camera
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="canMove"></param>
        public static void UpdateIn3D(GameTime gameTime, bool canMove)
        {
            var cam = Camera3D.Main;

            if (!_initialized)
            {
                _initialized = true;
                Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);
                _yaw = (float)MathF.Atan2(forward.X, forward.Z);
                _pitch = (float)MathF.Asin(forward.Y);
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float speed = MoveSpeed * dt;

            // Mouse look (right button held) - only in editor when UI doesn't have focus
            var ms = Mouse.GetState();
            bool rightMouseDown = ms.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && canMove;
            Microsoft.Xna.Framework.Point cur = ms.Position;

            // Handle right mouse button state changes for pivot mode
            if (rightMouseDown && !_wasRightMouseDown)
            {
                // Just pressed right mouse - start mouse look mode
                _pivotStartPosition = cur;
            }
            else if (rightMouseDown && _wasRightMouseDown)
            {
                // Right mouse held - process mouse movement for camera look
                var delta = cur - _pivotStartPosition;

                // Process mouse delta for smooth camera rotation
                if (Math.Abs(delta.X) > 0 || Math.Abs(delta.Y) > 0)
                {
                    _yaw -= delta.X * MouseSensitivity * 0.01f; // Remove dt for smoother rotation
                    _pitch -= delta.Y * MouseSensitivity * 0.01f;
                    _pitch = Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
                }

                // Reset mouse to pivot start position for continuous movement
                Mouse.SetPosition(_pivotStartPosition.X, _pivotStartPosition.Y);
            }

            _wasRightMouseDown = rightMouseDown;

            // Build camera basis from yaw/pitch
            Vector3 dir = Vector3.Normalize(new Vector3(
                MathF.Sin(_yaw) * MathF.Cos(_pitch),
                MathF.Sin(_pitch),
                MathF.Cos(_yaw) * MathF.Cos(_pitch)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(dir, Vector3.Up));
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, dir));

            // WASD + SHIFT/SPACE - only if we can move
            if (canMove)
            {
                Vector3 move = Vector3.Zero;
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W)) move += dir;       // Forward
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S)) move -= dir;       // Backward
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A)) move -= right;     // Left
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D)) move += right;     // Right
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space)) move += up;    // Up
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)) move -= up; // Down
                if (move.LengthSquared() > 0f) move = Vector3.Normalize(move);

                cam.Position += move * speed;
            }

            // Editor camera always looks in the direction it's facing
            cam.Target = cam.Position + dir;
            cam.Up = up;
        }
    }
}
