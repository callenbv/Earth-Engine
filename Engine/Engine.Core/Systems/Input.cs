/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Input.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace Engine.Core
{
    public static class Input
    {
        private static KeyboardState _currentKeyboard;
        private static KeyboardState _previousKeyboard;
        private static MouseState _currentMouse;
        private static MouseState _previousMouse;
        public static Vector2 mouseWorldPosition;
        
        // Reference to the Game instance for calling Exit()
        public static Microsoft.Xna.Framework.Game? gameInstance;
        
        // Reference to the GraphicsDeviceManager for fullscreen toggle
        public static Microsoft.Xna.Framework.GraphicsDeviceManager? graphicsManager;
        public static int ScrollDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        public static bool ScrolledUp => ScrollDelta > 0;
        public static bool ScrolledDown => ScrollDelta < 0;

        public static void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
            mouseWorldPosition = GetMouseWorldPosition();

            HandleHotkeys();
        }

        /// <summary>
        /// Returns the mouse position in world coordinates
        /// </summary>
        /// <returns></returns>
        private static Vector2 GetMouseWorldPosition()
        {
            MouseState mouseState = Mouse.GetState();
            Point mouseScreenPos = new Point(mouseState.X, mouseState.Y);
            return Camera.Main.ScreenToWorld(mouseScreenPos);
        }

        /// <summary>
        /// Check if hovering over a recangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static bool MouseHover(Rectangle rectangle)
        {
            return rectangle.Contains(mouseWorldPosition);
        }

        /// <summary>
        /// Handle hotkey pressing
        /// </summary>
        private static void HandleHotkeys()
        {
            // Gamepad quit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                gameInstance.Exit();
            }

            // Fullscreen toggle (F11 or Alt+Enter)
            if (IsKeyPressed(Keys.F11) || (IsKeyPressed(Keys.Enter) && (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt))))
            {
                ToggleFullscreen();
            }
        }

        /// <summary>
        /// Toggle between fullscreen and windowed mode
        /// </summary>
        private static void ToggleFullscreen()
        {
            if (graphicsManager != null)
            {
                graphicsManager.ToggleFullScreen();
            }
        }

        // Keyboard
        public static bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);
        public static bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);
        public static bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        public static bool IsKeyReleased(Keys key) => _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

        public static bool IsMouseDown(Button button = Button.Left)
        {
            return button switch
            {
                Button.Left => _currentMouse.LeftButton == ButtonState.Pressed,
                Button.Right => _currentMouse.RightButton == ButtonState.Pressed,
                Button.Middle => _currentMouse.MiddleButton == ButtonState.Pressed,
                _ => false
            };
        }
        public static bool IsMouseUp(Button button)
        {
            return button switch
            {
                Button.Left => _currentMouse.LeftButton == ButtonState.Released,
                Button.Right => _currentMouse.RightButton == ButtonState.Released,
                Button.Middle => _currentMouse.MiddleButton == ButtonState.Released,
                _ => false
            };
        }
        public static bool IsMousePressed(Button button = Button.Left)
        {
            return IsMouseDown(button) &&
                (button switch
                {
                    Button.Left => _previousMouse.LeftButton == ButtonState.Released,
                    Button.Right => _previousMouse.RightButton == ButtonState.Released,
                    Button.Middle => _previousMouse.MiddleButton == ButtonState.Released,
                    _ => false
                });
        }
        public static bool IsMouseReleased(Button button = Button.Left)
        {
            return IsMouseUp(button) &&
                (button switch
                {
                    Button.Left => _previousMouse.LeftButton == ButtonState.Pressed,
                    Button.Right => _previousMouse.RightButton == ButtonState.Pressed,
                    Button.Middle => _previousMouse.MiddleButton == ButtonState.Pressed,
                    _ => false
                });
        }
        public static Point MousePosition => _currentMouse.Position;

        public enum Button { Left, Right, Middle }
    }
} 
