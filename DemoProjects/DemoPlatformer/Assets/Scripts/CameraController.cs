using Engine.Core;
using Engine.Core.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class CameraController : GameScript
{
    [SliderEditor(0,1)]
    public float cameraSmoothing = 0.2f;
    public override void Create() 
    { 
        
    }

    public override void Update(GameTime gameTime) 
    {
        Camera.Main.Target = Owner;
        float targetX = MathHelper.Lerp(Camera.Main.Position.X, Owner.Position.X, cameraSmoothing * dt);
        float targetY = MathHelper.Lerp(Camera.Main.Position.Y, Owner.Position.Y, cameraSmoothing * dt);

        Camera.Main.Position = new Vector2(targetX, targetY);
    }

    public override void Draw(SpriteBatch spriteBatch) 
    { 
        
    }

    public override void Destroy() 
    {

    }
}

