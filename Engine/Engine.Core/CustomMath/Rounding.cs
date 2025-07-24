/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Rounding.cs
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
    public class Rounding
    {
        public static float RoundToNearest(float value, float multiple)
        {
            return (float)(Math.Round(value / multiple) * multiple);
        }
    }
}

