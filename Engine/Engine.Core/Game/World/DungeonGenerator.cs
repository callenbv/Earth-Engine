using Engine.Core.CustomMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game.World
{
    public enum Direction
    {
        Up,
        Down, 
        Left, 
        Right
    }

    public class DungeonGenerator
    {
        TileMap map;
        int brushSize = 5;
        int x = 0;
        int y = 0;
        int maxSteps = 25;
        Direction direction = Direction.Right;
        Direction previousDirection = Direction.Right;

        public DungeonGenerator(TileMap map)
        {
            this.map = map;
        }

        public void Generate()
        {
            for (int i = 0; i < maxSteps; i++)
            {
                PlaceFloor();
            }
        }

        /// <summary>
        /// Puts floor everywhere
        /// </summary>
        public void GenerateAllFloor()
        {
            for (int i = 0; i < map.Width; i++)
            {
                for (int j = 0; j < map.Width; j++)
                {
                    map.SetTile(x + i, y + j, 100);
                }
            }
        }

        public void PlaceFloor()
        {
            for (int i = 0; i < brushSize; i++)
            {
                for (int  j = 0; j < brushSize; j++)
                {
                    map.SetTile(x+i, y+j, 32);
                }
            }

            RandomizeDirection();
            Step(brushSize);
        }

        public void RandomizeDirection()
        {
            Direction nextDirection = (Direction)ERandom.Range(0, 3);

            if (nextDirection != previousDirection)
            {
                previousDirection = direction;
                direction = nextDirection;
            }
        }

        public void Step(int stepSize)
        {
            switch (direction)
            {
                case Direction.Up:
                    y += stepSize;
                    break;
                case Direction.Down:
                    y -= stepSize;
                    break;
                case Direction.Left:
                    x += stepSize;
                    break;
                case Direction.Right:
                    x -= stepSize;
                    break;
            }
        }
    }
}
