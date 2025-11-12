using Editor.AssetManagement;
using Editor.Windows.ImGuiWrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.Homepage
{
    /// <summary>
    /// Homepage for making new projects, managing recent projects
    /// </summary>
    public class Homepage
    {
        public bool Active = true;
        public EButton OpenProjectButton;
        public EButton NewProjectButton;
        public EButton Help;
        public EDropdown<EButton> RecentProjects;
        public List<EWidget> MainContainer = new List<EWidget>();

        /// <summary>
        /// Initialize homepage buttons
        /// </summary>
        public Homepage()
        {
            OpenProjectButton = new EButton();
            NewProjectButton = new EButton();
            Help = new EButton();
            RecentProjects = new EDropdown<EButton>();

            MainContainer.Add(OpenProjectButton);
            MainContainer.Add(NewProjectButton);
            MainContainer.Add(Help);
            MainContainer.Add(RecentProjects);
        }

        /// <summary>
        /// If we are connected to internet
        /// </summary>
        public bool ConnectedToInternet = false;

        /// <summary>
        /// If we need an update (runtime or IDE is out of date)
        /// </summary>
        public bool NeedsUpdate = false;

        /// <summary>
        /// Renders the homepage
        /// </summary>
        public void Render()
        {
            if (!Active)
                return;

            ImGui.Begin("Homepage");

            foreach (var widget in MainContainer)
            {
                widget.Draw();
            }

            ImGui.End();
        }
    }
}
