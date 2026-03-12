# Earth Engine

Earth Engine is a custom 2D game engine built in **C#** using the **MonoGame framework**.  
It is designed to support rapid development of indie games through modular gameplay systems, a custom rendering pipeline, and integrated developer tooling.

The engine is used as the foundation for several of my game projects and serves as a platform for experimenting with gameplay architecture, rendering techniques, and tooling.

---

## Features

- Entity Component System (ECS) architecture
- Custom 2D rendering pipeline
- Dynamic lighting and post-processing shaders
- Particle system
- Cross-platform runtime (Windows, Linux, macOS)
- ImGui-based editor for inspecting and managing entities

---

## Architecture

Earth Engine uses an **Entity Component System (ECS)** to organize game logic.

Core concepts include:

- **Entity** – unique game object identifier
- **Component** – data attached to an entity
- **System** – logic that processes entities with matching components
- **World** – manages entities and system updates

This architecture enables modular gameplay systems and encourages separation between engine code and game logic.

---

## Rendering

The engine includes a custom rendering pipeline built on top of MonoGame.

Key capabilities include:

- Sprite batching
- Particle rendering
- Dynamic lighting
- Post-processing effects
- Layered rendering

The renderer is designed to remain flexible while maintaining stable real-time performance.

---

## Editor

Earth Engine includes a lightweight editor built with **ImGui.NET** that provides tools for:

- Entity inspection
- Component editing
- Scene management
- Debug visualization

This allows developers to iterate on gameplay systems quickly during development.

---

## Technology Stack

- **Language:** C#
- **Framework:** MonoGame / Microsoft XNA Framework
- **UI:** ImGui.NET
- **Version Control:** Git

---

## Status

Earth Engine is actively used in my personal game projects and continues to evolve as new systems and tools are developed.

---

## License

This project is proprietary and intended for use in my personal game development projects.
