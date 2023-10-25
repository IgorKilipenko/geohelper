using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace IgorKL.ACAD3.Model.Commands {

    public class ToolsCmd {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_GetObjectTextInfo", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void GetObjectTextInfo() {
            PromptEntityOptions optinos = new PromptEntityOptions("\nSelect a object");
            optinos.AllowNone = false;
            optinos.AllowObjectOnLockedLayer = true;

            PromptEntityResult result = Tools.GetAcadEditor().GetEntity(optinos);
            if (result.Status != PromptStatus.OK)
                return;

            using (Transaction trans = Tools.StartOpenCloseTransaction()) {
                object obj = trans.GetObject(result.ObjectId, OpenMode.ForRead);
                Tools.GetAcadEditor().WriteMessage("\nType full name = {0}", obj.GetType().FullName);
            }
        }
#endif
    }
}
