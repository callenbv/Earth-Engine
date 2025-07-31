/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Collider2D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Basic 2D hitbox collision          
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;

namespace Engine.Core.Game.Components.Collision
{
    /// <summary>
    /// Represents a rectangle in 2D space with floating-point precision.
    /// </summary>
    public struct RectangleF
    {
        public float X, Y, Width, Height;

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Creates a new rectangle with the specified position and size.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Checks if this rectangle contains a point in world space.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Vector2 point)
        {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        /// Checks if this rectangle intersects with another rectangle.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(RectangleF other)
        {
            return !(other.Left > Right || other.Right < Left || other.Top > Bottom || other.Bottom < Top);
        }
    }

    /// <summary>
    /// Represents a point light component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Collision")]
    public class Collider2D : ObjectComponent
    {
        public override string Name => "Collider 2D";

        /// <summary>
        /// Size of the collider.
        /// </summary>
        public Vector2 Size = new Vector2(32, 32);

        /// <summary>
        /// Offset of the collider from the owner's position.
        /// </summary>
        public Vector2 Offset = Vector2.Zero;

        /// <summary>
        /// Whether this collider is a trigger (doesn't block, but detects).
        /// </summary>
        public bool IsTrigger = false;

        /// <summary>
        /// Gets the world-space bounding box of the collider.
        /// </summary>
        public RectangleF Bounds
        {
            get
            {
                var pos = Owner.Position + Offset;
                return new RectangleF(pos.X, pos.Y, Size.X, Size.Y);
            }
        }

        /// <summary>
        /// Checks if this collider intersects another.
        /// </summary>
        public bool Intersects(Collider2D other)
        {
            return Bounds.Intersects(other.Bounds);
        }

        /// <summary>
        /// Checks if this collider contains a point in world space.
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return Bounds.Contains(point);
        }

        /// <summary>
        /// Registers this collider with the global collision system.
        /// </summary>
        public override void Create()
        {
            CollisionSystem.Register(this);
        }

        /// <summary>
        /// Unregisters this collider from the global collision system.
        /// </summary>
        public override void Destroy()
        {
            CollisionSystem.Unregister(this);
        }

        /// <summary>
        /// Called when this collider collides with another collider.
        /// </summary>
        /// <param name="other"></param>
        public virtual void OnCollisionEnter(Collider2D other)
        {

        }

        /// <summary>
        /// Called when this collider enters a trigger with another collider.
        /// </summary>
        /// <param name="other"></param>
        public virtual void OnTriggerEnter(Collider2D other)
        {

        }
    }
}

