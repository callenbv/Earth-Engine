/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneViewWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.Windows.Inspector;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
        public Room? scene;
        private GameObject? _selectedObject;
        private GameObject? previousSelection;
        private IInspectable? _nodeBeingRenamed;
        public static int gridSize = 16;
        private string _renameBuffer = "";
        private bool _isRenaming = false;
        private bool _showSceneView = true;
        public static SceneViewWindow Instance { get; private set; }
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
                SyncUnfolderedObjects();
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

            // Get the mouse world coords and select the object
            if (EditorApp.Instance.gameFocused && EditorApp.Instance.selectionMode == EditorSelectionMode.Object)
            {
                if (Input.IsMousePressed())
                {
                    foreach (var obj in scene.objects)
                    {
                        Microsoft.Xna.Framework.Rectangle rect = obj.GetBoundingBox();

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

            // Draw the actual nodes in the tree
            // We also draw a folder view here for better organization
            DrawFolderNode(rootFolder);
        }
        private bool DrawFolderNode(SceneFolder folder)
        {
            bool open = ImGui.TreeNodeEx($"{folder.Name}");

            if (ImGui.BeginPopupContextItem($"FolderContext_{folder.Name}"))
            {
                if (ImGui.MenuItem("Create Folder"))
                {
                    folder.SubFolders.Add(new SceneFolder($"Group{rootFolder.SubFolders.Count}"));
                }
                if (ImGui.MenuItem("Create Empty GameObject"))
                {
                    var newObj = new GameObject($"Empty{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
                    newObj.AddComponent<Sprite2D>();
                    scene.objects.Add(newObj);
                    folder.GameObjects.Add(newObj);
                }
                if (ImGui.MenuItem("Create 2D Lighting"))
                {
                    var newObj = new GameObject($"Lighting{scene.objects.Count}");
                    newObj.AddComponent<Transform>();
                    newObj.AddComponent<Lighting2D>();
                    scene.objects.Add(newObj);
                    folder.GameObjects.Add(newObj);
                }
                if (ImGui.MenuItem("Rename"))
                {
                    _isRenaming = true;
                    _renameBuffer = folder.Name;
                    _nodeBeingRenamed = folder;
                }
                if (ImGui.MenuItem("Delete"))
                {
                    rootFolder.SubFolders.Remove(folder);
                    return false;
                }
                ImGui.EndPopup();
            }

            // Drop target for GameObjects
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    if (ImGui.AcceptDragDropPayload("GAMEOBJECT").NativePtr != null)
                    {
                        var dragged = _selectedObject;
                        if (dragged != null && !folder.GameObjects.Contains(dragged))
                        {
                            RemoveFromAllFolders(dragged, rootFolder); // Remove from all folders
                            folder.GameObjects.Add(dragged);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
            }

            // If currently renaming THIS node, draw InputText instead of label
            if (_isRenaming && _nodeBeingRenamed == folder)
            {
                ImGui.PushItemWidth(200); // prevent layout shifting
                if (ImGui.InputText("##renameNode", ref _renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    folder.Name = _renameBuffer.Trim();
                    _isRenaming = false;
                    _nodeBeingRenamed = null;
                }

                // Cancel rename on ESC or click away
                if (!ImGui.IsItemActive() && (ImGui.IsMouseClicked(0) || ImGui.IsKeyPressed(ImGuiKey.Escape)))
                {
                    _isRenaming = false;
                    _nodeBeingRenamed = null;
                }
                ImGui.PopItemWidth();
            }

            if (open)
            {
                foreach (var sub in folder.SubFolders)
                {
                    bool success = DrawFolderNode(sub);

                    if (!success)
                        break;
                }

                foreach (var obj in folder.GameObjects)
                {
                    bool sucess = DrawGameObjectNode(obj);

                    if (!sucess)
                        break;
                }

                ImGui.TreePop();
            }

            return true;
        }

        /// <summary>
        /// Synchronize unfoldered objects to the root folder
        /// </summary>
        private void SyncUnfolderedObjects()
        {
            foreach (var obj in scene.objects)
            {
                if (!IsGroupedInAnyFolder(obj))
                {
                    rootFolder.GameObjects.Add(obj);
                }
            }
        }

        /// <summary>
        /// Check if a game object is grouped in any folder in the hierarchy
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool IsGroupedInAnyFolder(GameObject obj)
        {
            bool Check(SceneFolder folder)
            {
                if (folder.GameObjects.Contains(obj))
                    return true;

                foreach (var sub in folder.SubFolders)
                    if (Check(sub)) return true;

                return false;
            }

            return Check(rootFolder);
        }

        /// <summary>
        /// Remove a game object from all folders in the hierarchy
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="folder"></param>
        private void RemoveFromAllFolders(GameObject obj, SceneFolder folder)
        {
            folder.GameObjects.Remove(obj);

            foreach (var sub in folder.SubFolders)
            {
                RemoveFromAllFolders(obj, sub);
            }
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

            if (hasChildren)
                open = ImGui.TreeNodeEx(obj.Name);
            else
                open = ImGui.TreeNodeEx(obj.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);

            // Inspect an item in the scene
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
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

            if (ImGui.BeginDragDropSource())
            {
                ImGui.SetDragDropPayload("GAMEOBJECT", IntPtr.Zero, 0); // No payload data needed, use context
                ImGui.Text(obj.Name);
                _selectedObject = obj; // Store reference in your editor context
                ImGui.EndDragDropSource();
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
                RemoveFromAllFolders(obj, rootFolder); // Remove from all folders
                drawNode = false;
            }

            return drawNode;
        }

        public bool IsVisible => _showSceneView;
        public void SetVisible(bool visible) => _showSceneView = visible;
    }
} 
