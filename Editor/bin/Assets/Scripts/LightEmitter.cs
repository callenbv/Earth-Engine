using Engine.Core;
using Microsoft.Xna.Framework;
using System;

public class LightEmitter : GameScript
{
    private bool isPulsing = false;
    
    public override void Create()
    {
        // Enable light emission with default settings
        SetLight(true, 160f, 1f, "Orange");
    }
    
    public override void Update(GameTime gameTime)
    {
        float dt = ((float)gameTime.TotalGameTime.TotalSeconds*10f)/25f;
        float intensity = 0.9f + MathF.Cos(dt*10f)/25f;
        SetLightIntensity(intensity);
    }
    
    public override void OnClick()
    {
        // Toggle pulsing effect when clicked
        isPulsing = !isPulsing;
        if (!isPulsing)
        {
            SetLightIntensity(1f); // Reset to normal intensity
        }
    }
} 