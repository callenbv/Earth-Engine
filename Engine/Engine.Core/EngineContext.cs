using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Game;
using GameRuntime;

namespace Engine.Core
{
    public class EngineContext
    {
        private static EngineContext? _current;
        public static EngineContext Current => _current ??= new EngineContext();

        // Core engine services
        public ContentManager? ContentManager { get; set; }
        public GraphicsDevice? GraphicsDevice { get; set; }
        public ScriptManager? ScriptManager { get; set; }
        public string? AssetsRoot { get; set; }
        public string? RoomsDir { get; set; }
        public GameOptions? GameOptions { get; set; }
        public const int InternalWidth = 1920;
        public const int InternalHeight = 1080;
        public static bool Paused = false;
        // Add more as needed

        private EngineContext() { }
    }
} 