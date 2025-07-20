using Editor.AssetManagement;
using Editor.Windows.Inspector;
using Engine.Core;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace EarthEngineEditor.Windows
{
    public class SceneViewWindow
    {
        private bool _showSceneView = true;
        public Room? scene;
        private GameObject? _selectedObject;
        public static SceneViewWindow Instance { get; private set; }
        private int gridSize = 8;

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

            // Get the mouse world coords and select the object
            if (Input.IsMousePressed())
            {
                foreach (var obj in scene.objects)
                {
                    Sprite2D sprite = obj.GetComponent<Sprite2D>();
                    if (sprite == null) continue;

                    Vector2 pos = obj.position;
                    Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(
                        (int)(pos.X-sprite.origin.X),
                        (int)(pos.Y-sprite.origin.Y),
                        sprite.spriteBox.Width,
                        sprite.spriteBox.Height
                        );

                    if (Input.MouseHover(rect))
                    {
                        _selectedObject = obj;
                        InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
                        break;
                    }
                }
            }

            if (Input.IsMouseDown())
            {
                if (_selectedObject != null)
                {
                    Transform t = _selectedObject.GetComponent<Transform>();

                    if (t != null)
                    {
                        t.Position = Vector2.Floor(Input.mouseWorldPosition / gridSize) * gridSize;
                    }
                    else
                    {
                        _selectedObject.position = Input.mouseWorldPosition;
                    }
                }
            }

            if (Input.IsMouseReleased())
            {
                _selectedObject = null;
            }

            // Right-click on the "Scene" tree node
            if (ImGui.BeginPopupContextItem("SceneContext"))
            {
                if (ImGui.MenuItem("Create Blank GameObject"))
                {
                    var newObj = new GameObject("Empty");
                    Sprite2D sprite = new Sprite2D();
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
                InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));

                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F2))
                {

                }
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