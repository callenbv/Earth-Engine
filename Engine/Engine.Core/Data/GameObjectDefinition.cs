/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameObjectDefinition.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    public interface IComponentContainer
    {
        string Name { get; }
        List<IComponent> components { get; }
    }
    public class GameObjectDefinition : IComponentContainer
    {
        public string Name { get; set; } = "Unnamed";
        public List<IComponent> components { get; set; } = new();
    }
}

