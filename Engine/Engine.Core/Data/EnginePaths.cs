/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EnginePaths.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.IO;

namespace Engine.Core.Data
{
    /// <summary>
    /// Static class to hold paths for the Earth Engine project and assets.
    /// </summary>
    public static class EnginePaths
    {
        private static string? _sharedContentPath;
        
        /// <summary>
        /// Gets the shared content path, dynamically resolving to the current user's directory
        /// </summary>
        public static string SHARED_CONTENT_PATH 
        { 
            get 
            {
                if (_sharedContentPath == null)
                {
                    // Get the current user's profile directory
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    _sharedContentPath = Path.Combine(userProfile, "Desktop", "Earth-Engine", "Content", "bin", "Windows");
                }
                return _sharedContentPath;
            }
        }
        
        public static string ProjectBase = string.Empty;
        public static string AssetsBase = string.Empty;
    }
} 
