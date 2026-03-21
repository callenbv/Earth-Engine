using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Door : GameScript
{
    public override void OnClick()
    {
        if (InventoryManager.Instance.Keys > 0)
        {
            InventoryManager.Instance.Keys--;
            Owner.Destroy();
        }
        else
        {
            Console.WriteLine("The door is locked. You need a key to open it.");
        }
    }
}

