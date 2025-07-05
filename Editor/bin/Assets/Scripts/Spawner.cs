using Engine.Core;
using Microsoft.Xna.Framework;

public class Spawner : GameScript
{
    public override void OnClick()
    {
        // Spawn an Item at a random position near the clicked object
        var randomOffset = new Vector2(
            (float)(System.Random.Shared.NextDouble() * 100 - 50),
            (float)(System.Random.Shared.NextDouble() * 100 - 50)
        );
        
        var spawnPosition = Owner.position + randomOffset;
        var spawnedItem = SpawnObject("Item", spawnPosition);
        
        if (spawnedItem != null)
        {
            System.Console.WriteLine($"Spawned Item at position {spawnPosition}");
        }
        else
        {
            System.Console.WriteLine("Failed to spawn Item");
        }
    }
} 