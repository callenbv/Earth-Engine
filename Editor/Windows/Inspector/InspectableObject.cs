/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         InspectableObject.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Game;

namespace Editor.Windows.Inspector
{
    /// <summary>
    /// Represents an inspectable game object in the editor.
    /// </summary>
    public class InspectableGameObject : IInspectable
    {
        private readonly IComponentContainer obj;
        public string Name = string.Empty;
        public GameObject? GameObject => obj as GameObject;

        /// <summary>
        /// Creates an instance of InspectableGameObject with the specified IComponentContainer.
        /// </summary>
        /// <param name="obj_"></param>
        public InspectableGameObject(IComponentContainer obj_)
        {
            obj = obj_;
            Name = obj.Name;
        }

        /// <summary>
        /// Renders the inspectable game object in the inspector window.
        /// </summary>
        public void Render()
        {
            InspectorUI.DrawGameObject(this.obj);
        }
    }
}

