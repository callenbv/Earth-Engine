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
        public bool IsDestroyed { get; private set; } = false;
        
        // Add any other properties you want scripts to access
        
        public void Destroy()
        {
            if (IsDestroyed) return;
            
            IsDestroyed = true;
            
            // Call Destroy on all attached scripts
            foreach (var script in scriptInstances)
            {
                if (script is GameScript gs)
                {
                    try
                    {
                        gs.Destroy();
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"Error calling Destroy on script for {name}: {ex.Message}");
                    }
                }
            }
        }
    }
} 