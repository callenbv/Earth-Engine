using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Engine.Core.Rooms.Tiles;
using Microsoft.Xna.Framework.Graphics;
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

        var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static void Load(string path)
    {
        Console.WriteLine($"[TilemapManager] Loading tilemaps from: {path}");
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var saveData = JsonSerializer.Deserialize<TilemapSaveData>(json);
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
