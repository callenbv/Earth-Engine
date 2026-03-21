using Engine.Core;
using Engine.Core.CustomMath;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;

public class Chest : GameScript
{
    /// <summary>
    /// If this chest is opened or not
    /// </summary>
    public bool Open = false;

    /// <summary>
    /// Range of coins (min, max)
    /// </summary>
    public Vector2 Coins = Vector2.One;

    public override void OnClick()
    {
        OnOpen();
    }

    private void OnOpen()
    {
        if (Open)
            return;

        Owner.Destroy();
        Open = true;

        // Create coins
        int coins = ERandom.Range((int)Coins.X, (int)Coins.Y);

        for (int i = 0; i < coins; i++)
        {
            Vector3 position = Position + new Vector3(ERandom.Range(-16, 16), ERandom.Range(-16, 16), 0f);
            GameObject.Instantiate("Objects/AstralShard", position);
        }
    }
}

