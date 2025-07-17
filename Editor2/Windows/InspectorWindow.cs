using Editor.AssetManagement;
using Engine.Core.Data;
using Engine.Core.Game;
using ImGuiNET;
using System.IO;
using System.Device.Gpio;

namespace EarthEngineEditor.Windows
{
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
            if (!_showInspector || selectedItem == null) return;

            ImGui.Begin("Inspector", ref _showInspector);
            ImGui.Text($"{Title}");
            ImGui.Separator();

            selectedItem.Render();

            ImGui.End();
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
                Title = asset.Name;

                // Open the asset if possible
                asset.Open();
            }

            if (item is GameObject gameObject)
            {
                Title = gameObject.Name;
            }
        }
    }
} 