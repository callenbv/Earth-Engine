using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class PlayerMovement : GameScript
{
    private float Speed = 1.25f;

    public override void Create()
    {

    }

    public override void Update(GameTime gameTime)
    {
        if (Input.IsKeyDown(Keys.W))
        {
            Owner.position.Y -= Speed;
        }
        if (Input.IsKeyDown(Keys.A))
        {
            Owner.position.X -= Speed;
            Owner.sprite.spriteEffect = SpriteEffects.FlipHorizontally;
        }
        if (Input.IsKeyDown(Keys.S))
        {
            Owner.position.Y += Speed;
        }
        if (Input.IsKeyDown(Keys.D))
        {
            Owner.position.X += Speed;
            Owner.sprite.spriteEffect = SpriteEffects.None;
        }
    }
}