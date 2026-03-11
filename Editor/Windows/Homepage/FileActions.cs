using EarthEngineEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.Homepage
{
    public static class FileActions
    {
        /// <summary>
        /// Select a project to open
        /// </summary>
        public static void SelectProject()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Earth Engine Project (*.earthproj)|*.earthproj|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var projectFile = openFileDialog.FileName;
                    if (File.Exists(projectFile))
                    {
                        try
                        {
                            EditorApp.Instance._windowManager.OpenProject(projectFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Error opening project:\n{ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            Console.WriteLine($"Error opening project: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Project file not found: {projectFile}");
                    }
                }
            }

            EditorApp.Instance.homePage.Active = false;
        }
    }
}
