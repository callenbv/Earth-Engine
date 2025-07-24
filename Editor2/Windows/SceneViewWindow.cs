using Editor.AssetManagement;
using Editor.Windows.Inspector;
using Engine.Core;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Text;

namespace EarthEngineEditor.Windows
{
    public class SceneViewWindow
    {
        private bool _showSceneView = true;
        public Room? scene;
        private GameObject? _selectedObject;
        public static SceneViewWindow Instance { get; private set; }
        private int gridSize = 8;
        private bool _showRenamePopup = false;
        private string _renameBuffer = "";
        private string currentName = "";
        private GameObject? _nodeBeingRenamed;
        private bool _isRenaming = false;
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

        /// <summary>
        /// Render the scene hierarchy of objects
        /// </summary>
        private void RenderHierarchy()
        {
            bool root = ImGui.TreeNodeEx("Scene");

            // Get the mouse world coords and select the object
            if (EditorApp.Instance.gameFocused)
            {
                if (Input.IsMousePressed())
                {
                    foreach (var obj in scene.objects)
                    {
                        Sprite2D sprite = obj.GetComponent<Sprite2D>();
                        if (sprite == null) continue;

                        Vector2 pos = obj.Position;
                        Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(
                            (int)(pos.X - sprite.origin.X),
                            (int)(pos.Y - sprite.origin.Y),
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
                            _selectedObject.Position = Input.mouseWorldPosition;
                        }
                    }
                }

                if (Input.IsMouseReleased())
                {
                    _selectedObject = null;
                }
            }

            // Right-click on the "Scene" tree node
            if (ImGui.BeginPopupContextItem("SceneContext"))
            {
                if (ImGui.MenuItem("Create Blank GameObject"))
                {
                    var newObj = new GameObject($"Empty{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
                    newObj.AddComponent<Sprite2D>();
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

        /// <summary>
        /// Draw a game object node and its children
        /// </summary>
        /// <param name="obj"></param>
        private void DrawGameObjectNode(GameObject obj)
        {
            ImGui.PushID(obj.Name); // Ensure unique ID

            bool hasChildren = obj.children != null && obj.children.Count > 0;
            bool open = false;
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;

            if (hasChildren)
                open = ImGui.TreeNodeEx(obj.Name);
            else
                open = ImGui.TreeNodeEx(obj.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);

            var nodeId = obj.Name.GetHashCode();

            // If currently renaming THIS node, draw InputText instead of label
            if (_isRenaming && _nodeBeingRenamed == obj)
            {
                ImGui.PushItemWidth(200); // prevent layout shifting
                if (ImGui.InputText("##renameNode", ref _renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    obj.Name = _renameBuffer.Trim();
                    _isRenaming = false;
                    _nodeBeingRenamed = null;
                    InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
                }

                // Cancel rename on ESC or click away
                if (!ImGui.IsItemActive() && (ImGui.IsMouseClicked(0) || ImGui.IsKeyPressed(ImGuiKey.Escape)))
                {
                    _isRenaming = false;
                    _nodeBeingRenamed = null;
                }
                ImGui.PopItemWidth();
            }
            else
            {
                // Context menu for Rename and Delete
                if (ImGui.BeginPopupContextItem($"ObjectContext_{nodeId}"))
                {
                    if (ImGui.MenuItem("Rename"))
                    {
                        _isRenaming = true;
                        _renameBuffer = obj.Name;
                        _nodeBeingRenamed = obj;
                    }

                    if (ImGui.MenuItem("Delete"))
                    {
                        obj.Destroy();
                    }
                    ImGui.EndPopup();
                }
            }

            // Rename
            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F2))
            {
                _isRenaming = true;
                _renameBuffer = obj.Name;
                _nodeBeingRenamed = obj;
            }

            if (ImGui.IsItemClicked())
            {
                // Handle selection
                InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
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