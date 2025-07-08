using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Text.Json;
using Engine.Core.Game;
using Engine.Core.Game.Rooms;

namespace GameRuntime
{
    public class RuntimeManager
    {
        private string assetsRoot;
        private string roomsDir;
        private string gameOptionsPath;
        private GameOptions gameOptions;
        private ScriptManager scriptManager;
        private GameObjectManager objectManager;

        /// <summary>
        /// Constructor to setup the runtime
        /// </summary>
        /// <param name="scriptManager"></param>
        public RuntimeManager(ScriptManager scriptManager)
        {
            this.scriptManager = scriptManager;
            this.objectManager = new GameObjectManager();
            
            // Always use the Assets folder relative to the EXE location
            assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            roomsDir = Path.Combine(assetsRoot, "Rooms");
            gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
            
            // Debug log to file on desktop for guaranteed visibility
            var debugLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "debug_log.txt");
            File.AppendAllText(debugLogPath, $"[DEBUG] Looking for game_options.json at: {gameOptionsPath}\n");
            if (File.Exists(gameOptionsPath))
            {

                File.AppendAllText(debugLogPath, "[DEBUG] game_options.json found!\n");
                var json = File.ReadAllText(gameOptionsPath);
                File.AppendAllText(debugLogPath, "[DEBUG] Contents:\n" + json + "\n");
            }
            else
            {
                File.AppendAllText(debugLogPath, "[DEBUG] game_options.json NOT FOUND!\n");
            }
            
            LoadGameOptions();
            
            // Load object definitions
            GameObjectRegistry.LoadAll(Path.Combine(assetsRoot, "Objects"));
        }

        /// <summary>
        /// Initialize the runtime manager
        /// </summary>
        /// <param name="device"></param>
        /// <param name="contentManager"></param>
        public void Initialize(GraphicsDevice device, ContentManager contentManager)
        {
            Room.LoadDefaultRoom(device, roomsDir, assetsRoot, contentManager, scriptManager, gameOptions);
        }

        /// <summary>
        /// Loads game options if possible
        /// </summary>
        public void LoadGameOptions()
        {
            if (File.Exists(gameOptionsPath))
            {
                Console.WriteLine($"Options found at {gameOptionsPath}");

                try
                {
                    var json = File.ReadAllText(gameOptionsPath);
                    gameOptions = JsonSerializer.Deserialize<GameOptions>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load game options: {ex.Message}");
                    gameOptions = new GameOptions();
                }
            }
            else
            {
                Console.WriteLine($"No options found at {gameOptionsPath}");
                gameOptions = new GameOptions();
            }
        }

        /// <summary>
        /// Update everything
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            objectManager.Update(gameTime);
        }

        /// <summary>
        /// Draw everything
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            objectManager.Draw(spriteBatch);
        }
    }
} 