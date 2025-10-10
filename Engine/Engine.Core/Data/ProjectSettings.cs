/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ProjectSettings.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Engine.Core.Data
{
    public static class ProjectSettings
    {
        public static string ProjectDirectory = string.Empty;
        public static string AssetsDirectory = string.Empty;
        public static string AbsoluteProjectPath = string.Empty;
        public static string AbsoluteAssetsPath = string.Empty;
        public static string BuildPath = string.Empty;
        public static string RuntimePath = string.Empty;
        public static string RecentProjects = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "recent_projects.json"
        );
        
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            path = path.Replace('\\', '/');
            path = Regex.Replace(path, "/{2,}", "/"); // replaces any "//" or more with a single "/"
            return path.Trim('/');
        }
    }
}

