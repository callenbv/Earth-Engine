using Engine.Core.Systems;
using Engine.Core.Data;
using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("Graphics")]
    public class Camera3DController : ObjectComponent
    {
        public override string Name => "Camera 3D Controller";
        public override bool UpdateInEditor => false;

        public float MouseSensitivity { get; set; } = 0.3f;
        public Vector3 CameraOffset { get; set; } = new Vector3(0, 0, 0);

        private float yaw;
        private float pitch;
        private Point screenCenter;
        private Point lastMousePosition;
        private bool initialized = false;
        private int framesSinceReset = 0;

        public override void Initialize()
        {
            // Get screen center for mouse locking
            var viewport = Engine.Core.EngineContext.Current?.GraphicsDevice.Viewport;
            if (viewport.HasValue)
            {
                screenCenter = new Point(viewport.Value.Width / 2, viewport.Value.Height / 2);
            }
            lastMousePosition = Input.MousePosition;
        }

        public override void Update(GameTime gameTime)
        {
            var cam = Camera3D.Main;
            var transform = Owner.GetComponent<Transform>();
            if (transform == null) return;

            // === FPS Mouse Look with Smart Center Locking ===
            Point currentMousePos = Input.MousePosition;
            
            if (!initialized)
            {
                Mouse.SetPosition(screenCenter.X, screenCenter.Y);
                lastMousePosition = Input.MousePosition; // Get the actual state after reset
                initialized = true;
                framesSinceReset = 0;
                return;
            }

            framesSinceReset++;

            // Only process movement if enough frames have passed since last reset to avoid feedback
            if (framesSinceReset > 1)
            {
                // Calculate delta from last known position
                float deltaX = currentMousePos.X - lastMousePosition.X;
                float deltaY = currentMousePos.Y - lastMousePosition.Y;
                
                // Apply mouse movement with delta time for consistent sensitivity
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                yaw -= deltaX * MouseSensitivity * 0.01f * dt;
                pitch -= deltaY * MouseSensitivity * 0.01f * dt;
                pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

                // Reset mouse to center periodically, not every frame
                if (Math.Abs(currentMousePos.X - screenCenter.X) > 50 || Math.Abs(currentMousePos.Y - screenCenter.Y) > 50)
                {
                    Mouse.SetPosition(screenCenter.X, screenCenter.Y);
                    framesSinceReset = 0;
                }
            }
            
            lastMousePosition = currentMousePos;

            Vector3 forward = new Vector3(
                MathF.Sin(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Cos(yaw) * MathF.Cos(pitch));
            Vector3 up = Vector3.Up;

            // Just follow the player exactly - convert position to camera space
            Vector3 playerPos = transform.Position;
            Vector3 cameraPosition = new Vector3(
                playerPos.X / Engine.Core.EngineContext.UnitsPerPixel,
                -playerPos.Y / Engine.Core.EngineContext.UnitsPerPixel, 
                playerPos.Z / Engine.Core.EngineContext.UnitsPerPixel
            ) + CameraOffset;
            
            cam.Position = cameraPosition;
            cam.Target = cameraPosition + forward;
            cam.Up = up;
        }
    }
}