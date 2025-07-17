using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game.Components
{
    public static class ComponentRegistry
    {
            public static readonly Dictionary<string, Type> Types = new()
        {
            { "Sprite2D", typeof(Sprite2D) },
            { "Transform", typeof(Transform) },
            { "TilemapRenderer", typeof(TilemapRenderer) },
            { "PointLight", typeof(PointLight) },
            { "TextRenderer", typeof(TextRenderer) },
            { "UITextRenderer", typeof(UITextRenderer) },
        };
    }
}
