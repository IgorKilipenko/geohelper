using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class VisualAcadPropertyAttribute:Attribute
    {
        public VisualAcadPropertyAttribute(string name)
        {

        }
    }
}
