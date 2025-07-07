using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game
{
    public class GameOptions
    {
        public string title { get; set; } = "My Game";
        public int windowWidth { get; set; } = 800;
        public int windowHeight { get; set; } = 600;
        public string defaultRoom { get; set; } = "";
    }
}
