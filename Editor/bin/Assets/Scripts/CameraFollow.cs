using Engine.Core;
using Microsoft.Xna.Framework;
using System;

public class CameraFollow : GameScript
{
    int targetZoom = 4;

    public override void Create()
    {
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Camera.Zoom = targetZoom;
        Camera.Target = Owner;
    }
}
