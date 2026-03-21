using Engine.Core;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

public class Health : GameScript
{
    public int MaxHealth = 100;
    public int CurrentHealth = 100;
    public Sprite2D HealthbarSprite;

    public override void Create() 
    {
    }

    public override void Update(GameTime gameTime) 
    {
        CurrentHealth -= 1;
        CurrentHealth = Math.Clamp(CurrentHealth, 0, MaxHealth);

        float percent = (float)CurrentHealth / MaxHealth;
        HealthbarSprite.SpriteScale.X = percent-1;
    }

    public override void Draw(SpriteBatch spriteBatch) 
    { 
        
    }

    public override void Destroy() 
    {

    }
}

