using EarthEngineEditor;
using Editor.Windows.ImGuiWrappers;
using Engine.Core.Graphics;
using ImGuiNET;
using System.IO;
using System.Numerics;

namespace Editor.UI.Homepage
{
    /// <summary>
    /// Define a demo project wrapper for displaying projects in the homepage
    /// </summary>
    public class DemoProject : EWidget
    {
        public string ProjectPath = "";
        public string ProjectName = "";
        public EButton Project;
        Vector2 Size = new Vector2(128, 128);

        public DemoProject()
        {
            Project = new EButton("",Size);
            Project.OnClick += Open;
        }

        /// <summary>
        /// Open the demo project
        /// </summary>
        public void Open()
        {
            EditorApp.Instance.homePage.Active = false;
            EditorApp.Instance._windowManager.OpenProject(ProjectPath);
        }

        /// <summary>
        /// Render the demo project
        /// </summary>
        public override void Draw()
        {
            string label = $"{ProjectName}";
            Vector2 prev = ImGui.GetCursorPos();
            float paddingX = 24f;
            float paddingY = 48f;

            Project.Size = new Vector2(Size.X+ paddingX, Size.Y+ paddingY);
            Project.Draw();
            ImGui.SetCursorPos(prev + new Vector2(paddingX/2, paddingY/1.25f));
            IntPtr imImage = ImGuiRenderer.Instance.BindTexture(TextureLibrary.Instance.Get("DemoIcon"));

            ImGui.Image(imImage, Size);

            Vector2 textSize = ImGui.CalcTextSize(ProjectName);

            // Center horizontally (and optionally vertically)
            Vector2 textPos = prev + new Vector2(
                (Project.Size.X - textSize.X) / 2f,
                paddingY/4
            );

            ImGui.SetCursorPos(textPos);
            ImGui.Text(ProjectName);
        }
    }
}
