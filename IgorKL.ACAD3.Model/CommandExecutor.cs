using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model
{
    public class CommandExecutor
    {
        public static void Execute(string cmd, bool echo)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd + " ", true, false, echo);
        }

        public static bool Execute(Action cmdMethod, bool echo)
        {
            var attr = _getCmdAttribute(cmdMethod);
            if (attr == null)
                return false;

            Execute(attr.GlobalName, echo);
            return true;
        }

        private static Autodesk.AutoCAD.Runtime.CommandMethodAttribute _getCmdAttribute(Action action)
        {
            var objs = action.Method.GetCustomAttributes(typeof(Autodesk.AutoCAD.Runtime.CommandMethodAttribute), false);
            if (objs == null)
                return null;

            return objs.Cast<Autodesk.AutoCAD.Runtime.CommandMethodAttribute>().FirstOrDefault();
        }
    }
}
