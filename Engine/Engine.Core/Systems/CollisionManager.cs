/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         CollisionSystem.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Global system for managing 2D colliders and detecting collisions
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using Engine.Core.Game.Components.Collision;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;

namespace Engine.Core.Systems
{
    public static class CollisionSystem
    {
        private static readonly List<Collider2D> colliders = new();

        /// <summary>
        /// Initialize the collision system. This should be called once at the start of the game.
        /// </summary>
        public static void Initialize()
        {
            Clear();
        }

        /// <summary>
        /// Register a collider with the system.
        /// </summary>
        public static void Register(Collider2D collider)
        {
            if (!colliders.Contains(collider))
                colliders.Add(collider);
        }

        /// <summary>
        /// Unregister a collider from the system.
        /// </summary>
        public static void Unregister(Collider2D collider)
        {
            colliders.Remove(collider);
        }

        /// <summary>
        /// Call this once per frame to check for collisions.
        /// </summary>
        public static void Update(GameTime gameTime)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                for (int j = i + 1; j < colliders.Count; j++)
                {
                    var a = colliders[i];
                    var b = colliders[j];

                    if (a.Owner == null || b.Owner == null)
                        continue;

                    if (a.Intersects(b))
                    {
                        if (a.IsTrigger || b.IsTrigger)
                        {
                            a.OnTriggerEnter(b);
                            b.OnTriggerEnter(a);
                        }
                        else
                        {
                            a.OnCollisionEnter(b);
                            b.OnCollisionEnter(a);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Optional: Clear all registered colliders.
        /// </summary>
        public static void Clear()
        {
            colliders.Clear();
        }
    }
}
