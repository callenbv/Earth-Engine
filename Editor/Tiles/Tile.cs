using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Text.Json.Serialization;

namespace Editor.Tiles
{
    public class Tile(int index, int tileset = 0, int z = 0, bool collide = false)
    {
        public int Index { get; set; } = index;
        public int Tileset { get; set; } = tileset;
        public int Z { get; set; } = z;
        public bool Collide { get; set; } = collide;
    }

    public class TileLayer
    {
        public string Name { get; set; }
        public Tile[,] Tiles { get; set; }
        public bool Visible { get; set; } = true;
        public bool Locked { get; set; } = false;
        public int TilesetId { get; set; } 

        public TileLayer(int width, int height, string name)
        {
            Name = name;
            Tiles = new Tile[width, height];
            TilesetId = -1; // No tileset assigned initially
        }
        public TileLayer(int w, int h, string name, int tilesetId)
        {
            Name = name;
            TilesetId = tilesetId;
            Tiles = new Tile[w, h];
        }
    }

    public class Tileset
    {
        public int Id { get; }
        public string FilePath { get; }
        public string Name { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }

        [JsonIgnore]
        public BitmapImage Atlas { get; }

        public int Columns => Atlas.PixelWidth / TileWidth;
        public int Rows => Atlas.PixelHeight / TileHeight;

        public Tileset(int id, string path, int w, int h)
        {
            Id = id;
            FilePath = path;
            Name = Path.GetFileNameWithoutExtension(path);
            TileWidth = w;
            TileHeight = h;
            
            // Load image with optimized settings
            Atlas = new BitmapImage();
            Atlas.BeginInit();
            Atlas.CacheOption = BitmapCacheOption.OnLoad;
            Atlas.UriSource = new Uri(path);
            Atlas.EndInit();
            Atlas.Freeze(); // Freeze for better performance
        }
    }
}
