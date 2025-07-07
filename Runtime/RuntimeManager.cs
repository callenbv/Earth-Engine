using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Linq;
using Engine.Core;
using Engine.Core.Graphics;
using Engine.Core.Game;
using Engine.Core.Game.Rooms;
using Engine.Core.Game.Components;

namespace GameRuntime
{
    public class RuntimeManager
    {
        private readonly string assetsRoot;
        private readonly string roomsDir;
        private readonly string gameOptionsPath;
        private GameOptions gameOptions;
        private ScriptManager scriptManager;
        private GameObjectManager objectManager;

        public RuntimeManager(ScriptManager scriptManager)
        {
            this.scriptManager = scriptManager;
            this.objectManager = new GameObjectManager();
            
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
            
            // Load object definitions
            GameObjectRegistry.LoadAll(Path.Combine(assetsRoot, "Objects"));
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

        public void LoadDefaultRoom(GraphicsDevice graphicsDevice, ContentManager content = null)
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
                        LoadRoom(firstRoom, graphicsDevice, content);
                        return;
                    }
                }
                Console.WriteLine("No rooms found to load.");
                return;
            }

            LoadRoom(gameOptions.defaultRoom, graphicsDevice, content);
        }

        public void LoadRoom(string roomName, GraphicsDevice graphicsDevice, ContentManager content = null)
        {
            var roomPath = Path.Combine(roomsDir, $"{roomName}.room");
            
            if (!File.Exists(roomPath))
            {
                Console.WriteLine($"Room file not found: {roomPath}");
                return;
            }

            try
            {
                // Clear existing objects
                objectManager.Clear();
                
                var json = File.ReadAllText(roomPath);
                var roomData = JsonSerializer.Deserialize<Room>(json);
                
                if (roomData?.objects != null)
                {
                    foreach (var obj in roomData.objects)
                    {
                        try
                        {
                            // Extract object name from path if objectName is not set
                            string objectName = obj.objectName;
                            if (string.IsNullOrEmpty(objectName) && !string.IsNullOrEmpty(obj.objectPath))
                            {
                                objectName = Path.GetFileNameWithoutExtension(obj.objectPath);
                            }
                            
                            if (!string.IsNullOrEmpty(objectName))
                            {
                                var position = new Vector2(obj.x, obj.y);
                                var gameObject = GameObject.Instantiate(objectName, position);
                                
                                // Load texture and attach scripts after creation
                                LoadTextureAndScripts(gameObject, objectName, content, scriptManager, graphicsDevice);
                                
                                Console.WriteLine($"Spawned {objectName} at ({obj.x}, {obj.y})");
                            }
                            else
                            {
                                Console.WriteLine($"No object name found for object at ({obj.x}, {obj.y})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to spawn object: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load room {roomName}: {ex.Message}");
            }
        }

        public void Update(GameTime gameTime)
        {
            objectManager.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            objectManager.Draw(spriteBatch);
        }

        /// <summary>
        /// Get all lights from game objects and populate the lighting system
        /// </summary>
        /// <param name="lighting">The lighting system to populate</param>
        public void GetLights(Lighting2D lighting)
        {
            if (lighting == null) return;
            
            lighting.Lights.Clear();
            
            foreach (var gameObj in objectManager.GetAllObjects())
            {
                // Check if the object has any scripts that emit light
                foreach (var script in gameObj.scriptInstances)
                {
                    if (script is Engine.Core.GameScript gameScript)
                    {
                        // Check if this script has lighting properties
                        var lightRadiusProperty = script.GetType().GetField("lightRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var lightIntensityProperty = script.GetType().GetField("lightIntensity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var lightColorProperty = script.GetType().GetField("lightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (lightRadiusProperty != null && lightIntensityProperty != null && lightColorProperty != null)
                        {
                            var radius = (float)lightRadiusProperty.GetValue(script);
                            var intensity = (float)lightIntensityProperty.GetValue(script);
                            var colorName = (string)lightColorProperty.GetValue(script);
                            
                            if (radius > 0 && intensity > 0)
                            {
                                // Parse color string to Color
                                var color = Microsoft.Xna.Framework.Color.White;
                                try
                                {
                                    var colorProperty = typeof(Microsoft.Xna.Framework.Color).GetProperty(colorName);
                                    if (colorProperty != null)
                                    {
                                        color = (Microsoft.Xna.Framework.Color)colorProperty.GetValue(null);
                                    }
                                }
                                catch
                                {
                                    // Default to white if color parsing fails
                                    color = Microsoft.Xna.Framework.Color.White;
                                }
                                
                                var light = new LightSource
                                {
                                    Position = gameObj.position,
                                    Radius = radius,
                                    Color = color,
                                    Intensity = intensity
                                };
                                
                                lighting.Lights.Add(light);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load texture and attach scripts to a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to configure</param>
        /// <param name="objectName">Name of the object definition</param>
        /// <param name="content">ContentManager for loading textures</param>
        /// <param name="scriptManager">ScriptManager for creating scripts</param>
        /// <param name="graphicsDevice">GraphicsDevice for loading textures</param>
        private void LoadTextureAndScripts(GameObject gameObject, string objectName, ContentManager content, object scriptManager, GraphicsDevice graphicsDevice)
        {

            try
            {
                // Get the object definition
                var objDef = Engine.Core.Game.GameObjectRegistry.Get(objectName);
                
                // Load texture
                if (!string.IsNullOrEmpty(objDef.Sprite))
                {
                    try
                    {
                        // Look for texture in the Sprites directory relative to the assets root
                        var spritePath = Path.Combine(assetsRoot, "Sprites", objDef.Sprite);
                        if (File.Exists(spritePath))
                        {
                            // Load texture directly from file using GraphicsDevice
                            gameObject.sprite = new SpriteData();
                            gameObject.sprite.texture = Texture2D.FromFile(graphicsDevice, spritePath);
                            
                            // Try to load sprite definition from .sprite file
                            var spriteDefPath = Path.Combine(assetsRoot, "Sprites", Path.GetFileNameWithoutExtension(objDef.Sprite) + ".sprite");
                            if (File.Exists(spriteDefPath))
                            {
                                try
                                {
                                    var spriteJson = File.ReadAllText(spriteDefPath);
                                    var spriteDef = JsonSerializer.Deserialize<SpriteData>(spriteJson);
                                    
                                    if (spriteDef != null)
                                    {
                                        gameObject.sprite.frameWidth = spriteDef.frameWidth;
                                        gameObject.sprite.frameHeight = spriteDef.frameHeight;
                                        gameObject.sprite.frameCount = spriteDef.frameCount;
                                        gameObject.sprite.frameSpeed = spriteDef.frameSpeed;
                                        gameObject.sprite.animated = spriteDef.animated;
                                        
                                        Console.WriteLine($"Loaded sprite definition for {objectName}: {spriteDef.frameWidth}x{spriteDef.frameHeight}, {spriteDef.frameCount} frames, animated: {spriteDef.animated}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to load sprite definition for {objectName}: {ex.Message}");
                                }
                            }
                            else
                            {
                                // Fallback: set default values if no .sprite file exists
                                gameObject.sprite.frameWidth = gameObject.sprite.texture.Width;
                                gameObject.sprite.frameHeight = gameObject.sprite.texture.Height;
                                gameObject.sprite.frameCount = 1;
                                gameObject.sprite.frameSpeed = 1;
                                gameObject.sprite.animated = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Texture file not found: {spritePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load texture '{objDef.Sprite}' for {objectName}: {ex.Message}");
                    }
                }
                
                // Attach scripts
                if (objDef.Scripts != null)
                {
                    foreach (var scriptName in objDef.Scripts.Values<string>())
                    {
                        try
                        {
                            // Use reflection to call CreateScriptInstanceByName on the scriptManager
                            var createMethod = scriptManager.GetType().GetMethod("CreateScriptInstanceByName");
                            if (createMethod != null)
                            {
                                var script = createMethod.Invoke(scriptManager, new object[] { scriptName }) as Engine.Core.GameScript;
                                if (script != null)
                                {
                                    script.Attach(gameObject);
                                    gameObject.scriptInstances.Add(script);
                                    Console.WriteLine($"Attached script '{scriptName}' to {objectName}");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to create script instance '{scriptName}' for {objectName}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"ScriptManager does not have CreateScriptInstanceByName method");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error attaching script '{scriptName}' to {objectName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading texture and scripts for {objectName}: {ex.Message}");
            }
        }

        public GameOptions GetGameOptions()
        {
            return gameOptions;
        }
    }
} 