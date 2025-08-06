/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Comments.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System.Reflection;
using System.Xml;

namespace Engine.Core.Data
{
    /// <summary>
    /// A static class that provides functionality to load and manage XML comments for properties and methods.
    /// </summary>
    public static class Comments
    {
        /// <summary>
        /// A dictionary that maps property names to their XML comments.
        /// </summary>
        public static Dictionary<string, string> propertyTooltips = new Dictionary<string, string>();

        /// <summary>
        /// Initializes the Comments class by loading XML comments from a specified file path.
        /// </summary>
        public static void Initialize()
        {
            // Load XML comments from the specified file path
            propertyTooltips = LoadXmlComments("Comments.xml");
        }

        /// <summary>
        /// Loads XML comments from a specified file path and returns a dictionary mapping property names to their comments.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<string,string> LoadXmlComments(string filePath)
        {
            var comments = new Dictionary<string, string>();

            // Load the XML file
            if (File.Exists(filePath))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                // Iterate through each property element in the XML
                foreach (XmlNode node in xmlDoc.SelectNodes("//member"))
                {
                    if (node.Attributes != null)
                    {
                        string name = node.Attributes["name"]?.Value;
                        string summary = node.SelectSingleNode("summary")?.InnerText.Trim();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(summary))
                        {
                            comments[name] = summary;
                        }
                    }
                }
            }
            return comments;
        }

        /// <summary>
        /// Gets the XML documentation member key for a given member (property, method, or type).
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetXmlDocMemberKey(MemberInfo member)
        {
            if (member is PropertyInfo prop)
            {
                return $"P:{prop.DeclaringType.FullName}.{prop.Name}";
            }
            else if (member is MethodInfo method)
            {
                string paramList = string.Join(",", method.GetParameters()
                    .Select(p => p.ParameterType.FullName));
                return $"M:{method.DeclaringType.FullName}.{method.Name}({paramList})";
            }
            else if (member is Type type)
            {
                return $"T:{type.FullName}";
            }

            return null;
        }
    }
}

