using Engine.Core;
using Engine.Core.Game;
using Microsoft.Xna.Framework;

public class Resource : GameScript
{
    public override void OnClick()
    {
        Owner.Destroy();
    }

    public override void Destroy()
    {
        var spawnedItem = GameObject.Instantiate("Item", Owner.position);

        if (spawnedItem != null)
        {
            System.Console.WriteLine($"Spawned Item at position {Owner.position}");
        }
        else
        {
            System.Console.WriteLine("Failed to spawn Item");
        }
    }
}