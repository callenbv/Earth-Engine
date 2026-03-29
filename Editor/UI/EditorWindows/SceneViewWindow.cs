/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneViewWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.Windows.Inspector;
using Editor.AssetManagement;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Windows.Forms;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents a folder in the scene hierarchy, which can contain other folders or game objects.
    /// </summary>
    public class SceneFolder : IInspectable
    {
        public string Name;
        public List<SceneFolder> SubFolders = new();
        public List<GameObject> GameObjects = new();

        public SceneFolder(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents the Scene View window in the editor, allowing users to view and manipulate game objects in a scene.
    /// </summary>
    public class SceneViewWindow
    {
        public Room? scene
        {
            get => SceneManager.CurrentSceneData;
        }

        private GameObject? _selectedObject;
        private GameObject? _selectedHierarchyObject;
        private GameObject? _draggedHierarchyObject;
        private GameObject? _copiedHierarchyObject;
        private GameObject? previousSelection;
        private IInspectable? _nodeBeingRenamed;
        public static int gridSize = 16;
        private string _renameBuffer = "";
        private bool _isRenaming = false;
        private bool _showSceneView = true;
        public static SceneViewWindow Instance { get; private set; }
        public static bool UIMode = false;
        public SceneFolder rootFolder = new SceneFolder("Root");

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

            if (_showSceneView && scene != null)
            {
                // Toggle UI mode on/off
                ImGui.Checkbox("UI Mode", ref UIMode);
                EngineContext.UIOnly = UIMode;

                // Render the hierarchy
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

            if (!_isRenaming && _selectedHierarchyObject != null && Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F2))
            {
                _isRenaming = true;
                _renameBuffer = _selectedHierarchyObject.Name;
                _nodeBeingRenamed = _selectedHierarchyObject;
            }

            HandleCopyPasteShortcuts();

            // Get the mouse world coords and select the object
            if (EditorApp.Instance.gameFocused && EditorApp.Instance.selectionMode == EditorSelectionMode.Object)
            {
                if (Input.IsMousePressed())
                {
                    foreach (var obj in scene.objects)
                    {
                        if (!obj.Active)
                            continue;

                        Microsoft.Xna.Framework.Rectangle rect = obj.GetBoundingBox();

                        if (Input.MouseHover(rect))
                        {
                            if (previousSelection == obj)
                            {
                                _selectedObject = obj;
                            }
                            previousSelection = obj;
                            _selectedHierarchyObject = obj;
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

                        _selectedObject.Position = new Vector3(
                            Vector2.Floor(new Vector2(Input.mouseWorldPosition.X, Input.mouseWorldPosition.Y) / gridSize) * gridSize,
                            0f
                        );
                    }
                }

                if (Input.IsMouseReleased())
                {
                    _selectedObject = null;
                }
            }

            // Draw the actual nodes in the tree
            DrawRootDropTarget();
        }

        /// <summary>
        /// Draw a game object node and its children
        /// </summary>
        /// <param name="obj"></param>
        private bool DrawGameObjectNode(GameObject obj)
        {
            bool drawNode = true;

            ImGui.PushID(obj.Name); // Ensure unique ID

            bool hasChildren = (obj.children != null && obj.children.Count > 0);
            bool open = false;
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

            if (_selectedHierarchyObject == obj)
                flags |= ImGuiTreeNodeFlags.Selected;

            if (hasChildren)
                open = ImGui.TreeNodeEx(obj.Name, flags);
            else
                open = ImGui.TreeNodeEx(obj.Name, flags | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);

            // Select only on a plain click, not when beginning a drag operation.
            if (ImGui.IsItemHovered() &&
                ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _selectedHierarchyObject = obj;
                InspectorWindow.Instance.Inspect(new InspectableGameObject(obj));
            }

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
                    if (ImGui.MenuItem("Create Empty GameObject"))
                    {
                        var newObj = new GameObject($"Empty{scene.objects.Count}");
                        newObj.AddComponent<Transform>();
                        newObj.SetParent(obj);
                        scene.objects.Add(newObj);
                    }

                    if (ImGui.MenuItem("Create 2D Lighting"))
                    {
                        var newObj = new GameObject($"Lighting{scene.objects.Count}");
                        newObj.AddComponent<Transform>();
                        newObj.AddComponent<Lighting2D>();
                        newObj.SetParent(obj);
                        scene.objects.Add(newObj);
                    }

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

            if (ImGui.BeginDragDropSource())
            {
                ImGui.SetDragDropPayload("GAMEOBJECT", IntPtr.Zero, 0);
                ImGui.Text(obj.Name);
                _draggedHierarchyObject = obj;
                PrefabHandler.SetDraggedGameObject(obj);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT");
                    if (payload.NativePtr != null && _draggedHierarchyObject != null)
                    {
                        var dragged = _draggedHierarchyObject;
                        if (dragged != obj && !obj.IsDescendantOf(dragged))
                        {
                            dragged.SetParent(obj);
                        }
                    }
                }

                ImGui.EndDragDropTarget();
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

            if (obj.IsDestroyed)
            {
                drawNode = false;
            }

            return drawNode;
        }

        private void DrawRootDropTarget()
        {
            bool open = ImGui.TreeNodeEx(
                "Scene Root",
                ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen);

            if (ImGui.BeginPopupContextItem("SceneRootContext"))
            {
                if (ImGui.MenuItem("Create Empty GameObject"))
                {
                    var newObj = new GameObject($"Empty{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
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

            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT");
                    if (payload.NativePtr != null && _draggedHierarchyObject != null)
                    {
                        _draggedHierarchyObject.SetParent(null);
                    }
                }

                ImGui.EndDragDropTarget();
            }

            if (!open)
                return;

            foreach (var obj in scene.objects.Where(o => o.Parent == null).ToList())
            {
                DrawGameObjectNode(obj);
            }

            ImGui.TreePop();
        }

        public bool IsVisible => _showSceneView;
        public void SetVisible(bool visible) => _showSceneView = visible;

        private void HandleCopyPasteShortcuts()
        {
            bool ctrlDown =
                Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

            if (!ctrlDown || scene == null || _isRenaming)
                return;

            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.C) && _selectedHierarchyObject != null)
            {
                _copiedHierarchyObject = _selectedHierarchyObject;
            }

            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.V) && _copiedHierarchyObject != null)
            {
                PasteCopiedHierarchy();
            }
        }

        private void PasteCopiedHierarchy()
        {
            if (scene == null || _copiedHierarchyObject == null)
                return;

            var clones = Room.DuplicateHierarchy(_copiedHierarchyObject);
            if (clones.Count == 0)
                return;

            var cloneRoot = clones.FirstOrDefault(obj => obj.Parent == null);
            if (cloneRoot == null)
                return;

            foreach (var clone in clones)
            {
                scene.objects.Add(clone);
            }

            cloneRoot.SetParent(_copiedHierarchyObject.Parent);
            cloneRoot.Position += new Vector3(gridSize, 0f, 0f);
            cloneRoot.Name = GetDuplicateName(_copiedHierarchyObject.Name);

            _selectedHierarchyObject = cloneRoot;
            InspectorWindow.Instance.Inspect(new InspectableGameObject(cloneRoot));
        }

        private string GetDuplicateName(string sourceName)
        {
            if (scene == null)
                return sourceName;

            string baseName = $"{sourceName} Copy";
            string candidate = baseName;
            int index = 2;

            while (scene.objects.Any(obj => obj.Name == candidate))
            {
                candidate = $"{baseName} {index}";
                index++;
            }

            return candidate;
        }
    }
} 
