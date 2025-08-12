/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AboutWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using ImGuiNET;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents the About window in the editor, displaying information about the Earth Engine Editor.
    /// </summary>
    public class AboutWindow
    {
        private bool _showAboutWindow = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class.
        /// </summary>
        public void Render()
        {
            if (!_showAboutWindow) return;

            ImGui.Begin("About Earth Engine Editor", ref _showAboutWindow);
            ImGui.Text("Earth Engine Editor v1.0");
            ImGui.Text("A modern game engine editor built with MonoGame and ImGui");
            ImGui.Separator();
            ImGui.Text("Features:");
            ImGui.BulletText("Windows, Linux, MacOS exports");
            ImGui.BulletText("Custom GPIO Input");
            ImGui.Separator();
            ImGui.End();
        }

        public bool IsVisible => _showAboutWindow;
        public void SetVisible(bool visible) => _showAboutWindow = visible;
    }
} 
