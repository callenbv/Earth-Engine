using Engine.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Enter a new scene
        /// </summary>
        /// <param name="scene"></param>
        public static void EnterScene(SceneAsset scene)
        {
            if (scene == null)
                return;

            EngineContext.Current.NextScene = scene;
        }

        /// <summary>
        /// Unload and activate scenes
        /// </summary>
        public static void Update()
        {
            if (EngineContext.Current.NextScene != CurrentScene)
            {
                CurrentScene = EngineContext.Current.NextScene;
                CurrentScene.LoadRoom();
            }
        }
    }
}
