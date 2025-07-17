using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Systems.Rooms;
using ImGuiNET;
using System.Numerics;

namespace EarthEngineEditor.Windows
{
    public class SceneViewWindow
    {
        private bool _showSceneView = true;
        public Room? scene;
        private GameObject? _selectedObject;
        public static SceneViewWindow Instance { get; private set; }

        public SceneViewWindow()
        {
            Instance = this;
        }

        public void Render()
        {
            ImGui.Begin("Scene View", ref _showSceneView);

            if (_showSceneView && scene != null)
            {
                ImGui.Text($"{scene.Name}");
                ImGui.Separator();
                RenderHierarchy();
            }

            ImGui.End();
        }

        private void RenderHierarchy()
        {
            bool root = ImGui.TreeNodeEx("Scene");

            // Right-click on the "Scene" tree node
            if (ImGui.BeginPopupContextItem("SceneContext"))
            {
                if (ImGui.MenuItem("Create Blank GameObject"))
                {
                    var newObj = new GameObject("Empty");
                    Sprite2D sprite = new Sprite2D();
                    sprite.Set("Player");
                    newObj.AddComponent(sprite);
                    scene.objects.Add(newObj);
                }

                ImGui.EndPopup();
            }

            if (root)
            {
                foreach (var obj in scene.objects)
                {
                    DrawGameObjectNode(obj);
                }

                ImGui.TreePop();
            }
        }

        private void DrawGameObjectNode(GameObject obj)
        {
            ImGui.PushID(obj.Name); // Ensure unique ID

            bool hasChildren = obj.children != null && obj.children.Count > 0;

            bool open = false;
            if (hasChildren)
                open = ImGui.TreeNodeEx(obj.Name);
            else
                open = ImGui.TreeNodeEx(obj.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);

            if (ImGui.IsItemClicked())
            {
                // Handle selection
                _selectedObject = obj;
                InspectorWindow.Instance.Inspect(obj);
            }

            if (open && hasChildren)
            {
                foreach (var child in obj.children)
                {
                    DrawGameObjectNode(child);
                }

                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        public bool IsVisible => _showSceneView;
        public void SetVisible(bool visible) => _showSceneView = visible;
    }
} 