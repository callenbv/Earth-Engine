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