using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    public class EarthObject
    {
        public double x { get; set; }
        public double y { get; set; }
        public string? name { get; set; }
        public string? objectPath { get; set; }
        public string? sprite { get; set; }
        public List<string> scripts { get; set; } = new List<string>();
        public Dictionary<string, Dictionary<string, object>> scriptProperties { get; set; } = new();
    }
}
