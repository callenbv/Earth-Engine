using Engine.Core.Game.Components;
using GameRuntime;

namespace Engine.Core.Game
{
    public class LightEmitter : GameScript
    {
        protected float lightRadius = 0f;
        protected float lightIntensity = 0f;
        protected string lightColor = "Orange";

        public void SetLight(float radius, float intensity, string color)
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
