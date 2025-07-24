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
    public interface IInspectable
    {
        virtual void Render() { }
    }

    public interface IAssetHandler
    {
        void Load(string path);
        void Render();
        void Unload();
        void Open(string path);
        void Save(string path);
    }
}

