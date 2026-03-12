/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EButton.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2026 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.ImGuiWrappers
{
    /// <summary>
    /// Wrapper for ImGui button
    /// </summary>
    public class EButton : EWidget
    {
        public string Text { get; set; } = string.Empty;
        public Sprite2D? Sprite { get; set; }
        public Action? OnClick { get; set; }
        public Vector2 Size { get; set; } = Vector2.One;

        public EButton()
        {

        }

        /// <summary>
        /// Create a button with text
        /// </summary>
        /// <param name="text"></param>
        public EButton(string text, Vector2 size)
        {
            Text = text;
            Size = size;
        }

        /// <summary>
        /// Bind a click action
        /// </summary>
        /// <param name="action"></param>
        public void Bind(Action action)
        {
            OnClick = action;
        }

        /// <summary>
        /// Render the button
        /// </summary>
        public override void Draw()
        {
            bool button = ImGui.Button($"{Text}##{GetHashCode()}",Size);

            if (button)
            {
                if (OnClick != null)
                {
                    OnClick.Invoke();
                }
            }
        }
    }
}

