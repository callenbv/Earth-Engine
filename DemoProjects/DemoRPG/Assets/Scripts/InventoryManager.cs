using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Item
{ 
    
}

public class InventoryManager : GameScript
{
    public int Keys = 0;
    public static InventoryManager Instance { get; private set; }

    public InventoryManager()
    {
        Instance = this;
    }
}

