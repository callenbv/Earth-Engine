/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Sprite2D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// A 2D sprite component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Graphics")]
    public class Sprite2D : ObjectComponent
    {
        public override string Name => "Sprite 2D";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// The texture used for the sprite. If set, it will automatically update the texturePath, frameWidth, frameHeight, and spriteBox properties.
        /// </summary>
        [JsonIgnore]
        public Texture2D? texture
        {
            get => _texture;
            set
            {
                _texture = value;

                if (_texture != null && !string.IsNullOrEmpty(_texture.Name))
                {
                    texturePath = _texture.Name;
                    frameHeight = _texture.Height;
                    frameWidth = _texture.Width/frameCount;
                    animated = frameCount > 1;
                    spriteBox = new Rectangle(0, 0, frameWidth, frameHeight);
                }
            }
        }

        /// <summary>
        /// The position of the sprite in world coordinates. This is where the sprite will be drawn in the game world.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// The position of the sprite in world coordinates. This is where the sprite will be drawn in the game world.
        /// </summary>
        [SliderEditor(1f, 10f)]
        public Vector2 SpriteScale;

        /// <summary>
        /// Height in terms of layers, used for tilemap depth ordering
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// The position of the sprite in world coordinates. This is where the sprite will be drawn in the game world.
        /// </summary>
        private Vector2 position;

        /// <summary>
        /// The texture used for the sprite. If set, it will automatically update the texturePath, frameWidth, frameHeight, and spriteBox properties.
        /// </summary>
        private Texture2D? _texture;

        /// <summary>
        /// The path to the texture file used for the sprite. This is set automatically when the texture is assigned.
        /// </summary>
        [HideInInspector]
        public string texturePath { get; set; } = string.Empty;

        /// <summary>
        /// The width and height of each frame in the sprite sheet.
        /// </summary>
        public int frameWidth { get; set; } = 16;

        /// <summary>
        /// The height of each frame in the sprite sheet.
        /// </summary>
        public int frameHeight { get; set; } = 16;

        /// <summary>
        /// The number of frames in the sprite sheet. If greater than 1, the sprite is considered animated.
        /// </summary>
        public int frameCount { get; set; } = 1;

        /// <summary>
        /// The speed at which the frames are animated. Higher values result in faster animations.
        /// </summary>
        [SliderEditor(0f, 60f)]
        public int frameSpeed { get; set; } = 1;

        /// <summary>
        /// Indicates whether the sprite is animated. If true, the sprite will cycle through frames based on frameSpeed.
        /// </summary>
        public bool animated = false;

        /// <summary>
        /// The depth of the sprite in the scene. This is used for rendering order.
        /// </summary>
        [HideInInspector]
        [JsonIgnore]
        public float depth = 0;

        /// <summary>
        /// Indicates if this sprite will be drawn in UI coordinates
        /// </summary>
        public bool IsUI { get; set; } = false;

        /// <summary>
        /// The sprite effects applied to the sprite, such as flipping or mirroring.
        /// </summary>
        public SpriteEffects spriteEffect = SpriteEffects.None;

        /// <summary>
        /// The origin point of the sprite, used for rotation and scaling. This is typically the center of the sprite.
        /// </summary>
        public Vector2 origin;

        /// <summary>
        /// The scale of the sprite. This allows for resizing the sprite when drawn.
        /// </summary>
        public Color Tint = Color.White;

        /// <summary>
        /// The bounding box of the sprite, used for collision detection and rendering.
        /// </summary>
        [HideInInspector]
        public Rectangle spriteBox = new Rectangle();

        /// <summary>
        /// The current frame of the sprite. This is used to determine which part of the sprite sheet to draw.
        /// </summary>
        public int frame
        {
            get => frame_;
            set 
            {
                spriteBox.Width = frameWidth;
                spriteBox.X = frame * frameWidth;
                spriteBox.Y = 0;
                frame_ = value;
            }
        }

        private int frame_ = 0;
        private float frameTimer = 0;

        /// <summary>
        /// Initialize sprite with a texture if possible
        /// </summary>
        public override void Initialize()
        {
            Texture2D tex = TextureLibrary.Instance.Get(texturePath);

            if (tex != null)
            {
                texture = tex;
            }
        }

        /// <summary>
        /// Get the depth of the sprite based on its position
        /// </summary>
        /// <returns></returns>
        public float GetDepth()
        {
            if (texture == null)
                return 0f;

            float feetY = Position.Y + frameHeight / 2;

            // Incorporate height into depth sorting
            float depth = ((Owner.Height+Height) * 10000f + feetY) / 100000f; // Adjust divisor to fit your world

            return Math.Clamp(depth, 0f, 1f);
        }

        /// <summary>
        /// Swap the texture
        /// </summary>
        /// <param name="textureName"></param>
        public void Set(string textureName)
        {
            texture = TextureLibrary.Instance.Get(textureName);
        }

        /// <summary>
        /// Set a texture with custom frame size
        /// </summary>
        /// <param name="textureName"></param>
        /// <param name="frameWidth"></param>
        /// <param name="frameHeight"></param>
        public void Set(string textureName, int frameWidth, int frameHeight)
        {
            if (texturePath != textureName)
            {
                texture = TextureLibrary.Instance.Get(textureName);
                this.frameWidth = frameWidth;
                this.frameHeight = frameHeight;
                this.frameCount = texture.Width / frameWidth;
                animated = frameCount > 1;
                spriteBox = new Rectangle(0, 0, frameWidth, frameHeight);
            }
        }

        /// <summary>
        /// Draw the sprite if valid
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (texture == null || IsUI)
                return;

            frame = Math.Clamp(frame, 0, frameCount - 1);
            depth = GetDepth();
            origin = new Vector2(frameWidth / 2, frameHeight / 2);
            spriteBatch.Draw(texture, Position+ Offset, spriteBox, Tint, Rotation, origin, Scale+ SpriteScale, spriteEffect, depth);
        }

        /// <summary>
        /// Draw the sprite in UI coordinates
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void DrawUI(SpriteBatch spriteBatch)
        {
            if (texture == null || !IsUI)
                return;

            frame = Math.Clamp(frame, 0, frameCount - 1);
            depth = GetDepth();
            origin = new Vector2(frameWidth / 2, frameHeight / 2);
            spriteBatch.Draw(texture, Position+ Offset, spriteBox, Tint, Rotation, origin, Scale+SpriteScale, spriteEffect, depth);
        }

        /// <summary>
        /// Advance the animation
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (texture == null || frameCount <= 1)
                return;

            if (animated)
            {
                Animate(gameTime);
            }
            else
            {
                spriteBox.X = 0;
                spriteBox.Y = 0;
                spriteBox.Width = frameWidth > 0 ? frameWidth : texture.Width;
            }

            int fh = frameHeight > 0 ? frameHeight : texture.Height;
            spriteBox.Height = Math.Min(fh, texture.Height);
        }

        /// <summary>
        /// Animates the sprite
        /// </summary>
        /// <param name="gameTime"></param>
        public void Animate(GameTime gameTime)
        {
            if (frameCount > 1)
            {
                frameTimer += frameSpeed * dt;

                // Advance the frame
                if (frameTimer >= 1f)
                {
                    frame = (frame + 1) % frameCount;
                    frameTimer = 0f;
                }
            }
        }
    }
}

