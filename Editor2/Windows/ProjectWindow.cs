/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ProjectWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using ImGuiNET;
using System.IO;
using System.Numerics;
using Editor.AssetManagement;
using Engine.Core.Data;
using System.Text;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Core.Game;
using Editor.Windows.Inspector;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents the project window in the editor, allowing users to manage assets and folders.
    /// </summary>
    public class ProjectWindow
    {
        private bool _showProject = true;
        private string _currentFolder = "";
        private List<Asset> items = new();
        private List<Asset> allAssets = new();
        private Asset? _selectedItem = null;

        // Folder creation dialog
        private bool _showCreateFolderDialog = false;
        private string _newFolderName = "New Folder";

        // Asset creation dialog
        private bool _showNewAssetDialog = false;
        private string _newAssetName = string.Empty;
        private AssetType _selectedAssetType = AssetType.Prefab;
        public static ProjectWindow Instance { get; set; }
        private GCHandle? _dragHandle = null;
        private byte[]? _dragData = null;
        private bool _awaitingDrop = false;

        /// <summary>
        /// Singleton instance of the ProjectWindow
        /// </summary>
        public ProjectWindow()
        {
            Instance = this;
        }

        /// <summary>
        /// Imports a file into the project, copying it to the Assets directory and registering it as an asset.
        /// </summary>
        /// <param name="fullFilePath"></param>
        public void TryImportFile(string fullFilePath)
        {
            if (!File.Exists(fullFilePath))
            {
                Console.WriteLine($"[IMPORT] File does not exist: {fullFilePath}");
                return;
            }

            string extension = Path.GetExtension(fullFilePath).ToLowerInvariant();
            AssetType type = Asset.GetAssetTypeFromExtension(fullFilePath);

            if (type == AssetType.Unknown)
            {
                Console.WriteLine($"[IMPORT] Unsupported file type: {extension}");
                return;
            }

            string fileName = Path.GetFileName(fullFilePath);

            // Destination path inside the Assets folder (preserve current folder)
            string relTargetPath = Path.Combine(_currentFolder ?? "", fileName);
            string absTargetPath = Path.Combine(ProjectSettings.AssetsDirectory, relTargetPath);

            // Create target directory if needed
            Directory.CreateDirectory(Path.GetDirectoryName(absTargetPath)!);

            try
            {
                File.Copy(fullFilePath, absTargetPath, overwrite: true);

                Console.WriteLine($"[IMPORT] Imported {fileName} to project folder: {relTargetPath}");

                // Register as an asset
                var asset = new Asset
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    Path = ProjectSettings.NormalizePath(relTargetPath),
                    Type = type,
                    FileIcon = Asset.GetIconForType(type),
                    Folder = false
                };

                allAssets.Add(asset);
                RefreshItems();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMPORT] Failed to import file: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the asset given the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Asset? Get(string name)
        {
            foreach (var asset in allAssets)
            {
                if (asset.Name == name)
                    return asset;
            }

            return null;
        }

        /// <summary>
        /// Try to save any changes made to assets
        /// </summary>
        public void Save()
        {
            try
            {
                foreach (var item in allAssets)
                {
                    item.Save();
                }
                Console.WriteLine("[DONE] Saved Project");
            }
            catch (Exception e) 
            {
                Console.WriteLine("[ERROR] "+e.ToString());
            }
        }

        /// <summary>
        /// Renders the project window, displaying the list of assets and folders in a grid layout.
        /// </summary>
        public void Render()
        {
            if (!_showProject) return;

            ImGui.Begin("Project", ref _showProject);

            // Search bar
            //ImGui.InputText("Search", ref _searchText, 128);

            // Project items
            RenderProjectItems();

            // Context menu
            RenderContextMenu();
            // Folder creation dialog
            if (_showCreateFolderDialog)
                RenderCreateFolderDialog();

            if (_showNewAssetDialog)
                RenderNewAssetDialog();

            ImGui.End();
        }

        /// <summary>
        /// Draws the breadcrumb navigation for the current folder path.
        /// </summary>
        private void DrawBreadcrumb()
        {
            ImGui.Text("Path: ");
            ImGui.SameLine();

            // Start from root (Assets)
            if (ImGui.SmallButton("Assets"))
            {
                _currentFolder = "";
                _selectedItem = null;
                RefreshItems();
            }

            if (!string.IsNullOrEmpty(_currentFolder))
            {
                string[] parts = _currentFolder.Split('/');
                string runningPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    ImGui.SameLine();
                    ImGui.Text(">");
                    ImGui.SameLine();

                    string part = parts[i];
                    runningPath = string.IsNullOrEmpty(runningPath) ? part : $"{runningPath}/{part}";

                    if (ImGui.SmallButton(part))
                    {
                        _currentFolder = runningPath;
                        _selectedItem = null;
                        RefreshItems();
                    }
                }
            }

            ImGui.Separator();
        }

        /// <summary>
        /// Renders the project items in a grid layout with icons and text.
        /// </summary>
        private void RenderProjectItems()
        {
            DrawBreadcrumb();

            float itemWidth = 100f;
            float itemHeight = 20f;
            float padding = 4f;
            float textWidth = 100f;

            float panelWidth = ImGui.GetContentRegionAvail().X;
            float xPos = 0;

            foreach (var item in items)
            {
                // Show only items directly in the current folder
                bool isSelected = _selectedItem == item;
                string label = $"{item.Name}##{item.Path}";
                ImGui.PushID(label);
                ImGui.BeginGroup();

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                drawList.ChannelsSplit(2);
                drawList.ChannelsSetCurrent(1); // Draw content

                Vector2 startPos = ImGui.GetCursorScreenPos();
                float iconSize = ImGui.GetFontSize();
                float spacing = 4f;
                float wrapWidth = textWidth - 8f;

                // Reserve space *before* padding
                float estimatedHeight = iconSize + spacing + ImGui.GetTextLineHeightWithSpacing() * 2;
                ImGui.InvisibleButton("selectable", new Vector2(itemWidth, estimatedHeight));
                bool hovered = ImGui.IsItemHovered();

                // Move cursor down for content only
                Vector2 contentStartPos = startPos + new Vector2(0, 16f);
                ImGui.SetCursorScreenPos(contentStartPos);

                // Icon
                string icon = item.Folder ? "\uf07b" : item.FileIcon;
                Vector2 iconSizeVec = ImGui.CalcTextSize(icon);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (itemWidth - iconSizeVec.X) * 0.5f);
                ImGui.Text(icon);
                Vector2 iconMin = ImGui.GetItemRectMin();
                Vector2 iconMax = ImGui.GetItemRectMax();

                // Text (wrapped and centered)
                ImGui.PushTextWrapPos(contentStartPos.X + wrapWidth);
                Vector2 textStart = ImGui.GetCursorScreenPos();
                float wrappedTextWidth = ImGui.CalcTextSize(item.Name, wrapWidth).X;
                ImGui.SetCursorScreenPos(new Vector2(
                    contentStartPos.X + (itemWidth - wrappedTextWidth) * 0.5f,
                    textStart.Y
                ));
                ImGui.TextWrapped(item.Name);
                ImGui.PopTextWrapPos();

                Vector2 textMin = ImGui.GetItemRectMin();
                Vector2 textMax = ImGui.GetItemRectMax();

                // Highlight area
                Vector2 highlightMin = startPos - new Vector2(0,padding);
                Vector2 highlightMax = startPos + new Vector2(itemWidth, estimatedHeight) + new Vector2(0, padding);

                drawList.ChannelsSetCurrent(0);
                if (hovered || isSelected)
                {
                    uint color = isSelected
                        ? ImGui.GetColorU32(ImGuiCol.Header)
                        : ImGui.GetColorU32(ImGuiCol.HeaderHovered);
                    drawList.AddRectFilled(highlightMin, highlightMax, color, 4.0f);
                }
                drawList.ChannelsMerge();

                ImGui.EndGroup();
                ImGui.PopID();

                xPos += itemWidth + padding;

                if (xPos + itemWidth > panelWidth)
                {
                    xPos = 0;
                }
                else
                {
                    ImGui.SameLine();
                }

                // Each asset needs a unique ID in ImGui
                if (_selectedItem != null)
                {
                    if (ImGui.IsItemActive() && ImGui.BeginDragDropSource())
                    {
                        string str = _selectedItem.Name;

                        if (!_awaitingDrop)
                        {
                            _dragData = Encoding.UTF8.GetBytes(str);
                            _dragHandle = GCHandle.Alloc(_dragData, GCHandleType.Pinned);
                            _awaitingDrop = true;

                            Console.WriteLine("[DEBUG] Payload pinned");
                        }

                        IntPtr ptr = _dragHandle.Value.AddrOfPinnedObject();
                        ImGui.SetDragDropPayload("TEST_PAYLOAD", ptr, (uint)_dragData.Length);
                        ImGui.Text($"{str}");

                        ImGui.EndDragDropSource();
                    }
                }

                // Drag files into scene
                if (!ImGui.GetIO().WantCaptureMouse && Input.IsMouseReleased())
                {
                    if (_awaitingDrop && _selectedItem.Type == AssetType.Prefab)
                    {
                        GameObject obj = GameObject.Instantiate(_selectedItem.Path, Input.mouseWorldPosition);
                        InspectableGameObject inspectableGameObject = new InspectableGameObject(obj);
                        InspectorWindow.Instance.Inspect(inspectableGameObject);
                        _awaitingDrop = false;
                        _dragHandle?.Free();
                        _dragHandle = null;
                        _dragData = null;
                    }
                }

                // Move file
                Vector2 min = ImGui.GetItemRectMin();
                Vector2 max = ImGui.GetItemRectMax();

                if (ImGui.IsMouseHoveringRect(min, max) && Input.IsMouseReleased() && _awaitingDrop)
                {
                    string targetFolderRel = item.Path;
                    string sourceRel = _selectedItem?.Path;

                    if (_selectedItem.Folder || targetFolderRel == GetParentPath(sourceRel) || targetFolderRel == sourceRel)
                    {
                        // Ignore moving folders or moving into current folder
                        _awaitingDrop = false;
                        return;
                    }

                    string sourceAbs = Path.Combine(ProjectSettings.AssetsDirectory, sourceRel);
                    string fileName = Path.GetFileName(sourceRel);
                    string newRelPath = Path.Combine(targetFolderRel, fileName);
                    string newAbsPath = Path.Combine(ProjectSettings.AssetsDirectory, newRelPath);

                    // Make sure target folder exists
                    Directory.CreateDirectory(Path.GetDirectoryName(newAbsPath)!);

                    try
                    {
                        File.Move(sourceAbs, newAbsPath, overwrite: true);
                        Console.WriteLine($"[MOVE] {sourceRel} → {newRelPath}");

                        _selectedItem.Path = ProjectSettings.NormalizePath(newRelPath);
                        _selectedItem.Folder = false;
                        RefreshItems();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to move: {ex.Message}");
                    }

                    _awaitingDrop = false;
                    _dragHandle?.Free();
                    _dragHandle = null;
                    _dragData = null;
                    break;
                }

                // Right-click select
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    _selectedItem = item;
                    InspectorWindow.Instance.Inspect(item);
                }

                // Left-click inspect
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                {
                    if (item.Folder)
                    {
                        _currentFolder = item.Path;
                        _selectedItem = null;
                        RefreshItems();
                        break;
                    }
                    else
                    {
                        // Show inspector and open asset
                        if (_selectedItem != item)
                        {
                            _selectedItem = item;
                            item.Open();
                            InspectorWindow.Instance.Inspect(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the parent path of the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            path = path.Replace('\\', '/').Trim('/');

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
                return ""; // No slash = top-level folder

            return path.Substring(0, lastSlash);
        }

        /// <summary>
        /// Renders the context menu for the project window, allowing users to create folders, assets, and import files.
        /// </summary>
        private void RenderContextMenu()
        {
            if (ImGui.BeginPopupContextWindow("ProjectContextMenu"))
            {
                if (ImGui.MenuItem("Create Folder"))
                {
                    _showCreateFolderDialog = true;
                    _newFolderName = "New Folder";
                }
                if (ImGui.MenuItem("Create Asset"))
                {
                    _showNewAssetDialog = true;
                }
                if (ImGui.MenuItem("Import Asset"))
                {
                    using var dialog = new OpenFileDialog();
                    dialog.Title = "Select Assets to Import";
                    dialog.Filter = "All Files (*.*)|*.*";
                    dialog.Multiselect = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string currentFolder = Path.Combine(ProjectSettings.AssetsDirectory, _currentFolder);

                        foreach (string selectedPath in dialog.FileNames)
                        {
                            if (File.Exists(selectedPath))
                            {
                                string destPath = Path.Combine(currentFolder, Path.GetFileName(selectedPath));
                                File.Copy(selectedPath, destPath, overwrite: true);
                                Console.WriteLine($"[Import] Imported: {destPath}");
                            }
                        }

                        RefreshItems();
                    }
                }
                if (_selectedItem != null)
                {
                    ImGui.Separator();
                    if (ImGui.MenuItem("Rename"))
                    {
                        // To be implemented...
                    }
                    if (ImGui.MenuItem("Delete"))
                    {
                        string path = Path.Combine(ProjectSettings.AssetsDirectory, _selectedItem.Path);

                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path);
                        }
                        else
                        {
                            _selectedItem.Delete(path);
                        }

                        _selectedItem = null;
                        RefreshItems();
                    }
                }
                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// Renders the dialog for creating a new folder in the project.
        /// </summary>
        private void RenderCreateFolderDialog()
        {
            ImGui.OpenPopup("Create Folder");
            if (ImGui.BeginPopupModal("Create Folder", ref _showCreateFolderDialog, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Enter folder name:");
                ImGui.InputText("##FolderName", ref _newFolderName, 64);
                ImGui.Spacing();
                if (ImGui.Button("Create", new Vector2(120, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(_newFolderName))
                    {
                        string currentFolder = Path.Combine(ProjectSettings.AssetsDirectory, _currentFolder);
                        var folderPath = Path.Combine(currentFolder, _newFolderName);
                        Console.WriteLine($"Creating folder: {folderPath}");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                            items.Add(new Asset
                            {
                                Name = _newFolderName,
                                Path = folderPath,
                                Folder = true
                            });
                            Console.WriteLine($"Folder created successfully: {folderPath}");
                            RefreshItems();
                        }
                        else
                        {
                            Console.WriteLine($"Folder already exists: {folderPath}");
                        }
                        _showCreateFolderDialog = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showCreateFolderDialog = false;
                }
                ImGui.EndPopup();
            }
        }


        /// <summary>
        /// Refreshes the list of items in the project window.
        /// </summary>
        public void RefreshItems()
        {
            items.Clear();
            allAssets.Clear();

            string absAssetsPath = ProjectSettings.AssetsDirectory; // Full path to "Assets"
            string absCurrentPath = Path.Combine(absAssetsPath, _currentFolder ?? "").Replace('\\', '/');

            if (!Directory.Exists(absCurrentPath))
                return;

            try
            {
                if (!string.IsNullOrEmpty(_currentFolder))
                {
                    var parentRelPath = GetParentPath(_currentFolder);
                    items.Add(new Asset
                    {
                        Name = "..",
                        Path = parentRelPath,  // relative path!
                        Folder = true
                    });
                }

                // Get all assets
                PopulateAllAssetsRecursively(absAssetsPath);

                var folders = Directory.GetDirectories(absCurrentPath);
                foreach (var folder in folders)
                {
                    string folderName = Path.GetFileName(folder);
                    string relPath = ProjectSettings.NormalizePath(Path.GetRelativePath(ProjectSettings.AssetsDirectory, folder));

                    items.Add(new Asset
                    {
                        Name = folderName,
                        Path = relPath, // store relative path
                        Folder = true,
                    });
                }

                foreach (var asset in allAssets)
                {
                    if (GetParentPath(asset.Path) == _currentFolder)
                    {
                        items.Add(asset);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively populates the list of all assets in the project directory.
        /// </summary>
        /// <param name="directory"></param>
        private void PopulateAllAssetsRecursively(string directory)
        {
            var folders = Directory.GetDirectories(directory);
            foreach (var folder in folders)
                PopulateAllAssetsRecursively(folder);

            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string relPath = ProjectSettings.NormalizePath(Path.GetRelativePath(ProjectSettings.AssetsDirectory, file));

                if (!allAssets.Any(a => a.Path == relPath))
                {
                    AssetType type = Asset.GetAssetTypeFromExtension(fileName);
                    allAssets.Add(new Asset
                    {
                        Name = fileName,
                        Path = relPath,
                        Folder = false,
                        Type = type,
                        FileIcon = Asset.GetIconForType(type)
                    });
                }
            }
        }

        /// <summary>
        /// Renders the dialog for creating a new asset in the project.
        /// </summary>
        private void RenderNewAssetDialog()
        {
            ImGui.OpenPopup("New Asset");

            if (ImGui.BeginPopupModal("New Asset", ref _showNewAssetDialog, ImGuiWindowFlags.AlwaysAutoResize))
            {
                // Input: Asset Name
                ImGui.Text("Asset Name:");
                ImGui.InputText("##NewAssetName", ref _newAssetName, 256);

                ImGui.Spacing();

                // Dropdown: Asset Type
                if (ImGui.BeginCombo("Asset Type", _selectedAssetType.ToString()))
                {
                    foreach (AssetType type in Enum.GetValues(typeof(AssetType)))
                    {
                        if (type == AssetType.Unknown)
                            continue;

                        bool isSelected = _selectedAssetType == type;
                        if (ImGui.Selectable(type.ToString(), isSelected))
                            _selectedAssetType = type;

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                ImGui.Spacing();

                // Create Button
                if (ImGui.Button("Create", new Vector2(120, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(_newAssetName))
                    {
                        string extension = Asset.GetExtensionFromType(_selectedAssetType);
                        string fileName = _newAssetName + extension;
                        string fullPath = Path.Combine(ProjectSettings.AssetsDirectory, _currentFolder ?? "", fileName);

                        // Create the new file (basic template content)
                        File.WriteAllText(fullPath, Asset.GenerateTemplateForAssetType(_selectedAssetType, _newAssetName));
                        RefreshItems();
                        _showNewAssetDialog = false;
                    }
                }

                ImGui.SameLine();

                // Cancel Button
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showNewAssetDialog = false;
                }

                ImGui.EndPopup();
            }
        }

        public bool IsVisible => _showProject;
        public void SetVisible(bool visible) => _showProject = visible;
    }
} 
