using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Engine.Core.Rooms.Tiles
{
    public static class TileArrayUtils
    {
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
        public int[][] TileIndices { get; set; } // Tile indices in the tileset
        public bool[][] Collision { get; set; } // Collision flags for each tile
        public int[][] HeightMap { get; set; } // Height values for each tile
    }

    public class TilemapSaveData
    {
        public List<TilemapLayerData> Layers { get; set; } = new();
    }

    public static class TilemapRendererExtensions
    {
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
                        continue;

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
