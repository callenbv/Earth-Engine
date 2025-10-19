/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TilemapManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Engine.Core.Rooms.Tiles;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Serialization.Json;
using System.Text.Json;

public static class TilemapManager
{
    public static List<TilemapRenderer> layers = new List<TilemapRenderer>();

    /// <summary>
    /// Render all tilemaps in the manager. Users can add their own, 
    /// </summary>
    public static void Render(SpriteBatch spriteBatch)
    {
        foreach (var layer in layers)
        {
            layer.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Save the static tilemaps per project
    /// </summary>
    public static void Save()
    {

    }

    public static void Load(string path)
    {

    }
}

