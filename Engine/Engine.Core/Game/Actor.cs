using Engine.Core.Game.Components;
using GameRuntime;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game
{
    public class Actor : GameObject
    {
        public SpriteData? spriteData;
        public int currentFrame = 0;
        public double frameTimer = 0;
        public string? spriteName;

        // Lighting properties
        public bool emitsLight = false;
        public float lightRadius = 100f;
        public float lightIntensity = 1f;
        public string lightColor = "White";

        public void SetSprite(string newSpriteName)
        {
            spriteName = newSpriteName;
            currentFrame = 0;
            frameTimer = 0;
        }

        public LightSource CreateLightSource()
        {
            if (!emitsLight) return null;

            // Parse color string to Color
            Color color = Color.White;
            try
            {
                var colorProperty = typeof(Color).GetProperty(lightColor);
                if (colorProperty != null)
                {
                    color = (Color)colorProperty.GetValue(null);
                }
            }
            catch
            {
                // Default to white if color parsing fails
                color = Color.White;
            }

            return new LightSource
            {
                Position = position,
                Radius = lightRadius,
                Color = color,
                Intensity = lightIntensity
            };
        }
    }

}
