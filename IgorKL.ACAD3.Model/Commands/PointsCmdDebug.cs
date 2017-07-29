using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Autodesk.Civil.DatabaseServices;

namespace IgorKL.ACAD3.Model.Commands
{
    public partial class PointsCmd
    {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_CreatePointFromText", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_CreatePointFromText()
        {
            ObjectId id = AcadPoints.PointFactory.CreateFromTxet("100;200;22", ";");
            Tools.GetAcadEditor().WriteMessage("\n" + (id == ObjectId.Null));
        }




#endif
    }
}
