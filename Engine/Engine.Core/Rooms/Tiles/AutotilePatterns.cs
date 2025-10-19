/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AutotilePatterns.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Different autotile patterns and their implementations                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Different types of autotile patterns
    /// </summary>
    public enum AutotilePattern
    {
        /// <summary>
        /// 3x3 pattern with 9 tiles (corners, edges, center)
        /// </summary>
        ThreeByThree,
        
        /// <summary>
        /// 2x2 pattern with 4 tiles (corners only)
        /// </summary>
        TwoByTwo,
        
        /// <summary>
        /// 4x4 pattern with 16 tiles (more detailed corners and edges)
        /// </summary>
        FourByFour,
        
        /// <summary>
        /// 2x3 pattern with 6 tiles (horizontal edges)
        /// </summary>
        TwoByThree,
        
        /// <summary>
        /// 3x2 pattern with 6 tiles (vertical edges)
        /// </summary>
        ThreeByTwo
    }

    /// <summary>
    /// Static class containing autotile pattern implementations
    /// </summary>
    public static class AutotilePatterns
    {
        /// <summary>
        /// Calculate the frame for a 3x3 autotile pattern
        /// </summary>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateThreeByThree(Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            int autotileIndex = 0;
            
            // Calculate index based on neighboring tiles
            if (directions[TileDirection.Up] && directions[TileDirection.Left])
                autotileIndex += 1; // Top-left corner
            if (directions[TileDirection.Up])
                autotileIndex += 2; // Top edge
            if (directions[TileDirection.Up] && directions[TileDirection.Right])
                autotileIndex += 4; // Top-right corner
                
            if (directions[TileDirection.Left])
                autotileIndex += 8; // Left edge
            autotileIndex += 16; // Center (always present)
            if (directions[TileDirection.Right])
                autotileIndex += 32; // Right edge
                
            if (directions[TileDirection.Down] && directions[TileDirection.Left])
                autotileIndex += 64; // Bottom-left corner
            if (directions[TileDirection.Down])
                autotileIndex += 128; // Bottom edge
            if (directions[TileDirection.Down] && directions[TileDirection.Right])
                autotileIndex += 256; // Bottom-right corner

            // Convert to frame coordinates (3x3 grid)
            int tilesPerRow = 3;
            int frameX = baseFrame.X + (autotileIndex % tilesPerRow) * baseFrame.Width;
            int frameY = baseFrame.Y + (autotileIndex / tilesPerRow) * baseFrame.Height;
            
            return new Rectangle(frameX, frameY, baseFrame.Width, baseFrame.Height);
        }

        /// <summary>
        /// Calculate the frame for a 2x2 autotile pattern (corners only)
        /// </summary>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateTwoByTwo(Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            int autotileIndex = 0;
            
            // Only check corners for 2x2 pattern
            if (directions[TileDirection.Up] && directions[TileDirection.Left])
                autotileIndex = 0; // Top-left
            else if (directions[TileDirection.Up] && directions[TileDirection.Right])
                autotileIndex = 1; // Top-right
            else if (directions[TileDirection.Down] && directions[TileDirection.Left])
                autotileIndex = 2; // Bottom-left
            else if (directions[TileDirection.Down] && directions[TileDirection.Right])
                autotileIndex = 3; // Bottom-right
            else
                autotileIndex = 0; // Default to top-left

            // Convert to frame coordinates (2x2 grid)
            int tilesPerRow = 2;
            int frameX = baseFrame.X + (autotileIndex % tilesPerRow) * baseFrame.Width;
            int frameY = baseFrame.Y + (autotileIndex / tilesPerRow) * baseFrame.Height;
            
            return new Rectangle(frameX, frameY, baseFrame.Width, baseFrame.Height);
        }

        /// <summary>
        /// Calculate the frame for a 4x4 autotile pattern (more detailed)
        /// </summary>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateFourByFour(Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            // More complex 4x4 pattern with 16 possible combinations
            int autotileIndex = 0;
            
            // Calculate based on all 4 directions
            if (directions[TileDirection.Up]) autotileIndex += 1;
            if (directions[TileDirection.Right]) autotileIndex += 2;
            if (directions[TileDirection.Down]) autotileIndex += 4;
            if (directions[TileDirection.Left]) autotileIndex += 8;

            // Convert to frame coordinates (4x4 grid)
            int tilesPerRow = 4;
            int frameX = baseFrame.X + (autotileIndex % tilesPerRow) * baseFrame.Width;
            int frameY = baseFrame.Y + (autotileIndex / tilesPerRow) * baseFrame.Height;
            
            return new Rectangle(frameX, frameY, baseFrame.Width, baseFrame.Height);
        }

        /// <summary>
        /// Calculate the frame for a 2x3 autotile pattern (horizontal edges)
        /// </summary>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateTwoByThree(Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            int autotileIndex = 0;
            
            // Focus on horizontal connections
            if (directions[TileDirection.Left] && directions[TileDirection.Right])
                autotileIndex = 0; // Both sides
            else if (directions[TileDirection.Left])
                autotileIndex = 1; // Left only
            else if (directions[TileDirection.Right])
                autotileIndex = 2; // Right only
            else
                autotileIndex = 3; // Neither side

            // Convert to frame coordinates (2x3 grid)
            int tilesPerRow = 2;
            int frameX = baseFrame.X + (autotileIndex % tilesPerRow) * baseFrame.Width;
            int frameY = baseFrame.Y + (autotileIndex / tilesPerRow) * baseFrame.Height;
            
            return new Rectangle(frameX, frameY, baseFrame.Width, baseFrame.Height);
        }

        /// <summary>
        /// Calculate the frame for a 3x2 autotile pattern (vertical edges)
        /// </summary>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateThreeByTwo(Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            int autotileIndex = 0;
            
            // Focus on vertical connections
            if (directions[TileDirection.Up] && directions[TileDirection.Down])
                autotileIndex = 0; // Both top and bottom
            else if (directions[TileDirection.Up])
                autotileIndex = 1; // Top only
            else if (directions[TileDirection.Down])
                autotileIndex = 2; // Bottom only
            else
                autotileIndex = 3; // Neither top nor bottom

            // Convert to frame coordinates (3x2 grid)
            int tilesPerRow = 3;
            int frameX = baseFrame.X + (autotileIndex % tilesPerRow) * baseFrame.Width;
            int frameY = baseFrame.Y + (autotileIndex / tilesPerRow) * baseFrame.Height;
            
            return new Rectangle(frameX, frameY, baseFrame.Width, baseFrame.Height);
        }

        /// <summary>
        /// Calculate the frame for any autotile pattern
        /// </summary>
        /// <param name="pattern">The autotile pattern to use</param>
        /// <param name="directions">Dictionary of tile directions</param>
        /// <param name="baseFrame">Base frame rectangle</param>
        /// <returns>Updated frame rectangle</returns>
        public static Rectangle CalculateFrame(AutotilePattern pattern, Dictionary<TileDirection, bool> directions, Rectangle baseFrame)
        {
            return pattern switch
            {
                AutotilePattern.ThreeByThree => CalculateThreeByThree(directions, baseFrame),
                AutotilePattern.TwoByTwo => CalculateTwoByTwo(directions, baseFrame),
                AutotilePattern.FourByFour => CalculateFourByFour(directions, baseFrame),
                AutotilePattern.TwoByThree => CalculateTwoByThree(directions, baseFrame),
                AutotilePattern.ThreeByTwo => CalculateThreeByTwo(directions, baseFrame),
                _ => baseFrame
            };
        }
    }
}
