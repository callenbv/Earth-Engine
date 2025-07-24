/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameObjectDefinition.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game;
using Engine.Core.Game.Components;

namespace Engine.Core.Data
{
    /// <summary>
    /// Interface for a component container, which can hold multiple components.
    /// </summary>
    public interface IComponentContainer
    {
        string Name { get; }
        List<IComponent> components { get; }

        /// <summary>
        /// Adds a component to the container.
        /// </summary>
        /// <param name="component"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddComponent(ObjectComponent? component)
        {
            // Do not add NULL component
            if (component == null)
            {
                Console.WriteLine($"Tried to add null component");
                return;
            }

            // If we are a game object instance, set the owner
            if (this is GameObject gameObject)
                component.Owner = gameObject;

            // Add component
            component.Initialize();
            components.Add(component);
        }
    }

    /// <summary>
    /// Represents a game object definition that can hold multiple components.
    /// </summary>
    public class GameObjectDefinition : IComponentContainer
    {
        public string Name { get; set; } = "Unnamed";
        public List<IComponent> components { get; set; } = new();
    }
}

