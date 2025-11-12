using Engine.Core.Game.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.ImGuiWrappers
{
    /// <summary>
    /// Wrapper for ImGui widgets base class
    /// </summary>
    public class EWidget
    {
        public bool Show { get; set; } = true;
        public bool Hovering { get; set; } = false;

        /// <summary>
        /// Render the button
        /// </summary>
        public virtual void Draw() { }
    }
}
