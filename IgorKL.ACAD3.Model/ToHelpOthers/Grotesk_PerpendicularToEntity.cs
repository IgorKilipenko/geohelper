using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.ToHelpOthers
{
    public class Grotesk_PerpendicularToEntity
    {
        [RibbonCommandButton("Пераендикуляр к линии", "Для ГЕОДЕЗИСТ.РУ")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_GetPerpendicularToEntity")]
        public void iCmd_GetPerpendicularToEntity()
        {
            Polyline line;
            if (!ObjectCollector.TrySelectAllowedClassObject(out line, "\nВыберите линию"))
                return;
            if (line != null)
            {
                PromptPointOptions opt = new PromptPointOptions("\nУкажите точку");
                opt.Keywords.Add("EXit", "ВЫХод", "ВЫХод");
                opt.Keywords.Add("SELectPoints", "ТОЧки", "ТОЧки");
                opt.Keywords.Add("LINe", "ЛИНия", "ЛИНия");
                PromptPointResult res = Tools.GetAcadEditor().GetPoint(opt);

                List<Point3d> points = new List<Point3d>();

                while (res.Status == PromptStatus.OK || res.Status == PromptStatus.Keyword)
                {
                    if (res.Status == PromptStatus.Keyword)
                    {
                        switch (res.StringResult)
                        {
                            case "EXit":
                                {
                                    return;
                                }
                            case "SELectPoints":
                                {
                                    List<DBPoint> pointSet;
                                    if (!ObjectCollector.TrySelectObjects(out pointSet, "\nВыберите точки"))
                                        return;

                                    points.Clear();

                                    foreach (var p in pointSet)
                                    {
                                        Tools.StartTransaction(() =>
                                            {
                                                points.Add(p.Id.GetObject<DBPoint>(OpenMode.ForRead).Position);
                                            });
                                    }
                                    break;
                                }
                            case "LINe":
                                {
                                    Polyline pline;
                                    if (!ObjectCollector.TrySelectAllowedClassObject(out pline, "\nВыберите точки"))
                                        return;

                                    points.Clear();

                                    foreach (var p in pline.GetPoints3d())
                                    {
                                        points.Add(p);
                                    }
                                    break;
                                }
                        }
                       
                    }

                    //Line resLine = line.GetPerpendicularFromPoint(res.Value);

                    if (res.Status == PromptStatus.OK)
                        points.Add(res.Value);

                    foreach (var p in points)
                    {
                        Line resLine = line.GetOrthoNormalLine(p, null, true);
                        if (resLine != null)
                            Tools.AppendEntity(new[] { resLine });
                    }

                    points.Clear();
                    res = Tools.GetAcadEditor().GetPoint(opt);
                }

            }
        }
    }
}
