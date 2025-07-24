/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AssetInterface.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    /// <summary>
    /// Interface for any asset that can be inspected in the editor.
    /// </summary>
    public interface IInspectable
    {
        virtual void Render() { }
    }

    /// <summary>
    /// Interface for handling assets in the editor.
    /// </summary>
    public interface IAssetHandler
    {
        void Load(string path);
        virtual void Render() { }
        virtual void Unload() { }
        virtual void Open(string path) { }
        void Save(string path);
    }
}

