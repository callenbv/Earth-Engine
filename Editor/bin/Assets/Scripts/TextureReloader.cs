using Engine.Core;
using Microsoft.Xna.Framework;

public class TextureReloader : GameScript
{
    private string spriteFileName = "";
    
    public override void Create()
    {
        // Store the sprite filename when the object is created
        // We'll need to get this from the object data, but for now we'll use a default
        spriteFileName = "spr_fish_cell.png"; // This should come from the object data
    }
    
    public override void OnClick()
    {
        // Force reload the sprite texture
        ReloadTexture(spriteFileName);
        System.Console.WriteLine($"Reloaded texture: {spriteFileName}");
    }
} 