/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Program.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;

namespace EarthEngineEditor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using var game = new EditorApp();
            game.Run();
        }
    }
} 
