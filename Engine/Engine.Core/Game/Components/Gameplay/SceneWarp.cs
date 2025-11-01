/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneWarp.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Causes this component to warp to the target scene when colliding         
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Rooms;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// A component for warping to a target scene when colliding with a specified object.
    /// </summary>
    [ComponentCategory("Gameplay")]
    public class SceneWarp : ObjectComponent
    {
        public override string Name => "Warp Transition";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// Object to check warp for
        /// </summary>
        public GameObject WarpObject { get; set; }

        /// <summary>
        /// The scene asset we want to go to. Assign a Scene asset in the inspector.
        /// </summary>
        public SceneAsset TargetSceneAsset { get; set; }

        /// <summary>
        /// If we have warped (prevent warping too much at once)
        /// </summary>
        private bool Warped = false;

        /// <summary>
        /// When colliding with the target object, it will go to the target scene
        /// </summary>
        /// <param name="other"></param>
        public override void OnCollisionTrigger(Collider2D other)
        {
            if (other.Owner?.ID == WarpObject.ID)
            {
                Warp();
            }
        }

        /// <summary>
        /// Warps to the target scene asset
        /// </summary>
        public void Warp()
        {
            if (Warped || TargetSceneAsset == null || string.IsNullOrEmpty(TargetSceneAsset.Path))
                return;

            Warped = true;

            // Load the room from the scene asset
            SceneManager.EnterScene(TargetSceneAsset);
        }
    }
} 
