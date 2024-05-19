using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

namespace IgorKL.ACAD3.Customization.Objects
{
    public abstract class CustomObject
    {
        public abstract ObjectId BlockId { get; }
    }
}
