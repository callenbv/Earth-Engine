/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AssignableHelper.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Helper utilities for working with IAssignable references in the editor                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using Engine.Core.Game;
using Engine.Core;
using System.Reflection;

namespace Engine.Core.Data
{
    /// <summary>
    /// Helper class for working with IAssignable references in the editor.
    /// Provides utilities to check field types and get all available assignable objects.
    /// 
    /// Usage in Editor:
    /// To draw an IAssignable field in the inspector:
    /// 1. Check if the field is assignable: AssignableHelper.IsAssignableField(field)
    /// 2. Get available assignables: var assignables = AssignableHelper.GetAssignablesForType(field.FieldType)
    /// 3. Display them in a dropdown/selector similar to GameObject/Component fields
    /// 4. When user selects one, set it: field.SetValue(component, selectedAssignable)
    /// 
    /// The serialization will automatically handle the conversion using AssignableReferenceConverter.
    /// </summary>
    public static class AssignableHelper
    {
        /// <summary>
        /// Checks if a field or property type is assignable (IAssignable).
        /// </summary>
        public static bool IsAssignableType(Type type)
        {
            return typeof(IAssignable).IsAssignableFrom(type);
        }

        /// <summary>
        /// Checks if a field info represents an IAssignable field.
        /// </summary>
        public static bool IsAssignableField(FieldInfo field)
        {
            return IsAssignableType(field.FieldType);
        }

        /// <summary>
        /// Checks if a property info represents an IAssignable property.
        /// </summary>
        public static bool IsAssignableProperty(PropertyInfo property)
        {
            return IsAssignableType(property.PropertyType);
        }

        /// <summary>
        /// Gets all available assignable objects in the current scene.
        /// This includes all Components, GameObjects, and Assets.
        /// </summary>
        public static List<IAssignable> GetAllAssignables()
        {
            var assignables = new List<IAssignable>();

            var scene = EngineContext.Current.Scene;
            if (scene != null)
            {
                // Add all GameObjects
                foreach (var gameObject in scene.objects)
                {
                    assignables.Add(gameObject);

                    // Add all Components from each GameObject
                    foreach (var component in gameObject.components)
                    {
                        if (component is IAssignable assignableComponent)
                        {
                            assignables.Add(assignableComponent);
                        }
                    }
                }
            }

            // Add all Assets from ProjectWindow (Editor only)
            // Use reflection to avoid hard dependency on Editor assembly
            try
            {
                var projectWindowType = Type.GetType("EarthEngineEditor.Windows.ProjectWindow, Editor");
                if (projectWindowType != null)
                {
                    var instanceProperty = projectWindowType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            var allAssetsProperty = projectWindowType.GetProperty("allAssets");
                            if (allAssetsProperty != null)
                            {
                                var allAssets = allAssetsProperty.GetValue(instance) as System.Collections.IEnumerable;
                                if (allAssets != null)
                                {
                                    foreach (var asset in allAssets)
                                    {
                                        if (asset is IAssignable assignableAsset)
                                        {
                                            assignables.Add(assignableAsset);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // ProjectWindow not available or reflection failed - silently continue
            }

            return assignables;
        }

        /// <summary>
        /// Gets all assignable objects of a specific type.
        /// </summary>
        public static List<T> GetAssignablesOfType<T>() where T : IAssignable
        {
            var assignables = GetAllAssignables();
            return assignables.OfType<T>().ToList();
        }

        /// <summary>
        /// Gets a display name for an IAssignable object.
        /// </summary>
        public static string GetDisplayName(IAssignable? assignable)
        {
            if (assignable == null)
                return "None";

            // Try to get a name property
            if (assignable is IComponent component)
                return $"{component.Name} ({assignable.GetType().Name})";
            
            if (assignable is GameObject gameObject)
                return $"{gameObject.Name} (GameObject)";

            // Check if it's an Asset (Editor.AssetManagement.Asset)
            try
            {
                var assetType = assignable.GetType();
                if (assetType.Name == "Asset" && assetType.Namespace == "Editor.AssetManagement")
                {
                    var nameProperty = assetType.GetProperty("Name");
                    var typeProperty = assetType.GetProperty("Type");
                    if (nameProperty != null && typeProperty != null)
                    {
                        var name = nameProperty.GetValue(assignable)?.ToString() ?? "Unknown";
                        var type = typeProperty.GetValue(assignable)?.ToString() ?? "Asset";
                        return $"{name} ({type})";
                    }
                }
            }
            catch
            {
                // Reflection failed - fall through to default
            }

            return assignable.GetType().Name;
        }

        /// <summary>
        /// Gets a unique identifier for an IAssignable object for serialization.
        /// This returns the appropriate identifier based on the type:
        /// - Component: ID (int as string)
        /// - GameObject: GUID (string)
        /// - Asset: Path (string)
        /// </summary>
        public static string GetAssignableIdentifier(IAssignable? assignable)
        {
            if (assignable == null)
                return string.Empty;

            if (assignable is IComponent component && component is ObjectComponent objComp)
                return objComp.ID.ToString();
            
            if (assignable is GameObject gameObject)
                return gameObject.ID.ToString();

            // Check if it's an Asset (Editor.AssetManagement.Asset)
            try
            {
                var assetType = assignable.GetType();
                if (assetType.Name == "Asset" && assetType.Namespace == "Editor.AssetManagement")
                {
                    var pathProperty = assetType.GetProperty("Path");
                    if (pathProperty != null)
                    {
                        var path = pathProperty.GetValue(assignable)?.ToString();
                        if (!string.IsNullOrEmpty(path))
                            return path;
                    }
                }
            }
            catch
            {
                // Reflection failed - fall through to default
            }

            return string.Empty;
        }

        /// <summary>
        /// Finds an IAssignable by its identifier.
        /// </summary>
        public static IAssignable? FindAssignableByIdentifier(string identifier, Type? targetType = null)
        {
            if (string.IsNullOrEmpty(identifier))
                return null;

            var scene = EngineContext.Current.Scene;
            if (scene == null)
                return null;

            // Try to find as Component (integer ID)
            if (int.TryParse(identifier, out int componentId))
            {
                foreach (var gameObject in scene.objects)
                {
                    foreach (var component in gameObject.components)
                    {
                        if (component is ObjectComponent objComp && objComp.ID == componentId)
                        {
                            if (targetType == null || targetType.IsAssignableFrom(component.GetType()))
                            {
                                return component;
                            }
                        }
                    }
                }
            }

            // Try to find as GameObject (GUID)
            if (Guid.TryParse(identifier, out Guid gameObjectId))
            {
                var gameObject = scene.objects.FirstOrDefault(o => o.ID == gameObjectId);
                if (gameObject != null && (targetType == null || targetType.IsAssignableFrom(typeof(GameObject))))
                {
                    return gameObject;
                }
            }

            // Try to find as Asset (Editor.AssetManagement.Asset) by path
            try
            {
                var projectWindowType = Type.GetType("EarthEngineEditor.Windows.ProjectWindow, Editor");
                if (projectWindowType != null)
                {
                    var instanceProperty = projectWindowType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            var getMethod = projectWindowType.GetMethod("Get");
                            if (getMethod != null)
                            {
                                var asset = getMethod.Invoke(instance, new object[] { identifier });
                                if (asset is IAssignable assignableAsset)
                                {
                                    if (targetType == null || targetType.IsAssignableFrom(asset.GetType()))
                                    {
                                        return assignableAsset;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Asset not found or reflection failed
            }

            return null;
        }

        /// <summary>
        /// Gets a filtered list of assignables that can be assigned to a specific field type.
        /// </summary>
        public static List<IAssignable> GetAssignablesForType(Type fieldType)
        {
            if (!IsAssignableType(fieldType))
                return new List<IAssignable>();

            var allAssignables = GetAllAssignables();
            
            // Special handling for SceneAsset - only show Scene assets
            if (fieldType == typeof(SceneAsset) || fieldType.Name == "SceneAsset")
            {
                return allAssignables.Where(IsSceneAsset).ToList();
            }

            // If it's a concrete type (not IAssignable itself), filter by that type
            if (fieldType != typeof(IAssignable))
            {
                return allAssignables.Where(a => fieldType.IsAssignableFrom(a.GetType())).ToList();
            }

            // Otherwise return all assignables
            return allAssignables;
        }

        /// <summary>
        /// Checks if an assignable is a Scene asset
        /// </summary>
        private static bool IsSceneAsset(IAssignable assignable)
        {
            try
            {
                var assetType = assignable.GetType();
                if (assetType.Name == "Asset" && assetType.Namespace == "Editor.AssetManagement")
                {
                    var typeProperty = assetType.GetProperty("Type");
                    if (typeProperty != null)
                    {
                        var assetTypeValue = typeProperty.GetValue(assignable);
                        return assetTypeValue?.ToString() == "Scene";
                    }
                }
            }
            catch
            {
                // Reflection failed
            }

            return false;
        }
    }
}

