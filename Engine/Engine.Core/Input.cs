using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Engine.Core
{
    public static class Input
    {
        private static KeyboardState _currentKeyboard;
        private static KeyboardState _previousKeyboard;
        private static MouseState _currentMouse;
        private static MouseState _previousMouse;

        public static void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
        }

        // Keyboard
        public static bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);
        public static bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);
        public static bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        public static bool IsKeyReleased(Keys key) => _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

        // Mouse
        public static bool IsMouseDown(Button button)
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
        public static bool IsMousePressed(Button button)
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
        public static bool IsMouseReleased(Button button)
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