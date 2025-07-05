using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace GameRuntime
{
    public class RoomManager
    {
        private readonly string assetsRoot;
        private readonly string roomsDir;
        private readonly string gameOptionsPath;
        private GameOptions gameOptions;
        private Room currentRoom;
        private List<Engine.Core.GameObject> gameObjects = new List<Engine.Core.GameObject>();
        private ScriptManager scriptManager;

        // Data classes (matching the editor)
        public class GameOptions
        {
            public string title { get; set; } = "My Game";
            public int windowWidth { get; set; } = 800;
            public int windowHeight { get; set; } = 600;
            public string defaultRoom { get; set; } = "";
        }

        public class Room
        {
            public string name { get; set; }
            public List<RoomObject> objects { get; set; } = new List<RoomObject>();
        }

        public class RoomObject
        {
            public string objectPath { get; set; }
            public double x { get; set; }
            public double y { get; set; }
        }

        public class EarthObject
        {
            public string name { get; set; }
            public string sprite { get; set; }
            public List<string> scripts { get; set; } = new List<string>();
        }

        public RoomManager(ScriptManager scriptManager)
        {
            this.scriptManager = scriptManager;
            // Robust path to Editor/bin/Assets/game_options.json
            var editorBinAssets = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Editor", "bin", "Assets"));
            assetsRoot = editorBinAssets;
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
        }

        public void LoadGameOptions()
        {
            if (File.Exists(gameOptionsPath))
            {
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
                gameOptions = new GameOptions();
            }
        }

        public void LoadDefaultRoom(GraphicsDevice graphicsDevice)
        {
            if (string.IsNullOrEmpty(gameOptions.defaultRoom))
            {
                // Try to load the first available room
                if (Directory.Exists(roomsDir))
                {
                    var roomFiles = Directory.GetFiles(roomsDir, "*.room");
                    if (roomFiles.Length > 0)
                    {
                        var firstRoom = Path.GetFileNameWithoutExtension(roomFiles[0]);
                        LoadRoom(firstRoom, graphicsDevice);
                        return;
                    }
                }
                Console.WriteLine("No rooms found to load.");
                return;
            }

            LoadRoom(gameOptions.defaultRoom, graphicsDevice);
        }

        public void LoadRoom(string roomName, GraphicsDevice graphicsDevice)
        {
            var roomPath = Path.Combine(roomsDir, $"{roomName}.room");
            if (!File.Exists(roomPath))
            {
                Console.WriteLine($"Room file not found: {roomPath}");
                return;
            }

            try
            {
                var json = File.ReadAllText(roomPath);
                currentRoom = JsonSerializer.Deserialize<Room>(json);
                
                // Clear existing objects
                gameObjects.Clear();
                
                // Create game objects from room data
                foreach (var roomObj in currentRoom.objects)
                {
                    CreateGameObject(roomObj, graphicsDevice);
                }
                
                Console.WriteLine($"Loaded room '{roomName}' with {gameObjects.Count} objects");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load room: {ex.Message}");
            }
        }

        private void CreateGameObject(RoomObject roomObj, GraphicsDevice graphicsDevice)
        {
            if (!File.Exists(roomObj.objectPath))
            {
                Console.WriteLine($"Object file not found: {roomObj.objectPath}");
                return;
            }

            try
            {
                var json = File.ReadAllText(roomObj.objectPath);
                var earthObj = JsonSerializer.Deserialize<EarthObject>(json);
                
                if (earthObj == null || string.IsNullOrEmpty(earthObj.sprite))
                {
                    Console.WriteLine("Object has no sprite assigned");
                    return;
                }

                var spritePath = Path.Combine(assetsRoot, "Sprites", earthObj.sprite);
                if (!File.Exists(spritePath))
                {
                    Console.WriteLine($"Sprite file not found: {spritePath}");
                    return;
                }

                // Load texture
                using (var fileStream = File.OpenRead(spritePath))
                {
                    var texture = Texture2D.FromStream(graphicsDevice, fileStream);
                    
                    var gameObj = new Engine.Core.GameObject
                    {
                        name = earthObj.name,
                        sprite = texture,
                        position = new Vector2((float)roomObj.x, (float)roomObj.y)
                    };

                    // Create script instances and call Create
                    foreach (var scriptName in earthObj.scripts)
                    {
                        var scriptInstance = scriptManager.CreateScriptInstanceByName(scriptName);
                        if (scriptInstance is Engine.Core.GameScript gs)
                        {
                            Console.WriteLine($"Attaching script to game object: {gameObj.name}");
                            gs.Attach(gameObj);
                            gameObj.scriptInstances.Add(gs);
                        }
                    }

                    gameObjects.Add(gameObj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create game object: {ex.Message}");
            }
        }

        public void Update(GameTime gameTime)
        {
            foreach (var gameObj in gameObjects)
            {
                foreach (var script in gameObj.scriptInstances)
                {
                    if (script is Engine.Core.GameScript gs)
                    {
                        try
                        {
                            gs.Update(gameTime);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating script for {gameObj.name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var gameObj in gameObjects)
            {
                if (gameObj.sprite != null)
                {
                    spriteBatch.Draw(gameObj.sprite, gameObj.position, Color.White);
                }

                foreach (var script in gameObj.scriptInstances)
                {
                    if (script is Engine.Core.GameScript gs)
                    {
                        try
                        {
                            gs.Draw(spriteBatch);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error drawing script for {gameObj.name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        public GameOptions GetGameOptions()
        {
            return gameOptions;
        }

        public string GetCurrentRoomName()
        {
            return currentRoom?.name ?? "";
        }
    }
} 