/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneViewWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Editor.Windows.Inspector;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Text;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents the Scene View window in the editor, allowing users to view and manipulate game objects in a scene.
    /// </summary>
    public class SceneViewWindow
    {
        public Room? scene;
        private GameObject? _selectedObject;
        private GameObject? previousSelection;
        private GameObject? _nodeBeingRenamed;
        public static int gridSize = 16;
        private string _renameBuffer = "";
        private bool _isRenaming = false;
        private bool _showSceneView = true;
        public static SceneViewWindow Instance { get; private set; }

        /// <summary>
        /// Singleton instance of the SceneViewWindow
        /// </summary>
        public SceneViewWindow()
        {
            Instance = this;
        }

        /// <summary>
        /// Render the scene view and enable object selection mode
        /// </summary>
        public void Render()
        {
            if (ImGui.Begin("Scene View", ref _showSceneView))
            {
                EditorApp.Instance.selectionMode = EditorSelectionMode.Object;
            }

            if (_showSceneView)
            {
                RenderHierarchy();
            }

            ImGui.End();
        }

        /// <summary>
        /// Render the scene hierarchy of objects
        /// </summary>
        private void RenderHierarchy()
        {
            if (scene == null)
            {
                ImGui.Text("No scene open");
                return;
            }

            // Draw scene title
            ImGui.Text($"{scene.Name}");
            ImGui.Separator();

            // Draw the scene root node
            bool root = ImGui.TreeNodeEx("Scene");

            // Get the mouse world coords and select the object
            if (EditorApp.Instance.gameFocused && EditorApp.Instance.selectionMode == EditorSelectionMode.Object)
            {
                if (Input.IsMousePressed())
                {
                    foreach (var obj in scene.objects)
                    {
                        Sprite2D? sprite = obj.GetComponent<Sprite2D>();
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
                            if (previousSelection == obj)
                            {
                                _selectedObject = obj;
                            }
                            previousSelection = obj;
                            InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
                            break;
                        }
                    }
                }

                if (Input.IsMouseDown())
                {
                    if (_selectedObject != null)
                    {
                        string text = _selectedObject.Name;
                        var drawList = ImGui.GetForegroundDrawList();
                        System.Numerics.Vector2 mousePos = ImGui.GetMousePos() + new System.Numerics.Vector2(8,-8);
                        System.Numerics.Vector2 textSize = ImGui.CalcTextSize(text);

                        float padding = 4f;
                        System.Numerics.Vector2 min = mousePos;
                        System.Numerics.Vector2 max = min + textSize + new System.Numerics.Vector2(padding * 2, padding * 2);

                        // Background box
                        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 0, 0, 0.7f)), 4f);

                        // Text
                        drawList.AddText(min + new System.Numerics.Vector2(padding, padding), ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1, 1, 1, 1)), text);

                        _selectedObject.Position = Vector2.Floor(Input.mouseWorldPosition / gridSize) * gridSize;
                    }
                }

                if (Input.IsMouseReleased())
                {
                    _selectedObject = null;
                }
            }

            // Delete
            if (_selectedObject != null && Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                _selectedObject.Destroy();
                _selectedObject = null; // Clear selection
            }

            // Right-click on the "Scene" tree node
            if (ImGui.BeginPopupContextItem("SceneContext"))
            {
                if (ImGui.MenuItem("Create Empty GameObject"))
                {
                    var newObj = new GameObject($"Empty{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
                    newObj.AddComponent<Sprite2D>();
                    scene.objects.Add(newObj);
                }
                if (ImGui.MenuItem("Create 2D Lighting"))
                {
                    var newObj = new GameObject($"Lighting{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
                    newObj.AddComponent<Lighting2D>();
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

            bool hasChildren = (obj.children != null && obj.children.Count > 0);
            bool open = false;

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

            // Quick rename F2
            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F2))
            {
                _isRenaming = true;
                _renameBuffer = obj.Name;
                _nodeBeingRenamed = obj;
            }

            // Inspect an item in the scene
            if (ImGui.IsItemClicked())
            {
                InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
                Camera.Main.Position = obj.Position;
            }

            if (open && hasChildren && obj.children != null)
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
