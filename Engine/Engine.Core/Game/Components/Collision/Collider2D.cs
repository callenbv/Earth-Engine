/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Collider2D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Basic 2D hitbox collision          
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Graphics;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using System;

namespace Engine.Core.Game.Components
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
        public TilemapRenderer Tilemap { get; set; } = null!;

        /// <summary>
        /// Size of the collider.
        /// </summary>
        public Vector2 Size = new Vector2(16, 16);

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
            Console.WriteLine("Colliding");
        }

        /// <summary>
        /// Called when this collider enters a trigger with another collider.
        /// </summary>
        /// <param name="other"></param>
        public virtual void OnTriggerEnter(Collider2D other)
        {
            Console.WriteLine("Triggered");
        }

        /// <summary>
        /// Called when this collider collides with a tile in a tilemap.
        /// </summary>
        public void OnTileCollision()
        {
            Console.WriteLine($"{OldPosition},{Position}");
            Position = OldPosition;
        }

        /// <summary>
        /// Updates the collider each frame. This can be used to update the position or size dynamically.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!EngineContext.Debug)
                return;

            var bounds = Bounds;
            var rect = new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
            spriteBatch.Draw(TextureLibrary.Instance.PixelTexture, rect, IsTrigger ? Color.Green * 0.5f : Color.Red * 0.5f);
        }

        /// <summary>
        /// Checks if this collider collides with any solid tiles in the given tilemap.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <returns></returns>
        public bool CollidesWithTiles(TilemapRenderer tilemap)
        {
            if (tilemap == null)
                return false;

            // Only collide on same layer
            if (tilemap.FloorLevel != Owner.Height)
                return false;

            var bounds = this.Bounds;

            // Convert bounding box to tile coordinates
            int xStart = (int)(bounds.Left / tilemap.TileSize);
            int yStart = (int)(bounds.Top / tilemap.TileSize);
            int xEnd = (int)(bounds.Right / tilemap.TileSize);
            int yEnd = (int)(bounds.Bottom / tilemap.TileSize);

            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    if (tilemap.IsSolidAtTile(x, y))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this collider is colliding with any solid tiles in the tilemaps at the owner's height.
        /// </summary>
        /// <returns></returns>
        public bool IsCollidingWithTiles()
        {
            foreach (var tilemap in TilemapManager.GetTilemapsAtFloor(Owner.Height))
            {
                if (!IsTrigger && CollidesWithTiles(tilemap))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all tiles this collider is currently overlapping across all tilemaps at the owner's height.
        /// </summary>
        public List<(int X, int Y, TilemapRenderer Map)> GetOverlappingTiles()
        {
            List<(int X, int Y, TilemapRenderer Map)> overlappingTiles = new();

            var bounds = this.Bounds;
            int floor = Owner.Height;

            foreach (var tilemap in TilemapManager.GetTilemapsAtFloor(floor))
            {
                if (tilemap.TileSize <= 0) continue;

                int xStart = (int)(bounds.Left / tilemap.TileSize);
                int yStart = (int)(bounds.Top / tilemap.TileSize);
                int xEnd = (int)(bounds.Right / tilemap.TileSize);
                int yEnd = (int)(bounds.Bottom / tilemap.TileSize);

                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        if (tilemap.IsValidTile(x, y))
                        {
                            overlappingTiles.Add((x, y, tilemap));
                        }
                    }
                }
            }

            return overlappingTiles;
        }

        /// <summary>
        /// Gets all tiles this collider is currently overlapping across all tilemaps (ignores floor level).
        /// </summary>
        public List<(int X, int Y, TilemapRenderer Map)> GetOverlappingTilesAllFloors()
        {
            List<(int X, int Y, TilemapRenderer Map)> overlappingTiles = new();


            var bounds = this.Bounds;

            foreach (var tilemap in TilemapManager.layers) // Don't filter by floor
            {
                if (tilemap.TileSize <= 0) continue;

                int xStart = (int)(bounds.Left / tilemap.TileSize);
                int yStart = (int)(bounds.Top / tilemap.TileSize);
                int xEnd = (int)(bounds.Right / tilemap.TileSize);
                int yEnd = (int)(bounds.Bottom / tilemap.TileSize);

                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        if (tilemap.IsValidTile(x, y))
                        {
                            overlappingTiles.Add((x, y, tilemap));
                        }
                    }
                }
            }

            return overlappingTiles;
        }

        /// <summary>
        /// Updates the collider each frame. This can be used to check for tile collisions or other logic.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (Owner == null)
                return;

            if (!IsTrigger && IsCollidingWithTiles())
            {
                OnTileCollision();
            }
        }
    }
}

