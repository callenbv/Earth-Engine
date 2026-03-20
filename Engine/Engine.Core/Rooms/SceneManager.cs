/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2026 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Engine.Core.Data;
using Engine.Core.Game;
using MonoGame.Extended.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Engine.Core.Rooms
{
    /// <summary>
    /// Scene manager to easily call scene updates
    /// </summary>
    public static class SceneManager
    {
        /// <summary>
        /// The current scene we are in
        /// </summary>
        public static SceneAsset? CurrentScene;

        /// <summary>
        /// The Scene data
        /// </summary>
        public static Room? CurrentSceneData;

        /// <summary>
        /// Enter a new scene
        /// </summary>
        /// <param name="scene"></param>
        public static void EnterScene(SceneAsset scene)
        {
            if (scene == null)
                return;

            CurrentScene = scene;
            CurrentSceneData = CurrentScene.LoadRoom();
        }

        /// <summary>
        /// Unload and activate scenes
        /// </summary>
        public static void Update()
        {

        }

        /// <summary>
        /// Returns a list of all active game objects. Empty if bad scene
        /// </summary>
        /// <returns></returns>
        public static List<GameObject> GetAllActiveObjects()
        {
            return (CurrentSceneData == null ? new List<GameObject>() : CurrentSceneData.objects);
        }

        /// <summary>
        /// Add a new object to the scene
        /// </summary>
        /// <param name="obj"></param>
        public static void AddObject(GameObject obj)
        {
            if (CurrentSceneData == null)
                return;

            CurrentSceneData.objects.Add(obj);
        }

        public static void RemoveObject(GameObject obj)
        {
            if (CurrentSceneData == null)
                return;

            obj.Destroy();
            CurrentSceneData.objects.Remove(obj);
        }

        /// <summary>
        /// Save the current scene
        /// </summary>
        /// <returns></returns>
        public static bool SaveScene()
        {
            if (CurrentSceneData == null)
            {
                Console.Error.WriteLine("Tried to save NULL scene");
                return false;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true,
                    Converters = { new ComponentListJsonConverter() },
                };
                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new Vector3JsonConverter());
                options.Converters.Add(new ColorJsonConverter());

                string json = JsonSerializer.Serialize(CurrentSceneData, options);
                Console.WriteLine($"Scene writing: {EnginePaths.AssetsBase} to {CurrentSceneData.FilePath}");
                File.WriteAllText($"{EnginePaths.AssetsBase}/{CurrentSceneData.FilePath}", json);
                Console.WriteLine($"Scene saved: {CurrentSceneData.Name} to {CurrentSceneData.FilePath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save scene: {ex.Message}");
            }

            return true;
        }
    }
}

