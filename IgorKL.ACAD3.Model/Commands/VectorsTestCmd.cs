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
using stat = IgorKL.ACAD3.Model.Helpers.Math.Statistical;

namespace IgorKL.ACAD3.Model.Commands
{
#if DEBUG
    public class VectorsTestCmd
    {
        [RibbonCommandButton("Проекция векторов", RibbonPanelCategories.Test_Points)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestDotProductVectors", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestDotProductVectors()
        {
            Line firstLine;
            Line secondLine;

            if (!ObjectCollector.TrySelectAllowedClassObject(out firstLine))
                return;

            if (!ObjectCollector.TrySelectAllowedClassObject(out secondLine))
                return;

            double ang = (firstLine.EndPoint - firstLine.StartPoint).GetAngle2d(secondLine.EndPoint-secondLine.StartPoint);
            double dot = (firstLine.EndPoint - firstLine.StartPoint).DotProduct(secondLine.EndPoint - secondLine.StartPoint);
            double cos = (firstLine.EndPoint - firstLine.StartPoint).GetCos2d(secondLine.EndPoint - secondLine.StartPoint);
            double project = cos * secondLine.Length;

            Tools.GetAcadEditor().WriteMessage("\nAngle = {0}", ang * 180d / Math.PI);
            Tools.GetAcadEditor().WriteMessage("\nDotProduct = {0}", dot);
            Tools.GetAcadEditor().WriteMessage("\nCos = {0}", cos);
            Tools.GetAcadEditor().WriteMessage("\nProject = {0}", project);
        }


        [RibbonCommandButton("Перпендикуляр", RibbonPanelCategories.Test_Points)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestPerpendicular", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestPerpendicular()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;
            Curve line;
            if (!ObjectCollector.TrySelectAllowedClassObject(out line))
                return;

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку: ");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;

            Tools.StartTransaction(() =>
            {
                /*Vector3d vector = ppr.Value - line.StartPoint;
                Vector3d lineVector = line.EndPoint - line.StartPoint;

                double cos = lineVector.GetCos2d(vector);
                if (cos >=0d && cos*vector.Length <= lineVector.Length)
                {
                    Point3d p = line.GetPointAtDist(cos * vector.Length);
                    Line perpendicular = new Line(ppr.Value, p);
                    perpendicular.SaveToDatebase();
                }*/

                if (line is Line)
                {
                    Line perpendicular = ((Line)line).GetOrthoNormalLine(ppr.Value, null, false);
                    if (perpendicular != null)
                        perpendicular.SaveToDatebase();
                }
                else if (line is Arc)
                {
                    Line perpendicular = ((Arc)line).GetOrthoNormalLine(ppr.Value ,false);
                    if (perpendicular != null)
                        perpendicular.SaveToDatebase();
                }
            });
        }


        [RibbonCommandButton("Перпендикуляр к полилинии", RibbonPanelCategories.Test_Points)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestPerpendicularToPolyline", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestPerpendicularToPolyline()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;
            Curve pline;
            if (!ObjectCollector.TrySelectAllowedClassObject(out pline))
                return;

            /*PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку: ");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/
            
            Tools.StartTransaction(() =>
            {
                /*Point3d? perpendicularPoint = pline.GetNormalPoint(ppr.Value);
                if (perpendicularPoint.HasValue)
                {
                    Line perpendicular = new Line(ppr.Value, perpendicularPoint.Value);
                    perpendicular.SaveToDatebase();
                }*/

                Drawing.PerpendicularVectorJigView view = new Drawing.PerpendicularVectorJigView(pline, ucs);
                view.StartJig();
            });
        }

        [RibbonCommandButton("Регрессия", RibbonPanelCategories.Lines_Dimensions)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestRegressSplit", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestRegressSplit()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;
            double r2Min = 0.5d;
            KeywordCollection keys = new KeywordCollection();
            keys.Add("R2", "R2", "R2", true, true);
            Func<PromptEntityResult, PromptStatus> promptAction = pr => 
                {
                    switch (pr.StringResult)
                    {
                        case "R2":
                            {
                                PromptDoubleOptions pdo = new PromptDoubleOptions("\nУкажите R^2: ");
                                pdo.AllowNegative = false;
                                pdo.UseDefaultValue = true;
                                pdo.DefaultValue = r2Min;

                                var res = Tools.GetAcadEditor().GetDouble(pdo);
                                if (res.Status != PromptStatus.OK)
                                    return PromptStatus.Cancel;
                                r2Min = res.Value;
                                return PromptStatus.OK;
                            }
                    }

                    return PromptStatus.Error;
                };
            Curve curve;
            if (!ObjectCollector.TrySelectAllowedClassObject(out curve, keys, promptAction))
                return;
            Line line = null;
            Tools.StartTransaction(() =>
            {
                Polyline pline = curve.ConvertToPolyline();
                var points = pline.GetPoints3d();
                Line regressTotal = stat.LinearRegression(pline);
                regressTotal.SaveToDatebase();

                int start = 0;
                for (int i = 1; i < pline.NumberOfVertices; i++)
                {
                    double r2;
                    stat.LinearRegression(pline, start, i+1, out r2);
                    r2 = double.IsNaN(r2) ? 1d : r2;
                    if (Math.Abs(r2 - r2Min) < Tolerance.Global.EqualVector)
                    {
                        line = new Line(pline.GetPoint3dAt(start), pline.GetPoint3dAt(i-1));
                        start = i-1;
                        line.SaveToDatebase();
                    }
                }
                line = new Line(pline.GetPoint3dAt(start), pline.EndPoint);
                line.SaveToDatebase();
            });
        }

    }
#endif
}
