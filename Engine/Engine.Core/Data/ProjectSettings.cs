using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    public static class ProjectSettings
    {
        public static string ProjectDirectory = string.Empty;
        public static string AssetsDirectory = string.Empty;
        public static string AbsoluteProjectPath = string.Empty;
        public static string AbsoluteAssetsPath = string.Empty;
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
