using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine.Core.Data.Graphics
{
    /// <summary>
    /// Texture data acts as serializable wrapper for textures for ease of use
    /// </summary>
    public class TextureData
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TextureData()
        {

        }

        /// <summary>
        /// Sets the frame size and count 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="frameCount"></param>
        public TextureData(int width, int height, int frameCount)
        {
            this.frameCount = frameCount;
            this.frameWidth = width;
            this.frameHeight = height;
        }

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
                    frameWidth = _texture.Width / frameCount;
                    textureWidth = _texture.Width;
                    textureHeight = _texture.Height;
                    animated = frameCount > 1;
                    spriteBox = new Rectangle(0, 0, frameWidth, frameHeight);
                }
            }
        }
        private Texture2D? _texture;

        /// <summary>
        /// This a list of the sliced texture sources
        /// </summary>
        [HideInInspector]
        public List<TextureData> Textures = new List<TextureData>();

        /// <summary>
        /// Path of the texture
        /// </summary>
        [HideInInspector]
        public string texturePath;

        /// <summary>
        /// Height of the frame
        /// </summary>
        public int frameHeight { get; set; } = 16;

        /// <summary>
        /// Width of the frame
        /// </summary>
        public int frameWidth { get; set; } = 16;

        /// <summary>
        /// Width of whole texture
        /// </summary>
        [HideInInspector]
        public int textureWidth { get; set; } = 16;

        /// <summary>
        /// Height of the texture
        /// </summary>
        [HideInInspector]
        public int textureHeight { get; set; } = 16;

        /// <summary>
        /// If this texture has a 2D animation
        /// </summary>
        public bool animated { get; set; } = false;

        /// <summary>
        /// The number of frames
        /// </summary>
        public int frameCount { get; set; } = 1;

        /// <summary>
        /// The visible frame of this sprite, default 16x16 square
        /// </summary>
        public Rectangle spriteBox { get; set; } = new Rectangle(0,0,16,16);

        /// <summary>
        /// The color tint of this texture
        /// </summary>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Inititalize the texture data when loaded
        /// </summary>
        public void Initialize()
        {
            Texture2D tex = TextureLibrary.Instance.Get(texturePath);

            if (tex != null)
            {
                texture = tex;
            }
        }

        /// <summary>
        /// Slicing can be done to textures to get multiple frame sources
        /// </summary>
        public void Slice(int cellWidth, int cellHeight)
        {
            // Clear the textures
            Textures.Clear();

            // Create a new texture for each slice
            TextureData textureData = new TextureData(cellWidth, cellHeight, 1);

            // Get the number of slices needed
            int horizontalSlices = textureWidth / cellWidth;
            int verticalSlices = textureHeight / cellHeight;
            
            // For each texture, get its source frame
            for (int i = 0; i < horizontalSlices; i++)
            {
                for (int j = 0;  j < verticalSlices; j++)
                {
                    textureData.spriteBox = new Rectangle(i * cellWidth, j * cellHeight, cellWidth, cellHeight);
                }
            }

            // Add the new texture
            Textures.Add(textureData);
        }

        /// <summary>
        /// Draw the texture
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch, Rectangle source, Color color)
        {
            if (texture == null)
                return;

            spriteBatch.Draw(texture, source, color);
        }
    }
}
