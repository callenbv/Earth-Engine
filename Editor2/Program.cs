/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Program.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

namespace EarthEngineEditor
{
    class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            using var game = new EditorApp();
            game.Run();
        }
    }
} 
