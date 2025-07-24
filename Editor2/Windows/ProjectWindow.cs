/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ProjectWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Editor.AssetManagement;
using Engine.Core.Data;
using System.Text;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Core.Game;

namespace EarthEngineEditor.Windows
{
    public class ProjectWindow
    {
        private bool _showProject = true;
        private string _currentProjectPath = "";
        private string _currentFolder = "";
        private List<Asset> items = new();
        private List<Asset> allAssets = new();
        private Asset? _selectedItem = null;
        private string _searchText = "";

        // Folder creation dialog
        private bool _showCreateFolderDialog = false;
        private string _newFolderName = "New Folder";
        private string? _pendingFolderPath = null;

        // Asset import dialog
        private bool _showImportAssetDialog = false;
        private string _importAssetPath = "";
        private string? _pendingImportPath = null;

        // Asset creation dialog
        private bool _showNewAssetDialog = false;
        private string _newAssetName = string.Empty;
        private AssetType _selectedAssetType = AssetType.Prefab;
        public static ProjectWindow Instance { get; set; }
        private GCHandle? _dragHandle = null;
        private byte[]? _dragData = null;
        private bool _awaitingDrop = false;
        public ProjectWindow()
        {
            Instance = this;
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

        public void SetProjectPath(string projectPath)
        {
            _currentProjectPath = ProjectSettings.ProjectDirectory;
            _currentFolder = "";
            RefreshItems();
        }

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

            // Asset import dialog
            if (_showImportAssetDialog)
                RenderImportAssetDialog();

            if (_showNewAssetDialog)
                RenderNewAssetDialog();

            ImGui.End();
        }

        void DrawBreadcrumb()
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
                string icon = item.Folder ? "\uf07b" : "\uf15b";
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

                if (!ImGui.GetIO().WantCaptureMouse && Input.IsMouseReleased())
                {
                    if (_awaitingDrop)
                    {
                        GameObject.Instantiate(_selectedItem.Path, Input.mouseWorldPosition);
                        _awaitingDrop = false;
                        _dragHandle?.Free();
                        _dragHandle = null;
                        _dragData = null;
                        Console.WriteLine("[DEBUG] Dropped payload");
                    }
                }

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
                        _selectedItem = item;
                        InspectorWindow.Instance.Inspect(item);
                    }
                }
            }
        }
       
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
                    _showImportAssetDialog = true;
                    _importAssetPath = "";
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
                        _selectedItem.Delete(path);
                        _selectedItem = null;
                        RefreshItems();
                    }
                }
                ImGui.EndPopup();
            }
        }

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
                        var folderPath = Path.Combine(_currentFolder, _newFolderName);
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

        private void RenderImportAssetDialog()
        {
            ImGui.OpenPopup("Import Asset");
            if (ImGui.BeginPopupModal("Import Asset", ref _showImportAssetDialog, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("##ImportAssetPath", ref _importAssetPath, 256);
                ImGui.Spacing();
                if (ImGui.Button("Import", new Vector2(120, 0)))
                {
                    if (File.Exists(_importAssetPath))
                    {
                        var destPath = Path.Combine(_currentFolder, Path.GetFileName(_importAssetPath));
                        File.Copy(_importAssetPath, destPath, overwrite: true);
                        RefreshItems();
                        _showImportAssetDialog = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showImportAssetDialog = false;
                }
                ImGui.EndPopup();
            }
        }

        private void RefreshItems()
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
                        Folder = true
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
                    allAssets.Add(new Asset
                    {
                        Name = fileName,
                        Path = relPath,
                        Folder = false,
                        Type = Asset.GetAssetTypeFromExtension(fileName)
                    });
                }
            }
        }
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
