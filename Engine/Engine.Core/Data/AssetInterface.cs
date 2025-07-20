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
