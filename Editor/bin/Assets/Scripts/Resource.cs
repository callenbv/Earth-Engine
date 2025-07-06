using Engine.Core;
using Microsoft.Xna.Framework;

public class Resource : GameScript
{
    public override void OnClick()
    {
        Owner.Destroy();
    }

    public override void Destroy()
    {
        var spawnedItem = SpawnObject("Item", Owner.position);

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