using Engine.Core;
using Microsoft.Xna.Framework;
using System;

public class Item : GameScript
{
    protected string name {  get; set; }
    protected string description { get; set; }

    public override void Update(GameTime gameTime)
    {
        Owner.scale += MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds*7)/100f;
    }
}