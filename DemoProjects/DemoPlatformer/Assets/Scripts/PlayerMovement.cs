using Engine.Core;
using Engine.Core.Audio;
using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;

public class PlayerMovement : GameScript
{
    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [SliderEditor(0, 200)]
    public float Speed = 100f;
    public int AnimationSpeed = 6;
    private Direction direction;
    public string PlayerName = string.Empty;

    private bool isStairCutsceneRunning = false;
    private Vector3 stairStartPos;
    private Vector3 stairEndPos;
    private float stairTimer = 0f;
    private float stairDuration = 0.5f;
    private int stairHeightChange = 0;
    private int inputDir = 0;

    public override void Create()
    {
        //Audio.Play("CastleOutside",true);
    }

    public override void Update(GameTime gameTime)
    {
        HandleMovement();
    }

    public override void OnCollisionTrigger(Collider2D other)
    {
        if (other.Tags.Contains("Chest"))
        {
            Owner.GetComponent<Health>().CurrentHealth -= 1;
        }
    }

    private void HandleMovement()
    {
        Owner.Sprite.frameSpeed = AnimationSpeed;

        var text = Owner.GetComponent<TextRenderer>();
        text.Text = PlayerName;

        Vector3 input = Vector3.Zero;

        if (InputAction.IsDown(InputID.MoveUp))
        {
            input.Y -= 1;
            direction = Direction.Up;
        }
        if (InputAction.IsDown(InputID.MoveDown))
        {
            direction = Direction.Down;
            input.Y += 1;
        }
        if (InputAction.IsDown(InputID.MoveLeft))
        {
            direction = Direction.Left;
            input.X -= 1;
        }
        if (InputAction.IsDown(InputID.MoveRight))
        {
            direction = Direction.Right;
            input.X += 1;
        }

        if (input != Vector3.Zero)
        {
            input.Normalize();
            Owner.Position += input * Speed * dt;

            // Choose sprite based on last input direction
            if (Math.Abs(input.X) > Math.Abs(input.Y))
            {
                // Horizontal priority
                if (input.X > 0)
                    Owner.Sprite.Set("PlayerWalkRight", 32, 32);
                else
                    Owner.Sprite.Set("PlayerWalkLeft", 32, 32);
            }
            else
            {
                // Vertical priority
                if (input.Y > 0)
                    Owner.Sprite.Set("PlayerWalkDown", 32, 32);
                else
                    Owner.Sprite.Set("PlayerWalkUp", 32, 32);
            }
        }
        else
        {
            switch (direction)
            { 
                case Direction.Up:
                    Owner.Sprite.Set("PlayerIdleUp", 32, 32);
                    break;
                case Direction.Down:
                    Owner.Sprite.Set("PlayerIdleDown", 32, 32);
                    break;
                case Direction.Left:
                    Owner.Sprite.Set("PlayerIdleLeft", 32, 32);
                    break;
                case Direction.Right:
                    Owner.Sprite.Set("PlayerIdleRight", 32, 32);
                    break;
            }

            Owner.Sprite.frameSpeed = 0;
            Owner.Sprite.frame = 0;
            Owner.Sprite.frameCount = 1;
        }
    }
}
