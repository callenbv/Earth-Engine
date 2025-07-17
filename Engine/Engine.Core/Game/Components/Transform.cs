using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game
{
    public class Transform : ObjectComponent
    {
        public Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public override string Name => "Transform";

        /// <summary>
        /// Object should follow the transform
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            Owner.position = Position;
            Owner.scale = Scale;
            Owner.rotation = Rotation;
        }
    }
}
