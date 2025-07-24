/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Random.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.CustomMath
{
    public static class ERandom
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Returns a float between 0.0 (inclusive) and 1.0 (exclusive).
        /// </summary>
        public static float NextFloat()
        {
            return (float)_rng.NextDouble();
        }

        /// <summary>
        /// Returns a float between min (inclusive) and max (exclusive).
        /// </summary>
        public static float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>
        /// Returns an int in the range [min, max).
        /// </summary>
        public static int Range(int min, int max)
        {
            return _rng.Next(min, max);
        }

        /// <summary>
        /// Returns true with the given probability (0.0 to 1.0).
        /// </summary>
        public static bool Chance(float probability)
        {
            return NextFloat() < probability;
        }
    }
}

