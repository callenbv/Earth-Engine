using Engine.Core.Data;
using Engine.Core.Data.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core.Rooms.Tiles
{
    public enum TileDirection
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }

    /// <summary>
    /// Tri-state neighbor condition like Unity’s RuleTile:
    /// Any = ignore, This = must match, NotThis = must not match.
    /// </summary>
    public enum NeighborCondition
    {
        Any,
        This,
        NotThis
    }

    /// <summary>
    /// Rule class tile for autotiling
    /// </summary>
    public class RuleTile : Tile
    {
        [HideInInspector]
        public List<TileRule> Rules = new();

        [HideInInspector]
        public int DefaultFrameIndex { get; set; } = 0;

        public RuleTile()
        {
            if (Rules.Count == 0)
                Rules.Add(new TileRule());
        }

        /// <summary>
        /// Autotile the tile based on surrounding tiles
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AutoTile(Tilemap tilemap, int x, int y)
        {
            var neighbors = GetNeighborStates(tilemap, x, y);

            // Prioritize more specific rules (those with more non-Any conditions)
            foreach (var rule in Rules.OrderByDescending(r => r.Conditions.Count(c => c.Value != NeighborCondition.Any)))
            {
                if (MatchesRule(rule, neighbors))
                {
                    SetFrameFromIndex(tilemap, rule.SelectedFrameIndex);
                    return;
                }
            }

            // Fallback to default
            SetFrameFromIndex(tilemap, DefaultFrameIndex);
        }

        /// <summary>
        /// Get the state of neighbor tiles
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Dictionary<TileDirection, bool> GetNeighborStates(Tilemap tilemap, int x, int y)
        {
            var dirs = new Dictionary<TileDirection, bool>
            {
                [TileDirection.Top] = HasSameTileType(tilemap.GetTile(x, y - 1)),
                [TileDirection.Bottom] = HasSameTileType(tilemap.GetTile(x, y + 1)),
                [TileDirection.Left] = HasSameTileType(tilemap.GetTile(x - 1, y)),
                [TileDirection.Right] = HasSameTileType(tilemap.GetTile(x + 1, y)),
                [TileDirection.TopLeft] = HasSameTileType(tilemap.GetTile(x - 1, y - 1)),
                [TileDirection.TopRight] = HasSameTileType(tilemap.GetTile(x + 1, y - 1)),
                [TileDirection.BottomLeft] = HasSameTileType(tilemap.GetTile(x - 1, y + 1)),
                [TileDirection.BottomRight] = HasSameTileType(tilemap.GetTile(x + 1, y + 1)),
                [TileDirection.Center] = true
            };
            return dirs;
        }

        /// <summary>
        /// If this neighbor is the same type
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        private bool HasSameTileType(Tile? neighbor)
        {
            return neighbor is RuleTile rule && rule.TileIndex == TileIndex;
        }

        /// <summary>
        /// Checks whether this tile’s neighbors satisfy the given rule.
        /// </summary>
        private bool MatchesRule(TileRule rule, Dictionary<TileDirection, bool> neighbors)
        {
            // Skip empty rules
            if (!rule.Conditions.Values.Any(v => v != NeighborCondition.Any))
                return false;

            foreach (var kvp in rule.Conditions)
            {
                if (kvp.Key == TileDirection.Center)
                    continue;

                bool actual = neighbors.GetValueOrDefault(kvp.Key);
                var expected = kvp.Value;

                switch (expected)
                {
                    case NeighborCondition.Any:
                        continue;
                    case NeighborCondition.This:
                        if (!actual)
                            return false;
                        break;
                    case NeighborCondition.NotThis:
                        if (actual)
                            return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Set the frame of texture to use
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="index"></param>
        private void SetFrameFromIndex(Tilemap tilemap, int index)
        {
            int cell = CellSize;

            if (Texture.texture == null)
                return;

            int texWidth = Texture.texture.Width;
            int cols = texWidth / cell;

            int x = (index % cols) * cell;
            int y = (index / cols) * cell;

            Frame = new Rectangle(x, y, cell, cell);
        }
    }

    /// <summary>
    /// Data structure of the tile to use
    /// </summary>
    public class TileRule
    {
        public Dictionary<TileDirection, NeighborCondition> Conditions = new();
        public int SelectedFrameIndex { get; set; } = 0;

        public TileRule()
        {
            foreach (TileDirection dir in Enum.GetValues(typeof(TileDirection)))
                Conditions[dir] = NeighborCondition.Any;
        }
    }
}
