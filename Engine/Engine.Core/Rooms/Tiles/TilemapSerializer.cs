/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TilemapSerializer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Handles serialization and deserialization of tilemap data                
/// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Handles the serialization and deserialization of tilemap tile data.
    /// This class provides methods to convert between the 2D Tile array and a serializable format.
    /// </summary>
    public static class TilemapSerializer
    {
        /// <summary>
        /// Convert a 2D tile array to a serializable list of TileData objects.
        /// Only non-null tiles are included in the result.
        /// </summary>
        /// <param name="tiles">The 2D tile array to convert</param>
        /// <returns>A list of TileData objects representing the non-null tiles</returns>
        public static List<TileData> ToSerializable(Tile[,] tiles)
        {
            var tileDataList = new List<TileData>();
            
            if (tiles == null)
                return tileDataList;

            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile != null)
                    {
                        tileDataList.Add(new TileData(tile, x, y));
                    }
                }
            }

            return tileDataList;
        }

        /// <summary>
        /// Convert a list of TileData objects back to a 2D tile array.
        /// </summary>
        /// <param name="tileDataList">The list of TileData objects to convert</param>
        /// <param name="width">Width of the tilemap</param>
        /// <param name="height">Height of the tilemap</param>
        /// <returns>A 2D tile array with tiles placed at their correct positions</returns>
        public static Tile[,] FromSerializable(List<TileData> tileDataList, int width, int height)
        {
            var tiles = new Tile[width, height];

            if (tileDataList != null)
            {
                foreach (var tileData in tileDataList)
                {
                    if (tileData.X >= 0 && tileData.X < width && tileData.Y >= 0 && tileData.Y < height)
                    {
                        tiles[tileData.X, tileData.Y] = tileData.ToTile();
                    }
                }
            }

            return tiles;
        }
    }
}
