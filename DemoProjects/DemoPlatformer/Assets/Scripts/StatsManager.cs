using Engine.Core;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class StatsManager : GameScript
{
    public GameObject Player;
    public int TimeUntilFade = 5;

    private bool Fade = false;
    private float timer = 0;

    public override void Create() 
    { 
        
    }

    public override void Update(GameTime gameTime) 
    {

    }

    public override void Draw(SpriteBatch spriteBatch) 
    {
    }

    public override void Destroy() 
    {

    }
}

