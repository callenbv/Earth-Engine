/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Input.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Implements an input binding class to allow multiple keybinds      
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Engine.Core.Systems
{
    /// <summary>
    /// Defines the type of input source for an action binding.
    /// </summary>
    public enum InputSourceType
    {
        Keyboard,
        GamePadButton,
        Gpio
    }

    /// <summary>
    /// Each virtual button is mapped to a GPIO pin button
    /// </summary>
    public enum VirtualButton
    {
        A,
        B,
        X,
        Y,
        Up,
        Down,
        Left,
        Right,
        Start,
        Select
    }

    /// <summary>
    /// Enumeration for mouse buttons.
    /// </summary>
    public enum Button
    {
        Left,
        Right,
        Middle
    }

    /// <summary>
    /// Represents a binding between an input action and a specific input source.
    /// </summary>
    public readonly struct InputBinding
    {
        public InputSourceType SourceType { get; }
        public object Code { get; } // e.g., Keys.D, Buttons.DPadRight, or GPIO pin int

        /// <summary>
        /// Creates a new input binding for a specific input source type and code.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="code"></param>
        public InputBinding(InputSourceType sourceType, object code)
        {
            SourceType = sourceType;
            Code = code;
        }
        public override string ToString() => $"{SourceType}:{Code}";
    }

    /// <summary>
    /// Represents a unique identifier for an input action.
    /// </summary>
    public sealed class InputID
    {
        private static readonly Dictionary<string, InputID> _registry = new();

        public string Name { get; }

        /// <summary>
        /// Private constructor to create a new input action ID.
        /// </summary>
        /// <param name="name"></param>
        private InputID(string name)
        {
            Name = name;
            _registry[name] = this;
        }

        /// <summary>
        /// Registers a new input action ID.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static InputID Get(string name)
        {
            if (_registry.TryGetValue(name, out var existing))
                return existing;

            throw new KeyNotFoundException($"InputID '{name}' is not defined.");
        }

        public override string ToString() => Name;

        public static readonly InputID MoveLeft = new("MoveLeft");
        public static readonly InputID MoveRight = new("MoveRight");
        public static readonly InputID MoveUp = new("MoveUp");
        public static readonly InputID MoveDown = new("MoveDown");

        public static readonly InputID Jump = new("Jump");
        public static readonly InputID Shoot = new("Shoot");
        public static readonly InputID Interact = new("Interact");
        public static readonly InputID Start = new("Start");
        public static readonly InputID Select = new("Select");
        public static readonly InputID Pause = new("Pause");

        public static readonly InputID Inventory = new("Inventory");
        public static readonly InputID Dash = new("Dash");
    }

    /// <summary>
    /// Manages input actions and their bindings.
    /// </summary>
    public static class InputAction
    {
        private static readonly Dictionary<InputID, List<InputBinding>> _bindings = new();

        /// <summary>
        /// Binds an input action to a specific input source.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="binding"></param>
        public static void Bind(InputID action, InputBinding binding)
        {
            if (!_bindings.TryGetValue(action, out var list))
                _bindings[action] = list = new List<InputBinding>();

            list.Add(binding);
        }

        /// <summary>
        /// Checks if an input action is currently pressed based on its bindings.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool IsDown(InputID action)
        {
            if (!_bindings.TryGetValue(action, out var list))
                return false;

            foreach (var binding in list)
            {
                if (binding.SourceType == InputSourceType.Keyboard && Input.IsKeyDown((Keys)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.GamePadButton && GamePad.GetState(PlayerIndex.One).IsButtonDown((Buttons)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.Gpio && Input.IsButtonDown((VirtualButton)binding.Code))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an input action is currently pressed based on its bindings.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool Pressed(InputID action)
        {
            if (!_bindings.TryGetValue(action, out var list))
                return false;

            foreach (var binding in list)
            {
                if (binding.SourceType == InputSourceType.Keyboard && Input.IsKeyPressed((Keys)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.GamePadButton && GamePad.GetState(PlayerIndex.One).IsButtonDown((Buttons)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.Gpio && Input.IsButtonPressed((VirtualButton)binding.Code))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an input action is currently pressed based on its bindings.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool Released(InputID action)
        {
            if (!_bindings.TryGetValue(action, out var list))
                return false;

            foreach (var binding in list)
            {
                if (binding.SourceType == InputSourceType.Keyboard && Input.IsKeyReleased((Keys)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.GamePadButton && GamePad.GetState(PlayerIndex.One).IsButtonDown((Buttons)binding.Code))
                    return true;
                if (binding.SourceType == InputSourceType.Gpio && Input.IsButtonReleased((VirtualButton)binding.Code))
                    return true;
            }

            return false;
        }
    }
}
