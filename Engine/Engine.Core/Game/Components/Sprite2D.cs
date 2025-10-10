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
        public Vector3 Offset;

        /// <summary>
        /// The position of the sprite in world coordinates. This is where the sprite will be drawn in the game world.
        /// </summary>
        [SliderEditor(1f, 10f)]
        public Vector3 SpriteScale;

        /// <summary>
        /// Height in terms of layers, used for tilemap depth ordering
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// The position of the sprite in world coordinates. This is where the sprite will be drawn in the game world.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The texture used for the sprite. If set, it will automatically update the texturePath, frameWidth, frameHeight, and spriteBox properties.
        /// </summary>
        private Texture2D? _texture;

        /// <summary>
        /// AlphaTestEffect for 3D sprite rendering with proper transparency
        /// </summary>
        [JsonIgnore]
        private AlphaTestEffect? _spriteEffect;

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

            float feetY = Math.Abs(Position.Y) + frameHeight / 2;

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
            frame = Math.Clamp(frame, 0, frameCount - 1);
            depth = GetDepth();
            origin = new Vector2(frameWidth / 2, frameHeight / 2);
            spriteBatch.Draw(texture, new Vector2(Position.X, Position.Y) + new Vector2(Offset.X, Offset.Y), spriteBox, Tint, Rotation, origin, new Vector2(Scale.X, Scale.Y) + new Vector2(SpriteScale.X, SpriteScale.Y), spriteEffect, depth);
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
            spriteBatch.Draw(texture, new Vector2(Position.X, Position.Y) + new Vector2(Offset.X, Offset.Y), spriteBox, Tint, Rotation, origin, new Vector2(Scale.X, Scale.Y) + new Vector2(SpriteScale.X, SpriteScale.Y), spriteEffect, depth);
        }

        /// <summary>
        /// Draw the sprite in 3D space using BasicEffect
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw3D(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            if (texture == null || IsUI)
                return;

            frame = Math.Clamp(frame, 0, frameCount - 1);

            // Get Transform component for 3D positioning
            var transform = Owner.GetComponent<Transform>();
            Vector3 position = transform?.Position ?? new Vector3(Position.X, Position.Y, 0f);
            float rotation2D = transform?.Rotation ?? Rotation;
            Vector3 scale3D = transform?.Scale ?? new Vector3(Scale.X, Scale.Y, 1f);

            // Calculate depth for proper ordering
            depth = GetDepth();
            
            // Convert 2D position to 3D world coordinates (pixels -> world units)
            Vector3 worldPos = new Vector3(
                (position.X + Offset.X) / Engine.Core.EngineContext.UnitsPerPixel,
                -(position.Y + Offset.Y) / Engine.Core.EngineContext.UnitsPerPixel,
                (position.Z + depth * 100f) / Engine.Core.EngineContext.UnitsPerPixel // Apply depth to Z coordinate
            );

            // Calculate sprite dimensions in world units
            float spriteWidth = (frameWidth * (scale3D.X + SpriteScale.X)) / Engine.Core.EngineContext.UnitsPerPixel;
            float spriteHeight = (frameHeight * (scale3D.Y + SpriteScale.Y)) / Engine.Core.EngineContext.UnitsPerPixel;

            // Create sprite quad vertices
            var vertices = new VertexPositionColorTexture[4];
            var indices = new int[] { 0, 1, 2, 0, 2, 3 };

            // Calculate texture coordinates from sprite box
            float texLeft = (float)spriteBox.X / texture.Width;
            float texTop = (float)spriteBox.Y / texture.Height;
            float texRight = (float)(spriteBox.X + spriteBox.Width) / texture.Width;
            float texBottom = (float)(spriteBox.Y + spriteBox.Height) / texture.Height;

            // Create quad vertices (centered on sprite position)
            float halfWidth = spriteWidth * 0.5f;
            float halfHeight = spriteHeight * 0.5f;

            vertices[0] = new VertexPositionColorTexture(new Vector3(-halfWidth, halfHeight, 0), Tint, new Vector2(texLeft, texTop));
            vertices[1] = new VertexPositionColorTexture(new Vector3(halfWidth, halfHeight, 0), Tint, new Vector2(texRight, texTop));
            vertices[2] = new VertexPositionColorTexture(new Vector3(halfWidth, -halfHeight, 0), Tint, new Vector2(texRight, texBottom));
            vertices[3] = new VertexPositionColorTexture(new Vector3(-halfWidth, -halfHeight, 0), Tint, new Vector2(texLeft, texBottom));

            // Create world matrix with rotation and translation
            Matrix world = Matrix.CreateRotationZ(MathHelper.ToRadians(rotation2D)) * Matrix.CreateTranslation(worldPos);

            // Use AlphaTestEffect for proper transparency handling
            if (_spriteEffect == null)
            {
                _spriteEffect = new AlphaTestEffect(graphicsDevice)
                {
                    Texture = texture,
                    VertexColorEnabled = true,
                    AlphaFunction = CompareFunction.Greater,
                    ReferenceAlpha = 1 // Only discard completely transparent pixels (alpha = 0)
                };
            }

            var alphaEffect = (AlphaTestEffect)_spriteEffect;
            alphaEffect.Texture = texture;
            alphaEffect.World = world;
            alphaEffect.View = view;
            alphaEffect.Projection = projection;

            // Set render states for alpha blending
            var originalBlendState = graphicsDevice.BlendState;
            var originalDepthStencilState = graphicsDevice.DepthStencilState;
            var originalRasterizerState = graphicsDevice.RasterizerState;

            try
            {
                graphicsDevice.BlendState = BlendState.AlphaBlend;
                graphicsDevice.DepthStencilState = DepthStencilState.Default; // Use default depth testing for proper ordering
                graphicsDevice.RasterizerState = RasterizerState.CullNone;

                foreach (EffectPass pass in alphaEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
                }
            }
            finally
            {
                // Restore render states
                graphicsDevice.BlendState = originalBlendState;
                graphicsDevice.DepthStencilState = originalDepthStencilState;
                graphicsDevice.RasterizerState = originalRasterizerState;
            }
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
                Animate();
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
        public void Animate()
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

