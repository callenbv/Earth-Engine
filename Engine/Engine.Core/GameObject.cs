using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Engine.Core
{
    public class GameObject
    {
        public string name;
        public Texture2D sprite;
        public Vector2 position;
        public List<object> scriptInstances = new List<object>();
        // Add any other properties you want scripts to access
    }
} 