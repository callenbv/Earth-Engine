using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Editor.AssetManagement;
using Engine.Core.Data;

namespace EarthEngineEditor.Windows
{
    public class ProjectWindow
    {
        private bool _showProject = true;
        private string _currentProjectPath = "";
        private string _currentFolder = "";
        private List<ProjectItem> items = new();
        private ProjectItem? _selectedItem = null;
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

        public ProjectWindow()
        {

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
            float padding = 10f;

            float panelWidth = ImGui.GetContentRegionAvail().X;
            float xPos = 0;

            foreach (var item in items)
            {
                // Show only items directly in the current folder
                string parentPath = GetParentPath(item.Path);

                if (parentPath != _currentFolder)
                    continue;

                var isSelected = _selectedItem == item;
                string label = $"{item.Name}##{item.Path}";

                if (ImGui.Selectable(label, isSelected, ImGuiSelectableFlags.None, new System.Numerics.Vector2(itemWidth, itemHeight)))
                {
                    _selectedItem = item;
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
                        InspectorWindow.Instance.Inspect(item);
                    }
                }

                xPos += itemWidth + padding;

                if (xPos + itemWidth > panelWidth)
                {
                    xPos = 0;
                }
                else
                {
                    ImGui.SameLine();
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
                    }
                    if (ImGui.MenuItem("Delete"))
                    {
                        string fileName = _selectedItem.Path;
                        string fullPath = Path.Combine(ProjectSettings.AssetsDirectory, fileName);
                        File.Delete(fullPath);
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
                            items.Add(new ProjectItem
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

            string absAssetsPath = ProjectSettings.AssetsDirectory; // Full path to "Assets"
            string absCurrentPath = Path.Combine(absAssetsPath, _currentFolder ?? "").Replace('\\', '/');

            if (!Directory.Exists(absCurrentPath))
                return;

            try
            {
                if (!string.IsNullOrEmpty(_currentFolder))
                {
                    var parentRelPath = GetParentPath(_currentFolder);
                    items.Add(new ProjectItem
                    {
                        Name = "..",
                        Path = parentRelPath,  // relative path!
                        Folder = true
                    });
                }

                var folders = Directory.GetDirectories(absCurrentPath);
                foreach (var folder in folders)
                {
                    string folderName = Path.GetFileName(folder);
                    string relPath = ProjectSettings.NormalizePath(Path.GetRelativePath(ProjectSettings.AssetsDirectory, folder));

                    items.Add(new ProjectItem
                    {
                        Name = folderName,
                        Path = relPath, // store relative path
                        Folder = true
                    });
                }

                var files = Directory.GetFiles(absCurrentPath);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string relPath = ProjectSettings.NormalizePath(Path.GetRelativePath(ProjectSettings.AssetsDirectory, file));

                    items.Add(new ProjectItem
                    {
                        Name = fileName,
                        Path = relPath, // store relative path
                        Folder = false,
                        Type = ProjectItem.GetAssetTypeFromExtension(fileName)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
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
                        File.WriteAllText(fullPath, Asset.GenerateTemplateForAssetType(_selectedAssetType));
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