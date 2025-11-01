/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EngineContext.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using GameRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
        public static SpriteBatch? SpriteBatch { get; set; }
        public Room? Scene { get; set; }
        public SceneAsset? NextScene { get; set; }
        public string? AssetsRoot { get; set; }
        public string? RoomsDir { get; set; }
        public GameOptions? GameOptions { get; set; }
        public static int InternalWidth = 1920;
        public static int InternalHeight = 1080;
        public static bool Running = false;
        public static bool Debug = true;
        public static float DeltaTime = 0f;
        public static bool Wireframe = false;
        public static bool UIOnly = false;
        public static float UnitsPerPixel = 1f;
        // Add more as needed

        private EngineContext() { }
    }
} 
