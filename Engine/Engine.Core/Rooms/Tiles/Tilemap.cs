using Engine.Core.Data;
using Engine.Core.Data.Graphics;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Holds the grid of tile data, manages adding, removing, and other operations
    /// </summary>
    [ComponentCategory("Tiles")]
    public class Tilemap : ObjectComponent
    {
        public override string Name => "Tilemap";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// Name of the tilemap to be displayed in the tile editor
        /// </summary>
        public string DisplayName = "Tilemap";

        /// <summary>
        /// The grid of tiles in the world (not serialized directly)
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public Tile[,]? Tiles { get; set; } = new Tile[,] { };

        /// <summary>
        /// Serializable representation of the tiles for JSON serialization
        /// </summary>
        [JsonPropertyName("Tiles")]
        [HideInInspector]
        public List<TileData> SerializableTiles
        {
            get => TilemapSerializer.ToSerializable(Tiles);
            set => Tiles = TilemapSerializer.FromSerializable(value, MapWidth, MapHeight);
        }

        /// <summary>
        /// List of brushes we can select on this tilemap
        /// </summary>
        [HideInInspector]
        [JsonIgnore]
        public List<RuleTile> Brushes = new List<RuleTile>();

        /// <summary>
        /// The actual renderer for the tilemap. Used to issue rendering calls
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public TilemapRenderer Renderer { get; set; }

        /// <summary>
        /// The tile to place (e.g, rule tile asset)
        /// </summary>
        [HideInInspector]
        public RuleTile Tile { get; set; }

        /// <summary>
        /// By default, our tilemap is 100x100 tiles (we allocate this much)
        /// </summary>
        public int MapSize { get; set; } = 100;

        /// <summary>
        /// Size of cell of the grid
        /// </summary>
        public int CellSize { get; set; } = 16;

        /// <summary>
        /// Tilemap width
        /// </summary>
        public int MapWidth { get; set; } = 100;

        /// <summary>
        /// Map height
        /// </summary>
        public int MapHeight { get; set; } = 100;

        /// <summary>
        /// The order at which tilemaps are rendered (0-255)
        /// </summary>
        public int SortingOrder = 0;

        /// <summary>
        /// Initialize the tilemap grid
        /// </summary>
        public Tilemap()
        {
            Renderer = new TilemapRenderer(this);
            Resize(MapSize);
        }

        /// <summary>
        /// Resize the tilemap
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(int width, int height)
        {
            Tiles = new Tile[width,height];
        }

        /// <summary>
        /// Overload for square (nxn) resize
        /// </summary>
        /// <param name="mapSize"></param>
        public void Resize(int mapSize)
        {
            Resize(mapSize, mapSize);
        }


        /// <summary>
        /// Place a tile in the grid at the given position
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetTile(Tile? tile, int x, int y)
        {
            // Out of bounds
            if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight)
                return;

            Tiles[x,y] = tile;
            
            // Trigger autotiling if this is a rule tile
            UpdateAutotiling(x, y);
        }

        /// <summary>
        /// Place a tile in the grid given a vector position
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="position"></param>
        public void SetTile(Tile? tile, Vector2 position)
        {
            // Set the tile
            SetTile(tile, (int)position.X, (int)position.Y);
        }

        /// <summary>
        /// Get a tile at the specified position, returns null if out of bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>The tile at the position, or null if out of bounds</returns>
        public Tile? GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight)
                return null;
            
            return Tiles[x, y];
        }

        /// <summary>
        /// Get a tile at the specified position, returns null if out of bounds
        /// </summary>
        /// <param name="position">Position as Vector2</param>
        /// <returns>The tile at the position, or null if out of bounds</returns>
        public Tile? GetTile(Vector2 position)
        {
            return GetTile((int)position.X, (int)position.Y);
        }

        /// <summary>
        /// Check if a tile exists at the specified position (not null and within bounds)
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if a tile exists at the position</returns>
        public bool HasTile(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile != null;
        }

        /// <summary>
        /// Check if a tile exists at the specified position (not null and within bounds)
        /// </summary>
        /// <param name="position">Position as Vector2</param>
        /// <returns>True if a tile exists at the position</returns>
        public bool HasTile(Vector2 position)
        {
            return HasTile((int)position.X, (int)position.Y);
        }

        /// <summary>
        /// Get all neighboring tiles (8-directional) for autotiling
        /// </summary>
        /// <param name="x">X coordinate of the center tile</param>
        /// <param name="y">Y coordinate of the center tile</param>
        /// <returns>Dictionary of directions to neighboring tiles</returns>
        public Dictionary<TileDirection, Tile?> GetNeighboringTiles(int x, int y)
        {
            var neighbors = new Dictionary<TileDirection, Tile?>();
            
            neighbors[TileDirection.Top] = GetTile(x, y - 1);
            neighbors[TileDirection.Bottom] = GetTile(x, y + 1);
            neighbors[TileDirection.Left] = GetTile(x - 1, y);
            neighbors[TileDirection.Right] = GetTile(x + 1, y);
            
            return neighbors;
        }

        /// <summary>
        /// Update autotiling for a specific tile and its neighbors
        /// </summary>
        /// <param name="x">X coordinate of the tile to update</param>
        /// <param name="y">Y coordinate of the tile to update</param>
        public void UpdateAutotiling(int x, int y)
        {
            // Update the tile itself
            var tile = GetTile(x, y);
            if (tile is RuleTile ruleTile)
            {
                ruleTile.AutoTile(this, x, y);
            }

            // Update neighboring tiles that might be affected
            var neighbors = GetNeighboringTiles(x, y);
            foreach (var kvp in neighbors)
            {
                var neighborTile = kvp.Value;
                if (neighborTile is RuleTile neighborRuleTile)
                {
                    var neighborPos = GetNeighborPosition(x, y, kvp.Key);
                    neighborRuleTile.AutoTile(this, (int)neighborPos.X, (int)neighborPos.Y);
                }
            }
        }

        /// <summary>
        /// Get the position of a neighbor in a specific direction
        /// </summary>
        /// <param name="x">X coordinate of the center tile</param>
        /// <param name="y">Y coordinate of the center tile</param>
        /// <param name="direction">Direction to get neighbor position for</param>
        /// <returns>Position of the neighbor</returns>
        private Vector2 GetNeighborPosition(int x, int y, TileDirection direction)
        {
            return direction switch
            {
                TileDirection.Top => new Vector2(x, y - 1),
                TileDirection.Bottom => new Vector2(x, y + 1),
                TileDirection.Left => new Vector2(x - 1, y),
                TileDirection.Right => new Vector2(x + 1, y),
                _ => new Vector2(x, y)
            };
        }

        /// <summary>
        /// Issue rendering calls via the tilemap renderer
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            Renderer.Draw(spriteBatch);
        }

        /// <summary>
        /// Initialize the renderer's texture
        /// </summary>
        public override void Initialize()
        {
            Renderer.texture?.Initialize();
        }
    }
}
