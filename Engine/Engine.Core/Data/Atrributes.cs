using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HideInInspectorAttribute : Attribute { }

}
