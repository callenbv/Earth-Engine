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
    public class AboutWindow
    {
        private bool _showAboutWindow = false;

        public void Render()
        {
            if (!_showAboutWindow) return;

            ImGui.Begin("About Earth Engine Editor", ref _showAboutWindow);
            ImGui.Text("Earth Engine Editor v1.0");
            ImGui.Text("A modern game engine editor built with MonoGame and ImGui");
            ImGui.Separator();
            ImGui.Text("Features:");
            ImGui.BulletText("Modern dark theme");
            ImGui.BulletText("Dockable windows");
            ImGui.BulletText("Asset management");
            ImGui.BulletText("Scene editing");
            ImGui.BulletText("Console window");
            ImGui.BulletText("Performance monitoring");
            ImGui.Separator();
            ImGui.Text("Built with:");
            ImGui.BulletText("MonoGame Framework");
            ImGui.BulletText("ImGui.NET");
            ImGui.BulletText(".NET 8");
            ImGui.End();
        }

        public bool IsVisible => _showAboutWindow;
        public void SetVisible(bool visible) => _showAboutWindow = visible;
    }
} 
