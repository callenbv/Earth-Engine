using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HideInInspectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentCategoryAttribute : Attribute
    {
        public string Category { get; }
        public ComponentCategoryAttribute(string category)
        {
            Category = category;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class EditorOnlyAttribute : Attribute { }
}
