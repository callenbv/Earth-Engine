using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game.Components
{
    public class SpriteData
    {
        public Texture2D? texture;
        public string Name { get; set; } = string.Empty; // Name of the sprite, can be used for identification
        public int frameWidth { get; set; } = 0; // 0 means use full image
        public int frameHeight { get; set; } = 0; // 0 means use full image
        public int frameCount { get; set; } = 1;
        public int frameSpeed { get; set; } = 1;
        public bool animated { get; set; } = false;
    }
}
