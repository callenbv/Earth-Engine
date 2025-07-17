using Engine.Core.Data;
using Engine.Core.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.Inspector
{
    public class InspectableGameObject : IInspectable
    {
        private readonly GameObject obj;
        public string Name = string.Empty;

        public InspectableGameObject(GameObject obj)
        {
            this.obj = obj;
            this.Name = obj.Name;
        }

        public void Render()
        {
            InspectorUI.DrawGameObject(this.obj);
        }
    }
}
