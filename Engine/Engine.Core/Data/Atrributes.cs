/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Atrributes.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Define attributes for serialization and editor inputs          
/// -----------------------------------------------------------------------------

namespace Engine.Core.Data
{
    /// <summary>
    /// Attribute to mark a property or field as editable in the editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HideInInspectorAttribute : Attribute { }

    /// <summary>
    /// Attribute to mark a class as a component category for organization in the editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentCategoryAttribute : Attribute
    {
        public string Category { get; }
        public ComponentCategoryAttribute(string category)
        {
            Category = category;
        }
    }

    /// <summary>
    /// Attribute to mark a method as editor-only, meaning it should not be called in the final build of the game.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EditorOnlyAttribute : Attribute { }

    /// <summary>
    /// This text field allows multilines (new lines)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MultilineTextAttribute : Attribute { }

    /// <summary>
    /// Attribute to mark a property or field as editable in the editor with a slider.
    /// </summary>

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SliderEditorAttribute : Attribute
    {
        public float Min { get; }
        public float Max { get; }

        public SliderEditorAttribute(float min = 0f, float max = 1f)
        {
            Min = min;
            Max = max;
        }
    }
}

