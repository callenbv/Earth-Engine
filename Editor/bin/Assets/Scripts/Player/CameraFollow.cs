using Engine.Core;
using Microsoft.Xna.Framework;
using System;

public class CameraFollow : GameScript
{
    float targetZoom = 4f;

    public override void Create()
    {
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Camera.Zoom = targetZoom+MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds)/55f;
        Camera.Target = Owner;
    }
}
