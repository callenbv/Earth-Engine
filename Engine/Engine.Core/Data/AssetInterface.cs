using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    public interface IInspectable
    {
        void Render();
    }

    public interface IAssetHandler
    {
        void Load(string path);
        void Render();
        void Unload();
        void Open();
        void Save(string path);
    }
}
