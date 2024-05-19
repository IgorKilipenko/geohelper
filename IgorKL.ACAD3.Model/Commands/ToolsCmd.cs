using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace IgorKL.ACAD3.Model.Commands {

    public class ToolsCmd {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_GetObjectTextInfo", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void GetObjectTextInfo() {
            PromptEntityOptions options = new PromptEntityOptions("\nSelect a object");
            options.AllowNone = false;
            options.AllowObjectOnLockedLayer = true;

            PromptEntityResult result = Tools.GetAcadEditor().GetEntity(options);
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
