using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game.Rooms
{
    public class Room
    {
        public string background { get; set; } = "";
        public bool backgroundTiled { get; set; } = false;
        public int width { get; set; } = 800;
        public int height { get; set; } = 600;
        public List<RoomObject> objects { get; set; } = new List<RoomObject>();
    }

    public class RoomObject
    {
        public string? objectName { get; set; }
        public string? objectPath { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
}
