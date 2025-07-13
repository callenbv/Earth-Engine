using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Engine.Core.Game;
using Engine.Core.Data;
using Newtonsoft.Json;

namespace Engine.Core.Systems.Rooms
{
    public class Room
    {
        public string background { get; set; } = "";
        public bool backgroundTiled { get; set; } = false;
        public int width { get; set; } = 800;
        public int height { get; set; } = 600;
        public List<EarthObject> objects { get; set; } = new List<EarthObject>();

        /// <summary>
        /// Load the default room
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public static void LoadDefaultRoom(GraphicsDevice graphicsDevice, string roomsDir, string assetRoot, ContentManager contentManager, object scriptManager, GameOptions gameOptions = null)
        {
            // Use provided game options or fall back to singleton
            var options = GameOptions.Main;

            if (string.IsNullOrEmpty(options.defaultRoom))
            {
                // Try to load the first available room
                if (Directory.Exists(roomsDir))
                {
                    var roomFiles = Directory.GetFiles(roomsDir, "*.room");
                    if (roomFiles.Length > 0)
                    {
                        var firstRoom = Path.GetFileNameWithoutExtension(roomFiles[0]);
                        LoadRoom(firstRoom, roomsDir, assetRoot, contentManager, scriptManager, graphicsDevice);
                        return;
                    }
                }
                Console.WriteLine("No rooms found to load.");
                return;
            }

            LoadRoom(options.defaultRoom, roomsDir, assetRoot, contentManager, scriptManager, graphicsDevice);
        }

        /// <summary>
        /// Loads a room
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="roomsDir"></param>
        /// <param name="graphicsDevice"></param>
        public static void LoadRoom(string roomName, string roomsDir, string assetsRoot, ContentManager contentManager, object scriptManager, GraphicsDevice graphicsDevice)
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
                GameObjectManager.Main.Clear();

                var json = File.ReadAllText(roomPath);

                var roomData = JsonConvert.DeserializeObject<Room>(json);

                if (roomData?.objects != null)
                {
                    Console.WriteLine($"Creating {roomData.objects.Count} objects...");

                    foreach (var obj in roomData.objects)
                    {
                        try
                        {
                            // Extract object name from path if objectName is not set
                            string? objectName = obj.name;
                            if (string.IsNullOrEmpty(objectName) && !string.IsNullOrEmpty(obj.objectPath))
                            {
                                objectName = Path.GetFileNameWithoutExtension(obj.objectPath);
                            }

                            if (!string.IsNullOrEmpty(objectName))
                            {
                                var position = new Vector2((float)obj.x, (float)obj.y);
                                var gameObject = GameObject.Instantiate(objectName, position);

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
    }
}
