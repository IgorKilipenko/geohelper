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

using IgorKL.ACAD3.Model.Helpers.SdrFormat;
using wnd = System.Windows.Forms;

namespace IgorKL.ACAD3.Model.Commands
{
    
    public class ToolsCmd
    {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_GetObjectTextInfo", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void GetObjectTextInfo()
        {
            PromptEntityOptions optinos = new PromptEntityOptions("\nSelect a object");
            optinos.AllowNone = false;
            optinos.AllowObjectOnLockedLayer = true;

            PromptEntityResult result = Tools.GetAcadEditor().GetEntity(optinos);
            if (result.Status != PromptStatus.OK)
                return;

            using (Transaction trans = Tools.StartOpenCloseTransaction())
            {
                object obj = trans.GetObject(result.ObjectId, OpenMode.ForRead);
                Tools.GetAcadEditor().WriteMessage("\nType full name = {0}", obj.GetType().FullName);
            }
        }
#endif
    }
}
