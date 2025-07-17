using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game
{
    public class PointLight : ObjectComponent
    {
        public float lightRadius = 64f;
        public float lightIntensity = 1f;
        public Color lightColor = Color.White;
        private Texture2D? softCircleTexture;
        public override string Name => "Point Light";
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

        private void EnsureSoftCircleTexture(int diameter)
        {
            if (softCircleTexture != null && !softCircleTexture.IsDisposed && softCircleTexture.Width == diameter)
                return;

            softCircleTexture?.Dispose();
            softCircleTexture = new Texture2D(GraphicsDevice, diameter, diameter);

            Color[] data = new Color[diameter * diameter];
            float r = diameter / 2f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = (float)System.Math.Sqrt(dx * dx + dy * dy) / r;
                    float alpha = 1f - MathHelper.Clamp(dist, 0f, 1f);
                    data[y * diameter + x] = new Color(1f, 1f, 1f, alpha * alpha); // Soft falloff
                }
            }
            softCircleTexture.SetData(data);
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            int diameter = Math.Max(1, (int)(lightRadius * 2));
            EnsureSoftCircleTexture(diameter);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, null, null, null, Camera.Main.GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight));
            spriteBatch.Draw(
                softCircleTexture,
                Owner.position,
                null,
                lightColor * lightIntensity,
                0f,
                new Vector2(diameter / 2f, diameter / 2f),
                1f,
                SpriteEffects.None,
                0f);
            spriteBatch.End();

        }
    }
}
