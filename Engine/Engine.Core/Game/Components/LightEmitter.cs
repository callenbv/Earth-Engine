using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;

namespace Engine.Core.Game
{
    public class PointLight : GameScript
    {
        public float lightRadius = 0f;
        public float lightIntensity = 0f;
        public Color lightColor = Color.White;

        public void SetLight(float radius, float intensity, Color color)
        {
            lightRadius = radius;
            lightIntensity = intensity;
            lightColor = color;
        }

        public void SetLightIntensity(float intensity)
        {
            lightIntensity = intensity;
        }
    }
}
