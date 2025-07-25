/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TileData.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using System.Numerics;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Utility class for converting between jagged arrays and 2D arrays for tile data.
    /// </summary>
    public static class TileArrayUtils
    {
        /// <summary>
        /// Converts a 2D array to a jagged array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[][] ToJagged<T>(T[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);

            var jagged = new T[width][];
            for (int x = 0; x < width; x++)
            {
                jagged[x] = new T[height];
                for (int y = 0; y < height; y++)
                    jagged[x][y] = array[x, y];
            }
            return jagged;
        }

        /// <summary>
        /// Converts a jagged array to a 2D array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jagged"></param>
        /// <returns></returns>
        public static T[,] To2D<T>(T[][] jagged)
        {
            int width = jagged.Length;
            int height = jagged[0].Length;

            var array = new T[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    array[x, y] = jagged[x][y];

            return array;
        }
    }

    /// <summary>
    /// Serializable data for a single tilemap layer.
    /// </summary>
    public class TilemapLayerData
    {
        public string Title { get; set; }
        public string TexturePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Depth { get; set; }
        public Vector2 Offset { get; set; }
        public int[][] TileIndices { get; set; } 
        public bool[][] Collision { get; set; }
        public int[][] HeightMap { get; set; }
    }

    /// <summary>
    /// Serializable data for a tilemap, containing multiple layers.
    /// </summary>
    public class TilemapSaveData
    {
        public List<TilemapLayerData> Layers { get; set; } = new();
    }

    /// <summary>
    /// Extensions for TilemapRenderer to convert to and from TilemapLayerData.
    /// </summary>
    public static class TilemapRendererExtensions
    {
        /// <summary>
        /// Converts the TilemapRenderer to TilemapLayerData, extracting its properties and tiles.
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns></returns>
        public static TilemapLayerData ToData(this TilemapRenderer renderer)
        {
            int w = renderer.Width;
            int h = renderer.Height;

            var indices = new int[w, h];
            var solid = new bool[w, h];
            var height = new int[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var tile = renderer.Tiles[x, y];

                    if (tile == null)
                    {
                        indices[x, y] = -1;
                        continue;
                    }

                    indices[x, y] = tile.TileIndex;
                    solid[x, y] = tile.IsCollidable;
                    height[x, y] = tile.Height;
                }
            }

            return new TilemapLayerData
            {
                Title = renderer.Title,
                TexturePath = renderer.TexturePath,
                Width = w,
                Height = h,
                Depth = renderer.Depth,
                TileIndices = TileArrayUtils.ToJagged(indices),
                Collision = TileArrayUtils.ToJagged(solid),
                HeightMap = TileArrayUtils.ToJagged(height),
                Offset = renderer.Offset
            };
        }

        /// <summary>
        /// Applies the given TilemapLayerData to the TilemapRenderer, updating its properties and tiles.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="data"></param>
        public static void ApplyData(this TilemapRenderer renderer, TilemapLayerData data)
        {
            renderer.Title = data.Title;
            renderer.TexturePath = data.TexturePath;
            renderer.Texture = TextureLibrary.Instance.Get(renderer.TexturePath);
            renderer.Width = data.Width;
            renderer.Height = data.Height;
            renderer.Offset = data.Offset;
            renderer.Depth = data.Depth;

            int[,] indices = TileArrayUtils.To2D(data.TileIndices);
            bool[,] collision = TileArrayUtils.To2D(data.Collision);
            int[,] height = TileArrayUtils.To2D(data.HeightMap);

            renderer.Tiles = new Tile[renderer.Width, renderer.Height];
            for (int x = 0; x < renderer.Width; x++)
            {
                for (int y = 0; y < renderer.Height; y++)
                {
                    if (indices[x, y] < 0)
                    {
                        continue;
                    }

                    renderer.Tiles[x, y] = new Tile
                    {
                        TileIndex = indices[x, y],
                        IsCollidable = collision[x, y],
                        Height = height[x, y]
                    };
                }
            }
        }
    }
}

