using Engine.Core;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Key : GameScript
{
    public ParticleEmitter Emitter;

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
        //StatsManager.Instance.Keys += 1;
    }

    public override void OnCollisionTrigger(Collider2D other)
    {
        Owner.Destroy();
        InventoryManager.Instance.Keys += 1;
        //Emitter.EmitParticle();
    }
}

