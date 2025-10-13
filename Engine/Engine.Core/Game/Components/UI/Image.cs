/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Image.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Implements a UI image inheriting from Sprite2D     
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// A 2D sprite component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("UI")]
    public class Image : Sprite2D
    {
        public override string Name => "Image";
        public override bool UpdateInEditor => true;


        /// <summary>
        /// Draw the sprite if valid
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
        }

        /// <summary>
        /// Draw the sprite in UI coordinates
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void DrawUI(SpriteBatch spriteBatch)
        {
            if (texture == null)
                return;

            frame = Math.Clamp(frame, 0, frameCount - 1);
            depth = GetDepth();
            origin = new Vector2(frameWidth / 2, frameHeight / 2);
            spriteBatch.Draw(texture, new Vector2(Position.X, Position.Y) + new Vector2(Offset.X, Offset.Y), spriteBox, Tint, Rotation, origin, new Vector2(Scale.X, Scale.Y) + new Vector2(SpriteScale.X, SpriteScale.Y), spriteEffect, depth);
        }
    }
}

