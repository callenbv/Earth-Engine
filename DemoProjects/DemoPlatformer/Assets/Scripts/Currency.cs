using Engine.Core;
using Engine.Core.Game.Components;

public class Currency : GameScript
{
    public int Value = 1;

    public override void OnCollisionTrigger(Collider2D other)
    {
        foreach (var tag in other.Tags)
        {
            Console.WriteLine(tag);
        }

        if (other.Tags.Contains("Player"))
        {
            Owner.Destroy();
            //StatsManager.Instance.Money += Value;
        }
    }
}

