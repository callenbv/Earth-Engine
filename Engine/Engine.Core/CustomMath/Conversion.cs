using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.CustomMath
{
    public static class VectorConversion
    {
        public static System.Numerics.Vector2 ToNumerics(this Microsoft.Xna.Framework.Vector2 v) =>
            new System.Numerics.Vector2(v.X, v.Y);

        public static Microsoft.Xna.Framework.Vector2 ToXna(this System.Numerics.Vector2 v) =>
            new Microsoft.Xna.Framework.Vector2(v.X, v.Y);
    }
}
