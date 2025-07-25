/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         InspectorWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Engine.Core.Data;
using ImGuiNET;
using Editor.Windows.Inspector;
using Engine.Core;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents the Inspector window in the editor, allowing users to inspect and modify game objects and assets.
    /// </summary>
    public class InspectorWindow
    {
        private bool _showInspector = true;
        public static InspectorWindow Instance { get; private set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IInspectable? selectedItem;

        public InspectorWindow()
        {
            Instance = this;
        }

        /// <summary>
        /// Render the inspector panel
        /// </summary>
        public void Render()
        {
            if (!_showInspector) return;

            ImGui.Begin("Inspector", ref _showInspector);
            ImGui.Text($"{Title}");
            ImGui.Separator();

            selectedItem?.Render();
            Update();

            ImGui.End();
        }

        /// <summary>
        /// Update the inspector window
        /// </summary>
        public void Update()
        {
            if (selectedItem != null)
            {
                // Delete
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Delete))
                {
                    if (selectedItem is InspectableGameObject obj)
                    {
                        obj.GameObject?.Destroy();
                        selectedItem = null;
                    }
                }
            }
            else
            {
                Title = string.Empty;
            }
        }

        /// <summary>
        /// Set the visibility flag
        /// </summary>
        public bool IsVisible => _showInspector;

        /// <summary>
        /// Set the inspector to be visible
        /// </summary>
        /// <param name="visible"></param>
        public void SetVisible(bool visible) => _showInspector = visible;
       
        /// <summary>
        /// Trigger an inspect
        /// </summary>
        /// <param name="item"></param>
        public void Inspect(IInspectable item)
        {
            if (item == null) return;

            selectedItem = item;

            if (item is Asset asset)
            {
                // Set the title and display name
                Title = $"{asset.Name}\n{asset.Type}";
            }

            if (item is InspectableGameObject gameObject)
            {
                Title = gameObject.Name;
            }
        }
    }
} 
