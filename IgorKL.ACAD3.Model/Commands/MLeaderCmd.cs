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

namespace IgorKL.ACAD3.Model.Commands {
    public class MLeaderCmd {
        [RibbonCommandButton("Копировать текст", "Текст/Аннотации")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_CopyMLeaderTextContext", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void CopyMLeaderTextContext() {
            MLeader sourceLeader;
            MLeader destLeader;

            if (!ObjectCollector.TrySelectAllowedClassObject(out sourceLeader, "\nВыберите мультивыноску - источник"))
                return;
            if (!ObjectCollector.TrySelectAllowedClassObject(out destLeader, "\nВыберите мультивыноску - назначение"))
                return;

            string res = MLeaders.MLeaderTools.CopyTextContents(sourceLeader, destLeader);
            Tools.GetAcadEditor().WriteMessage("\n" + res);
        }

        [RibbonCommandButton("Выбор текста с шагом", "Текст/Аннотации")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmd_SelectTextAtGrid")]
        public static void SelectTextAtGrid() {
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
            Tools.StartTransaction(() => {
                textHeight = sourceObj.Id.GetObjectForRead<DBText>(false).Height;
            });

            Vector3d vector = destObj.Position - sourceObj.Position;

            double angle = vector.GetAngleTo(ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Zaxis.Negate());
            Matrix3d rot = Matrix3d.Rotation(-angle, ucs.CoordinateSystem3d.Zaxis, sourceObj.Position);
            Matrix3d displace = Matrix3d.Displacement(ucs.CoordinateSystem3d.Origin - sourceObj.Position);

            var transformedText = selectedTexts.Select(ent => new KeyValuePair<Point3d, ObjectId>(ent.Position.TransformBy(rot).TransformBy(displace), ent.Id));
            Tools.StartTransaction(() => {
                List<ObjectId> selected = new List<ObjectId>();
                foreach (var text in transformedText) {
                    Vector3d v = text.Key - sourceObj.Position.TransformBy(displace);

                    double tolerance = textHeight * 0.1 + (v.Length - vector.Length) * Tolerance.Global.EqualPoint * 100;

                    double dx = Math.Abs(v.X % vector.Length);
                    double dy = Math.Abs(v.Y) % vector.Length;

                    if (dx < tolerance || Math.Abs(dx - vector.Length) < tolerance)
                        if (dy < tolerance || Math.Abs(dy - vector.Length) < tolerance) {
                            selected.Add(text.Value);
                        }
                }
                Tools.GetAcadEditor().SetImpliedSelection(selected.ToArray());
            });
        }

        [RibbonCommandButton("Рамка текста", RibbonPanelCategories.Text_Annotations)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_AddBboxToText", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void AddBboxToText() {

            List<DBText> textItems;
            if (!ObjectCollector.TrySelectObjects<DBText>(out textItems, "\nВыберите текстовые объекты")) {
                return;
            }

            Tools.UseTransaction((Transaction trans, BlockTable acBlkTbl, BlockTableRecord acBlkTblRec) => {

                foreach (DBText text in textItems) {
                    DBText db_text = trans.GetObject(text.Id, OpenMode.ForRead) as DBText;
                    var bounds = db_text.GetTextBoxCorners();
                    if (!bounds.HasValue) {
                        continue;
                    }

                    double offset = db_text.Height * 0.1;
                    var layer = db_text.Layer;
                    var color = db_text.Color;

                    Polyline pline = new Polyline(4);
                    pline.AddVertexes(new List<Point3d> {
                            bounds.Value.LowerLeft,
                            bounds.Value.UpperLeft,
                            bounds.Value.UpperRight,
                            bounds.Value.LowerRight
                        }
                    );

                    pline.Closed = true;
                    pline.Layer = layer;
                    pline.Color = color;

                    var offsets = pline.GetOffsetCurves(-offset);
                    if (offsets.Count == 0 || !(offsets[0] is Polyline)) {
                        return;
                    }

                    pline = offsets[0] as Polyline;

                    pline.SetDatabaseDefaults();
                    acBlkTblRec.AppendEntity(pline);
                    trans.AddNewlyCreatedDBObject(pline, true);
                }

                trans.Commit();

            });
        }

        [RibbonCommandButton("Изменить отметку", RibbonPanelCategories.Text_Annotations)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_EditTextElevation", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void EditTextElevation() {
            string sep = ".";
            var culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            List<DBText> textItems;
            if (!ObjectCollector.TrySelectObjects<DBText>(out textItems, "\nВыберите текстовые объекты")) {
                return;
            }

            var valueOption = new PromptDoubleOptions("\nВведите значение приращения отметок") {
                DefaultValue = 0,
                UseDefaultValue = true,
            };

            var valueResult = Tools.GetAcadEditor().GetDouble(valueOption);
            if (valueResult.Status != PromptStatus.OK) {
                return;
            }
            double dh = valueResult.Value;


            Tools.UseTransaction((Transaction trans, BlockTable acBlkTbl, BlockTableRecord acBlkTblRec) => {

                foreach (DBText text in textItems) {
                    DBText db_text = trans.GetObject(text.Id, OpenMode.ForWrite) as DBText;
                    double textVal;
                    if (!double.TryParse(db_text.TextString.Replace(",", sep), System.Globalization.NumberStyles.Any, culture, out textVal)) {
                        continue;
                    }

                    textVal += dh;
                    db_text.TextString = Math.Round(textVal, 3).ToString("#0.000", culture);
                }

                trans.Commit();

            });

            valueOption = new PromptDoubleOptions("\nВведите значение множителя") {
                DefaultValue = 1000,
                UseDefaultValue = true,
            };

            valueResult = Tools.GetAcadEditor().GetDouble(valueOption);
            if (valueResult.Status != PromptStatus.OK || valueResult.Value == 1) {
                return;
            }
            double xh = valueResult.Value;

            Tools.UseTransaction((Transaction trans, BlockTable acBlkTbl, BlockTableRecord acBlkTblRec) => {

                foreach (DBText text in textItems) {
                    DBText db_text = trans.GetObject(text.Id, OpenMode.ForWrite) as DBText;
                    double textVal;
                    if (!double.TryParse(db_text.TextString, System.Globalization.NumberStyles.Any, culture, out textVal)) {
                        continue;
                    }

                    textVal *= xh;
                    db_text.TextString = textVal.ToString("+#;-#;0", culture);
                }

                trans.Commit();

            });
        }
    }
}
