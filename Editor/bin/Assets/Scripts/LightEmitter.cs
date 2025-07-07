using Engine.Core;
using Microsoft.Xna.Framework;
using System;

public class LightEmitter : GameScript
{
    private bool isPulsing = false;
    
    public override void Create()
    {
        SetLight(160f, 1f, "Orange");
    }
    
    public override void Update(GameTime gameTime)
    {
        float dt = ((float)gameTime.TotalGameTime.TotalSeconds*15f)/25f;
        float intensity = 0.9f + MathF.Cos(dt*13f)/55f;
        SetLightIntensity(intensity);
    }
} 