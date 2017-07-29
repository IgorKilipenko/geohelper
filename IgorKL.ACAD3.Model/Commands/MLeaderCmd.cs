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

using IgorKL.ACAD3.Model.Helpers.SdrFormat;
using wnd = System.Windows.Forms;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Commands
{
    public class MLeaderCmd
    {
        [RibbonCommandButton("Копировать текст", "Текст/Аннотации")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_CopyMLeaderTextContext", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void CopyMLeaderTextContext()
        {
            MLeader sourceLeader;
            MLeader destLeader;

            if (!ObjectCollector.TrySelectAllowedClassObject(out sourceLeader, "\nВыберите мультивыноску - источник"))
                return;
            if (!ObjectCollector.TrySelectAllowedClassObject(out destLeader, "\nВыберите мультивыноску - назначение"))
                return;

            //destLeader = trans.GetObject(destLeader.Id, OpenMode.ForWrite) as MLeader;
            string res = MLeaders.MLeaderTools.CopyTextContents(sourceLeader, destLeader);
            //trans.Commit();
            Tools.GetAcadEditor().WriteMessage("\n" + res);
        }

        [RibbonCommandButton("Выбор текста с шагом", "Текст/Аннотации")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmd_SelectTextAtGrid")]
        public static void SelectTextAtGrid()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;

            List<DBText> res = new List<DBText>();

            DBText sourceObj;
            if (!ObjectCollector.TrySelectAllowedClassObject<DBText>(out sourceObj, "\nВыберите первый текстовый элемент: "))
                return;
            DBText destObj;
            if (!ObjectCollector.TrySelectAllowedClassObject<DBText>(out destObj, "\nВыберите второй текстовый элемент: "))
                return;

            List<DBText> selectedTexts;
            if (!ObjectCollector.TrySelectObjects(out selectedTexts, "\nВыберите исходный массив: "))
                return;


            double textHeight = 0;
            Tools.StartTransaction(() =>
            {
                textHeight = sourceObj.Id.GetObjectForRead<DBText>(false).Height;
            });

            Vector3d vector = destObj.Position - sourceObj.Position;

            double angle = vector.GetAngleTo(ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Zaxis.Negate());
            Matrix3d rot = Matrix3d.Rotation(-angle, ucs.CoordinateSystem3d.Zaxis, sourceObj.Position);
            Matrix3d displace = Matrix3d.Displacement(ucs.CoordinateSystem3d.Origin - sourceObj.Position);

            var transformedText = selectedTexts.Select(ent => new KeyValuePair<Point3d, ObjectId>(ent.Position.TransformBy(rot).TransformBy(displace), ent.Id));
            Tools.StartTransaction(() =>
            {
                List<ObjectId> selected = new List<ObjectId>();
                foreach (var text in transformedText)
                {
                    Vector3d v = text.Key - sourceObj.Position.TransformBy(displace);

                    double tolerance = textHeight * 0.1 + (v.Length - vector.Length) * Tolerance.Global.EqualPoint * 100;

                    double dx = Math.Abs(v.X % vector.Length);
                    double dy = Math.Abs(v.Y) % vector.Length;

                    if (dx < tolerance || Math.Abs(dx - vector.Length) < tolerance)
                        if (dy < tolerance || Math.Abs(dy - vector.Length) < tolerance)
                        {
                            //text.Value.GetObjectForWrite<DBText>(false).ColorIndex = 181;
                            selected.Add(text.Value);
                        }
                }
                Tools.GetAcadEditor().SetImpliedSelection(selected.ToArray());
            });
        }
    }

}
