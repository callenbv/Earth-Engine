using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Linq;

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
        private int viewportWidth = 800;
        private int viewportHeight = 600;
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private Texture2D backgroundTexture = null;
        private bool backgroundTiled = false;

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
            public string background { get; set; } = "";
            public bool backgroundTiled { get; set; } = false;
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
            
            // Lighting properties
            public bool emitsLight { get; set; } = false;
            public float lightRadius { get; set; } = 100f;
            public float lightIntensity { get; set; } = 1f;
            public string lightColor { get; set; } = "White"; // Color name as string
        }

        public class SpriteData
        {
            public string name { get; set; }
            public int frameWidth { get; set; } = 0; // 0 means use full image
            public int frameHeight { get; set; } = 0; // 0 means use full image
            public int frameCount { get; set; } = 1;
            public double frameSpeed { get; set; } = 1.0; // frames per second
            public bool animated { get; set; } = false;
        }

        // Runtime extension for animation state
        public class AnimatedGameObject : Engine.Core.GameObject
        {
            public SpriteData spriteData;
            public int currentFrame = 0;
            public double frameTimer = 0;
            public string spriteName;
            
            // Lighting properties
            public bool emitsLight = false;
            public float lightRadius = 100f;
            public float lightIntensity = 1f;
            public string lightColor = "White";

            public void SetSprite(string newSpriteName, RoomManager roomManager)
            {
                spriteName = newSpriteName;
                sprite = roomManager.LoadTexture(spriteName);
                spriteData = roomManager.LoadSpriteData(spriteName, sprite);
                currentFrame = 0;
                frameTimer = 0;
            }
            
            public LightSource CreateLightSource()
            {
                if (!emitsLight) return null;
                
                // Parse color string to Color
                Color color = Color.White;
                try
                {
                    var colorProperty = typeof(Color).GetProperty(lightColor);
                    if (colorProperty != null)
                    {
                        color = (Color)colorProperty.GetValue(null);
                    }
                }
                catch
                {
                    // Default to white if color parsing fails
                    color = Color.White;
                }
                
                return new LightSource
                {
                    Position = position,
                    Radius = lightRadius,
                    Color = color,
                    Intensity = lightIntensity
                };
            }
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
                
                // Load background
                backgroundTexture = null;
                backgroundTiled = false;
                
                if (!string.IsNullOrEmpty(currentRoom.background))
                {
                    backgroundTexture = LoadTexture(currentRoom.background);
                    backgroundTiled = currentRoom.backgroundTiled;
                    
                    if (backgroundTexture != null)
                    {
                        Console.WriteLine($"Loaded background: {currentRoom.background} (Tiled: {backgroundTiled})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load background: {currentRoom.background}");
                    }
                }
                
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

                // Load texture using the cache system
                var texture = LoadTexture(earthObj.sprite);
                if (texture == null)
                {
                    Console.WriteLine($"Failed to load sprite: {earthObj.sprite}");
                    return;
                }

                // Create animated game object
                var gameObj = new AnimatedGameObject
                {
                    name = earthObj.name,
                    sprite = texture,
                    position = new Vector2((float)roomObj.x, (float)roomObj.y),
                    spriteName = earthObj.sprite,
                    spriteData = LoadSpriteData(earthObj.sprite, texture),
                    currentFrame = 0,
                    frameTimer = 0,
                    
                    // Copy lighting properties
                    emitsLight = earthObj.emitsLight,
                    lightRadius = earthObj.lightRadius,
                    lightIntensity = earthObj.lightIntensity,
                    lightColor = earthObj.lightColor
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
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create game object: {ex.Message}");
            }
        }

        public void Update(GameTime gameTime)
        {
            // Update animation for all animated game objects
            foreach (var obj in gameObjects)
            {
                if (obj is AnimatedGameObject animObj && animObj.spriteData != null && animObj.spriteData.animated && animObj.spriteData.frameCount > 1)
                {
                    animObj.frameTimer += gameTime.ElapsedGameTime.TotalSeconds;
                    double frameDuration = 1.0 / Math.Max(0.01, animObj.spriteData.frameSpeed);
                    if (animObj.frameTimer >= frameDuration)
                    {
                        int framesToAdvance = (int)(animObj.frameTimer / frameDuration);
                        animObj.currentFrame = (animObj.currentFrame + framesToAdvance) % animObj.spriteData.frameCount;
                        animObj.frameTimer -= framesToAdvance * frameDuration;
                    }
                }
            }
            
            // Check for mouse clicks on objects
            if (Engine.Core.Input.IsMousePressed(Engine.Core.Input.Button.Left))
            {
                var mousePos = Engine.Core.Input.MousePosition;
                // Get viewport dimensions from the graphics device (we'll need to pass this)
                var worldPos = Engine.Core.Camera.Main.ScreenToWorld(mousePos, viewportWidth, viewportHeight);
                
                // Check each object for click
                foreach (var gameObj in gameObjects)
                {
                    if (gameObj.sprite != null && !gameObj.IsDestroyed)
                    {
                        // Calculate object bounds (assuming sprite is centered on position)
                        var spriteWidth = gameObj.sprite.Width;
                        var spriteHeight = gameObj.sprite.Height;
                        var left = gameObj.position.X;
                        var right = gameObj.position.X + spriteWidth;
                        var top = gameObj.position.Y;
                        var bottom = gameObj.position.Y + spriteHeight;
                        
                        // Check if mouse is within object bounds
                        if (worldPos.X >= left && worldPos.X <= right && 
                            worldPos.Y >= top && worldPos.Y <= bottom)
                        {
                            // Call OnClick on all scripts attached to this object
                            foreach (var script in gameObj.scriptInstances)
                            {
                                if (script is Engine.Core.GameScript gs)
                                {
                                    try
                                    {
                                        gs.OnClick();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error calling OnClick on script for {gameObj.name}: {ex.Message}");
                                    }
                                }
                            }
                            break; // Only click the first object found
                        }
                    }
                }
            }
            
            // Update all scripts
            foreach (var gameObj in gameObjects)
            {
                if (!gameObj.IsDestroyed)
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
            
            // Remove destroyed objects
            gameObjects.RemoveAll(obj => obj.IsDestroyed);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw background first
            if (backgroundTexture != null)
            {
                if (backgroundTiled)
                {
                    // Draw tiled background
                    var textureWidth = backgroundTexture.Width;
                    var textureHeight = backgroundTexture.Height;
                    
                    // Calculate how many tiles we need to cover the viewport
                    var camera = Engine.Core.Camera.Main;
                    var cameraLeft = camera.Position.X - viewportWidth / (2 * camera.Zoom);
                    var cameraTop = camera.Position.Y - viewportHeight / (2 * camera.Zoom);
                    var cameraRight = camera.Position.X + viewportWidth / (2 * camera.Zoom);
                    var cameraBottom = camera.Position.Y + viewportHeight / (2 * camera.Zoom);
                    
                    // Calculate tile positions
                    var startX = (int)(cameraLeft / textureWidth) * textureWidth;
                    var startY = (int)(cameraTop / textureHeight) * textureHeight;
                    var endX = (int)(cameraRight / textureWidth + 1) * textureWidth;
                    var endY = (int)(cameraBottom / textureHeight + 1) * textureHeight;
                    
                    // Draw tiles
                    for (int x = startX; x <= endX; x += textureWidth)
                    {
                        for (int y = startY; y <= endY; y += textureHeight)
                        {
                            spriteBatch.Draw(backgroundTexture, new Vector2(x, y), Color.White);
                        }
                    }
                }
                else
                {
                    // Draw single background image centered
                    var position = new Vector2(
                        Engine.Core.Camera.Main.Position.X - backgroundTexture.Width / 2,
                        Engine.Core.Camera.Main.Position.Y - backgroundTexture.Height / 2
                    );
                    spriteBatch.Draw(backgroundTexture, position, Color.White);
                }
            }
            
            // Draw game objects
            foreach (var gameObj in gameObjects.OrderBy(obj => obj.position.Y + (obj.sprite?.Height ?? 0) / 2f))
            {
                // inside your draw loop
                if (gameObj.sprite == null || gameObj.IsDestroyed)
                    continue;

                // 1) Pick texture + position
                var tex = gameObj.sprite;
                var position = gameObj.position;

                // 2) Pick scale & effects (fall back to GameObject's if not animated)
                SpriteEffects effect = gameObj is AnimatedGameObject a2 ? a2.spriteEffect : SpriteEffects.None;

                // 3) Build your source rectangle
                //    - full texture by default
                Rectangle? sourceRect = new Rectangle(0, 0, tex.Width, tex.Height);

                if (gameObj is AnimatedGameObject anim
                    && anim.spriteData?.animated == true
                    && anim.spriteData.frameCount > 1)
                {
                    // compute frame size
                    int fw = anim.spriteData.frameWidth > 0 ? anim.spriteData.frameWidth : tex.Width;
                    int fh = anim.spriteData.frameHeight > 0 ? anim.spriteData.frameHeight : tex.Height;
                    int maxFrame = Math.Max(1, tex.Width / fw);

                    // pick the current frame index
                    int frame = anim.currentFrame % maxFrame;
                    int x = frame * fw;
                    if (x + fw > tex.Width)
                        x = 0;  // safe-guard

                    // clamp height
                    fh = Math.Min(fh, tex.Height);

                    sourceRect = new Rectangle(x, 0, fw, fh);
                }

                Vector2 origin = new Vector2(gameObj.sprite.Width/2,gameObj.sprite.Height/2);

                // 4) Single Draw call
                spriteBatch.Draw(
                    tex,
                    position,
                    sourceRect,
                    Color.White,
                    0f,                // rotation
                    origin,      // origin
                    gameObj.scale,
                    effect,
                    0f                 // layerDepth
                );


                if (!gameObj.IsDestroyed)
                {
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
        }

        public GameOptions GetGameOptions()
        {
            return gameOptions;
        }

        public string GetCurrentRoomName()
        {
            return currentRoom?.name ?? "";
        }

        public void SetViewportDimensions(int width, int height)
        {
            viewportWidth = width;
            viewportHeight = height;
        }

        private Texture2D LoadTexture(string spriteName, bool forceReload = false)
        {
            var spritePath = Path.Combine(assetsRoot, "Sprites", spriteName);
            
            if (!File.Exists(spritePath))
            {
                Console.WriteLine($"Sprite file not found: {spritePath}");
                return null;
            }

            // Check if we need to reload (force reload or file has changed)
            if (forceReload || !textureCache.ContainsKey(spriteName))
            {
                try
                {
                    // Dispose old texture if it exists
                    if (textureCache.ContainsKey(spriteName))
                    {
                        textureCache[spriteName]?.Dispose();
                        textureCache.Remove(spriteName);
                    }

                    // Load new texture with proper alpha support
                    byte[] imageBytes = File.ReadAllBytes(spritePath);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        // Use Texture2D.FromStream with explicit format to preserve alpha
                        var texture = Texture2D.FromStream(Engine.Core.GameScript.GraphicsDevice, ms);
                        
                        // Ensure the texture has proper alpha format
                        if (texture.Format != SurfaceFormat.Color)
                        {
                            // Convert to Color format if needed
                            var colorData = new Color[texture.Width * texture.Height];
                            texture.GetData(colorData);
                            texture.Dispose();
                            
                            texture = new Texture2D(Engine.Core.GameScript.GraphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color);
                            texture.SetData(colorData);
                        }
                        
                        textureCache[spriteName] = texture;
                        Console.WriteLine($"Loaded texture: {spriteName}");
                        return texture;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture {spriteName}: {ex.Message}");
                    return null;
                }
            }

            return textureCache[spriteName];
        }

        public void ClearTextureCache()
        {
            foreach (var texture in textureCache.Values)
            {
                texture?.Dispose();
            }
            textureCache.Clear();
            Console.WriteLine("Texture cache cleared");
        }

        public Engine.Core.GameObject SpawnObject(string objectName, Vector2 position)
        {
            // Find the object file by name
            var objectsDir = Path.Combine(assetsRoot, "Objects");
            var objectPath = Path.Combine(objectsDir, $"{objectName}.eo");
            
            if (!File.Exists(objectPath))
            {
                Console.WriteLine($"Object file not found: {objectPath}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(objectPath);
                var earthObj = JsonSerializer.Deserialize<EarthObject>(json);
                
                if (earthObj == null || string.IsNullOrEmpty(earthObj.sprite))
                {
                    Console.WriteLine("Object has no sprite assigned");
                    return null;
                }

                // Load texture using the cache system
                var texture = LoadTexture(earthObj.sprite);
                if (texture == null)
                {
                    Console.WriteLine($"Failed to load sprite: {earthObj.sprite}");
                    return null;
                }
                
                var gameObj = new AnimatedGameObject
                {
                    name = earthObj.name,
                    sprite = texture,
                    position = position,
                    spriteName = earthObj.sprite,
                    spriteData = LoadSpriteData(earthObj.sprite, texture),
                    currentFrame = 0,
                    frameTimer = 0,
                    
                    // Copy lighting properties
                    emitsLight = earthObj.emitsLight,
                    lightRadius = earthObj.lightRadius,
                    lightIntensity = earthObj.lightIntensity,
                    lightColor = earthObj.lightColor
                };

                // Create script instances and call Create
                foreach (var scriptName in earthObj.scripts)
                {
                    var scriptInstance = scriptManager.CreateScriptInstanceByName(scriptName);
                    if (scriptInstance is Engine.Core.GameScript gs)
                    {
                        Console.WriteLine($"Attaching script to spawned object: {gameObj.name}");
                        gs.Attach(gameObj);
                        gameObj.scriptInstances.Add(gs);
                    }
                }

                gameObjects.Add(gameObj);
                Console.WriteLine($"Spawned object '{objectName}' at position {position}");
                return gameObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to spawn object: {ex.Message}");
                return null;
            }
        }

        public SpriteData LoadSpriteData(string spriteName, Texture2D texture)
        {
            try
            {
                var spritePath = Path.Combine(assetsRoot, "Sprites", Path.ChangeExtension(spriteName, ".sprite"));
                if (File.Exists(spritePath))
                {
                    var json = File.ReadAllText(spritePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<SpriteData>(json);
                    if (data != null)
                    {
                        // Fallbacks for missing/invalid values
                        if (data.frameWidth <= 0) data.frameWidth = texture.Width;
                        if (data.frameHeight <= 0) data.frameHeight = texture.Height;
                        if (data.frameCount <= 0) data.frameCount = 1;
                        if (data.frameSpeed <= 0) data.frameSpeed = 1.0;
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load sprite data for {spriteName}: {ex.Message}");
            }
            // Fallback: single frame, full image
            return new SpriteData
            {
                name = Path.GetFileNameWithoutExtension(spriteName),
                frameWidth = texture.Width,
                frameHeight = texture.Height,
                frameCount = 1,
                frameSpeed = 1.0,
                animated = false
            };
        }

        public List<Occluder> GetOccluders()
        {
            var occluders = new List<Occluder>();
            int debugCount = 0;
            
            foreach (var gameObj in gameObjects)
            {
                if (gameObj.sprite != null && !gameObj.IsDestroyed)
                {
                    // Only create occluders for objects that should cast shadows
                    if (ShouldCastShadow(gameObj))
                    {
                        var occluder = CreateOccluderFromGameObject(gameObj);
                        if (occluder != null)
                        {
                            occluders.Add(occluder);
                            if (debugCount < 2)
                            {
                                //Console.WriteLine($"[Occluder Debug] {gameObj.name} verts:");
                                //foreach (var v in occluder.Vertices)
                                //    Console.WriteLine($"  {v}");
                                debugCount++;
                            }
                        }
                    }
                }
            }
            
            return occluders;
        }

        private bool ShouldCastShadow(Engine.Core.GameObject gameObj)
        {
            // Skip light-emitting objects (they shouldn't block their own light!)
            if (gameObj.name == "Torch" || gameObj.name.Contains("Light") || gameObj.name.Contains("Torch"))
            {
                return false;
            }
            
            // Everything else should cast shadows
            return true;
        }

        private Occluder CreateOccluderFromGameObject(Engine.Core.GameObject gameObj)
        {
            if (gameObj.sprite == null) return null;

            // Get the sprite bounds (usually 0,0 to width,height)
            var bounds = gameObj.sprite.Bounds;

            // Offset all vertices by the object's world position!
            var pos = gameObj.position;

            var occluder = new Occluder
            {
                Position = pos,
                Sprite = Lighting2D.GenerateSilhouette(gameObj.sprite.GraphicsDevice, gameObj.sprite),
                Vertices = new List<Vector2>
                {
                    new Vector2(bounds.Left, bounds.Top) + pos,
                    new Vector2(bounds.Right, bounds.Top) + pos,
                    new Vector2(bounds.Right, bounds.Bottom) + pos,
                    new Vector2(bounds.Left, bounds.Bottom) + pos
                }
            };
            return occluder;
        }

        public List<LightSource> GetLights()
        {
            var lights = new List<LightSource>();
            
            foreach (var gameObj in gameObjects)
            {
                if (gameObj is AnimatedGameObject animObj && animObj.emitsLight)
                {
                    var light = animObj.CreateLightSource();
                    if (light != null)
                    {
                        lights.Add(light);
                    }
                }
            }
            
            return lights;
        }
    }
} 