/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TilemapManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

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
        string path = Path.Combine(ProjectSettings.AbsoluteProjectPath, "Tilemaps", "tilemaps.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        TilemapSaveData saveData = new TilemapSaveData();
        foreach (var renderer in layers)
        {
            saveData.Layers.Add(renderer.ToData());
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        options.Converters.Add(new Vector2JsonConverter());

        var json = JsonSerializer.Serialize(saveData, options);
        File.WriteAllText(path, json);
    }

    public static void Load(string path)
    {
        Console.WriteLine($"[TilemapManager] Loading tilemaps from: {path}");
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        options.Converters.Add(new Vector2JsonConverter());
        var saveData = JsonSerializer.Deserialize<TilemapSaveData>(json, options);

        if (saveData == null) return;

        layers.Clear(); // Start fresh

        foreach (var layerData in saveData.Layers)
        {
            var texture = TextureLibrary.Instance.Get(layerData.TexturePath);

            var renderer = new TilemapRenderer
            {
                Texture = texture
            };

            renderer.ApplyData(layerData);
            layers.Add(renderer);
        }
    }
}

