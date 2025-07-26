/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Input.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Defines an Input class to handle input states and actions        
/// -----------------------------------------------------------------------------

#define WINDOWS

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.Device.Gpio;
using Engine.Core.Systems;

namespace Engine.Core
{
    /// <summary>
    /// Mock GpioController for platforms without GPIO support (e.g., Windows).
    /// </summary>
    public class MockGpioController
    {
        public void OpenPin(int pin, PinMode mode) { /* no-op */ }
        public bool Read(int pin) => false; // Always not pressed
    }

    /// <summary>
    /// Handles input from keyboard and mouse, including hotkeys and fullscreen toggling.
    /// </summary>
    public static class Input
    {
        private static KeyboardState _currentKeyboard;
        private static KeyboardState _previousKeyboard;
        private static MouseState _currentMouse;
        private static MouseState _previousMouse;
        public static Vector2 mouseWorldPosition;
        
        public static Microsoft.Xna.Framework.Game gameInstance;
        public static Microsoft.Xna.Framework.GraphicsDeviceManager? graphicsManager;

#if WINDOWS
        private static MockGpioController _gpio;
#else
        private static GpioController _gpio;
#endif

        /// <summary>
        /// Mapping of virtual buttons to GPIO pins.
        /// </summary>
        private static readonly Dictionary<VirtualButton, int> _gpioPins = new()
        {
            { VirtualButton.A, 4 }, // 7
            { VirtualButton.B, 14 }, // 8
            { VirtualButton.X, 17 }, // 11
            { VirtualButton.Y, 18 }, // 12
            { VirtualButton.Up, 27 }, // 13
            { VirtualButton.Down, 22 }, // 15
            { VirtualButton.Left, 23 }, // 16
            { VirtualButton.Right, 24 }, // 18
            { VirtualButton.Start, 5 }, // 29
            { VirtualButton.Select, 6 }, // 31
        };

        private static Dictionary<VirtualButton, bool> _gpioStates = new();
        private static readonly Dictionary<VirtualButton, bool> _currentButtonStates = new();
        private static readonly Dictionary<VirtualButton, bool> _previousButtonStates = new();

        /// <summary>
        /// Scroll delta value (positive for scroll up, negative for scroll down).
        /// </summary>
        public static int ScrollDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

        /// <summary>
        /// Indicates if the mouse was scrolled up (positive delta).
        /// </summary>
        public static bool ScrolledUp => ScrollDelta > 0;

        /// <summary>
        /// Indicates if the mouse was scrolled down (negative delta).
        /// </summary>
        public static bool ScrolledDown => ScrollDelta < 0;

        /// <summary>
        /// Get the current mouse position in screen coordinates.
        /// </summary>
        public static Point MousePosition => _currentMouse.Position;

        /// <summary>
        /// Initializes the Input system, setting up keyboard and mouse states, and GPIO pins if available.
        /// </summary>
        public static void Initialize()
        {
#if WINDOWS
            _gpio = new MockGpioController();
#else
            _gpio = new GpioController();

            foreach (var kv in _gpioPins)
            {
                _gpio.OpenPin(kv.Value, PinMode.InputPullUp);
                _gpioStates[kv.Key] = false;
            }
            Console.WriteLine("Setup GPIO");
#endif
            BindInput();
        }

        /// <summary>
        /// Binds input actions to virtual buttons and initializes the current button states.
        /// </summary>
        public static void BindInput()
        {
            // --- Movement ---
            InputAction.Bind(InputID.MoveLeft, new(InputSourceType.Keyboard, Keys.A));
            InputAction.Bind(InputID.MoveLeft, new(InputSourceType.Keyboard, Keys.Left));
            InputAction.Bind(InputID.MoveLeft, new(InputSourceType.GamePadButton, Buttons.DPadLeft));
            InputAction.Bind(InputID.MoveLeft, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Left]));

            InputAction.Bind(InputID.MoveRight, new(InputSourceType.Keyboard, Keys.D));
            InputAction.Bind(InputID.MoveRight, new(InputSourceType.Keyboard, Keys.Right));
            InputAction.Bind(InputID.MoveRight, new(InputSourceType.GamePadButton, Buttons.DPadRight));
            InputAction.Bind(InputID.MoveRight, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Right]));

            InputAction.Bind(InputID.MoveUp, new(InputSourceType.Keyboard, Keys.W));
            InputAction.Bind(InputID.MoveUp, new(InputSourceType.Keyboard, Keys.Up));
            InputAction.Bind(InputID.MoveUp, new(InputSourceType.GamePadButton, Buttons.DPadUp));
            InputAction.Bind(InputID.MoveUp, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Up]));

            InputAction.Bind(InputID.MoveDown, new(InputSourceType.Keyboard, Keys.S));
            InputAction.Bind(InputID.MoveDown, new(InputSourceType.Keyboard, Keys.Down));
            InputAction.Bind(InputID.MoveDown, new(InputSourceType.GamePadButton, Buttons.DPadDown));
            InputAction.Bind(InputID.MoveDown, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Down]));

            // --- Action Buttons ---
            InputAction.Bind(InputID.Jump, new(InputSourceType.Keyboard, Keys.Space));
            InputAction.Bind(InputID.Jump, new(InputSourceType.GamePadButton, Buttons.A));
            InputAction.Bind(InputID.Jump, new(InputSourceType.Gpio, _gpioPins[VirtualButton.A]));

            InputAction.Bind(InputID.Shoot, new(InputSourceType.Keyboard, Keys.LeftControl));
            InputAction.Bind(InputID.Shoot, new(InputSourceType.GamePadButton, Buttons.X));
            InputAction.Bind(InputID.Shoot, new(InputSourceType.Gpio, _gpioPins[VirtualButton.X]));

            InputAction.Bind(InputID.Dash, new(InputSourceType.Keyboard, Keys.LeftShift));
            InputAction.Bind(InputID.Dash, new(InputSourceType.GamePadButton, Buttons.B));
            InputAction.Bind(InputID.Dash, new(InputSourceType.Gpio, _gpioPins[VirtualButton.B]));

            InputAction.Bind(InputID.Interact, new(InputSourceType.Keyboard, Keys.E));
            InputAction.Bind(InputID.Interact, new(InputSourceType.GamePadButton, Buttons.Y));
            InputAction.Bind(InputID.Interact, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Y]));

            // --- System ---
            InputAction.Bind(InputID.Start, new(InputSourceType.Keyboard, Keys.Enter));
            InputAction.Bind(InputID.Start, new(InputSourceType.GamePadButton, Buttons.Start));
            InputAction.Bind(InputID.Start, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Start]));

            InputAction.Bind(InputID.Select, new(InputSourceType.Keyboard, Keys.Tab));
            InputAction.Bind(InputID.Select, new(InputSourceType.GamePadButton, Buttons.Back));
            InputAction.Bind(InputID.Select, new(InputSourceType.Gpio, _gpioPins[VirtualButton.Select]));
        }

        /// <summary>
        /// Initializes the Input system with the game instance and graphics manager.
        /// </summary>
        public static void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
            mouseWorldPosition = GetMouseWorldPosition();

            // Update GPIO Pins
            if (_gpio != null)
            {
                foreach (var kv in _gpioPins)
                {
                    // GPIO input is pulled up, so pressed = Low (false)
                    _gpioStates[kv.Key] = !_gpio.Read(kv.Value).Equals(PinValue.High);
                }
            }

            HandleHotkeys();
        }

        /// <summary>
        /// Check if a specific virtual button is currently pressed, released, or just pressed/released.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsButtonDown(VirtualButton button)
        {
            return _currentButtonStates.TryGetValue(button, out var isDown) ? isDown : false;
        }

        /// <summary>
        /// Check if a specific virtual button is currently released (not pressed).
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsButtonPressed(VirtualButton button)
        {
            bool wasDown = _previousButtonStates.TryGetValue(button, out var prev) && prev;
            return IsButtonDown(button) && !wasDown;
        }

        /// <summary>
        /// Check if a specific virtual button was just released (transition from down to up).
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsButtonReleased(VirtualButton button)
        {
            bool prevState;
            _previousButtonStates.TryGetValue(button, out prevState);
            return !IsButtonDown(button) && prevState;
        }

        /// <summary>
        /// Check if a specific key is currently pressed, released, or just pressed/released.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);

        /// <summary>
        /// Check if a specific key is currently released (not pressed).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);

        /// <summary>
        /// Check if a specific key was just pressed (transition from up to down).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

        /// <summary>
        /// Check if a specific key was just released (transition from down to up).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyReleased(Keys key) => _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

        /// <summary>
        /// Check if a specific mouse button is currently pressed, released, or just pressed/released.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if a specific mouse button is currently released (not pressed).
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if a specific mouse button was just pressed (transition from up to down).
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if a specific mouse button was just released (transition from down to up).
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
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
        public static void ToggleFullscreen()
        {
            if (graphicsManager != null)
            {
                graphicsManager.IsFullScreen = !graphicsManager.IsFullScreen;
                graphicsManager.ApplyChanges();
            }
        }

        /// <summary>
        /// Set the fullscreen mode directly
        /// </summary>
        /// <param name="val"></param>
        public static void SetFullscreen(bool val)
        {
            if (graphicsManager != null)
            {
                graphicsManager.IsFullScreen = val;
                graphicsManager.ApplyChanges();
            }
        }
    }
} 
