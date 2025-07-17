using Editor.AssetManagement;
using ImGuiNET;
using System.IO;

namespace EarthEngineEditor.Windows
{
    public class InspectorWindow
    {
        private bool _showInspector = true;
        public static InspectorWindow Instance { get; private set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Asset? selectedItem;


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
            ImGui.Text($"{selectedItem.Type}");
            ImGui.Separator();

            selectedItem.RenderEditor();

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
        public void Inspect(Asset item)
        {
            if (item == null) return;
            selectedItem = item;

            // Set the title and display name
            Title = item.Name;
            Description = item.Path;

            // Open the asset if possible
            selectedItem.Open();
        }
    }
} 