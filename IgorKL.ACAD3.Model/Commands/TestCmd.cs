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

namespace IgorKL.ACAD3.Model.Commands
{
    public class TestCmd
    {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TextMirror", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TextMirror()
        {
            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            DBText text = new DBText();
            text.TextString = "Test";
            text.Position = new Point3d(0, 0, 0);
            text.HorizontalMode = TextHorizontalMode.TextMid;
            text.VerticalMode = TextVerticalMode.TextBase;
            text.AlignmentPoint = text.Position;
            text.Rotation = Math.PI / 4d;

            Tools.AppendEntityEx(text);

            Polyline pline = null;

            Tools.StartTransaction(() =>
                {
                    text = text.Id.GetObjectForRead<DBText>();
                    Rectangle3d? bounds = text.GetTextBoxCorners();

                    if (bounds.HasValue)
                    {
                        pline = new Polyline(5);
                        pline.AddVertexAt(0, bounds.Value.LowerLeft);
                        pline.AddVertexAt(1, bounds.Value.UpperLeft);
                        pline.AddVertexAt(2, bounds.Value.UpperRight);
                        pline.AddVertexAt(3, bounds.Value.LowerRight);
                        pline.AddVertexAt(4, bounds.Value.LowerLeft);
                    }

                });
            if (pline != null)
                Tools.AppendEntityEx(pline);

            DBTextMirroringJig textJig = new DBTextMirroringJig((DBText)text.Clone());
            if (textJig.Run() != PromptStatus.OK)
                return;
        }




        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_SelectPointAtPolygon", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void SelectPointAtPolygon()
        {
            Polyline pline;
            if (!ObjectCollector.TrySelectAllowedClassObject(out pline))
                return;

            Vector3d vector = pline.Bounds.Value.MaxPoint - pline.Bounds.Value.MinPoint;

            Point3d centralPoint = pline.Bounds.Value.MinPoint.Add(vector.DivideBy(2d));
            var randomPoints = centralPoint.CreateRandomCirclePoints(10000, vector.Length);

            List<ObjectId> pointIds = new List<ObjectId>();

            Tools.StartTransaction(() =>
            {
                pointIds = Tools.AppendEntity(
                    randomPoints.Select(x =>
                    {
                        return (new DBPoint(x));
                    }));
            });

            Tools.StartTransaction(() =>
                {
                    foreach (var id in pointIds)
                    {
                        DBPoint p = id.GetObjectForRead<DBPoint>();
                        if (pline.IsInsidePolygon(p.Position))
                        {
                            p.UpgradeOpen();
                            p.Highlight();
                        }
                    }
                });
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_DynamicBlockTest", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void DynamicBlockTest()
        {
            BlockReference br;
            if (!ObjectCollector.TrySelectAllowedClassObject(out br))
                return;

            Tools.StartTransaction(() =>
            {
                br = br.Id.GetObjectForRead<BlockReference>();
                var pSet = br.DynamicBlockReferencePropertyCollection;

                BlockTableRecord btr = br.BlockTableRecord.GetObjectForRead<BlockTableRecord>();

                if (pSet == null)
                    return;

                foreach (DynamicBlockReferenceProperty p in pSet)
                {
                    if (p.PropertyTypeCode == (short)PropertyTypeCodes.Mirror)
                    {
                        Type t = p.Value.GetType();
                        p.Value = (short)((short)p.Value == 0 ? 1 : 0);
                    } 
                }

                BlockTableRecord btrDyn = br.DynamicBlockTableRecord.GetObjectForRead<BlockTableRecord>();

                foreach (ObjectId id in btrDyn.GetBlockReferenceIds(false, false))
                {
                    var brDyn = id.GetObjectForRead<BlockReference>();
                    pSet = brDyn.DynamicBlockReferencePropertyCollection;

                    if (pSet == null)
                        return;

                    foreach (DynamicBlockReferenceProperty p in pSet)
                    {
                        object obj = p;
                    }
                }

                var rbuffer = btrDyn.XData;
                byte[] buffer = new byte[System.Runtime.InteropServices.Marshal.SizeOf(rbuffer.UnmanagedObject)];
                IntPtr destPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(rbuffer.UnmanagedObject);
                System.Runtime.InteropServices.Marshal.Copy(rbuffer.UnmanagedObject, buffer, 0, 
                    System.Runtime.InteropServices.Marshal.SizeOf(rbuffer.UnmanagedObject));
            });
        }

        //[RibbonCommandButton("AddCusPropertyField", RibbonPanelCategories.TestProperties)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("AddCusPropertyField")]
        public void AddCusPropertyField()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptPointOptions ppo = new
                        PromptPointOptions("\nSpecify insertion point: ");

            PromptPointResult ppr = ed.GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return;

            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                //%<\AcVar CustomDP.Address \f "%tc4">%
                MText text = new MText();
                text.Location = ppr.Value;
                ObjectId ModelSpaceId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);

                BlockTableRecord record = Tx.GetObject(ModelSpaceId,
                                     OpenMode.ForWrite) as BlockTableRecord;
                record.AppendEntity(text);
                Tx.AddNewlyCreatedDBObject(text, true);

                Field custom =
                   new Field("%<\\AcVar CustomDP.Address \\f \"%tc4\">%");

                custom.EvaluationOption = FieldEvaluationOptions.Automatic;
                custom.Evaluate((int)(FieldEvaluationOptions.Automatic), db);
                text.SetField(custom);
                Tx.AddNewlyCreatedDBObject(custom, true);

                Tx.Commit();

                ed.Regen();
            }
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmdTest_SelectAtDistanseAtVis")]
        public void SelectAtDistanseAtVis()
        {
            List<DBText> res = new List<DBText>();

            DBText sourceObj;
            if (!ObjectCollector.TrySelectAllowedClassObject<DBText>(out sourceObj))
                return;
            DBText destObj;
            if (!ObjectCollector.TrySelectAllowedClassObject<DBText>(out destObj))
                return;

            Rectangle3d? bounds = sourceObj.GetTextBoxCorners();
            if (!bounds.HasValue)
                return;
            Rectangle3d? destBounds = destObj.GetTextBoxCorners();
            if (!destBounds.HasValue)
                return;

            Vector3d vector =  destBounds.Value.LowerLeft - bounds.Value.LowerLeft;

            Drawing.SimpleGride gride1 = new Drawing.SimpleGride(bounds.Value.LowerLeft, vector.GetPerpendicularVector().Negate(), vector, vector.Length, vector.Length);

            TypedValue[] tvs = new[] {
                new TypedValue((int)DxfCode.Start, "TEXT")};

            //Tools.GetActiveAcadDocument().SendStringToExecute("._zoom _e ", true, false, false);


            Polyline rectg = new Polyline(5);
            rectg.AddVertexAt(0, bounds.Value.LowerLeft.Add((bounds.Value.LowerLeft - bounds.Value.UpperRight).MultiplyBy(0.2)));
            rectg.AddVertexAt(1, bounds.Value.UpperLeft.Add((bounds.Value.UpperLeft - bounds.Value.LowerRight).MultiplyBy(0.2)));
            rectg.AddVertexAt(2, bounds.Value.UpperRight.Add((bounds.Value.UpperRight - bounds.Value.LowerLeft).MultiplyBy(0.2)));
            rectg.AddVertexAt(3, bounds.Value.LowerRight.Add((bounds.Value.LowerRight - bounds.Value.UpperLeft).MultiplyBy(0.2)));
            rectg.AddVertexAt(4, bounds.Value.LowerLeft.Add((bounds.Value.LowerLeft - bounds.Value.UpperRight).MultiplyBy(0.2)));

            int count = 200;
            Point3d maxPointScreen = sourceObj.Position.Add(vector.MultiplyBy(count) +
                vector.GetPerpendicularVector().Negate().MultiplyBy(count*vector.Length));
            Point3d minPointScreen = sourceObj.Position.Add(vector.Negate().MultiplyBy(count) +
                vector.GetPerpendicularVector().MultiplyBy(count * vector.Length));
            Drawing.Helpers.Zoomer.Zoom(minPointScreen, maxPointScreen, new Point3d(), 1);

            Tools.StartTransaction(() =>
            {
                for (int r = -count; r < count; r++)
                {

                    for (int c = -count; c < count; c++)
                    {
                        var point1 = gride1.CalculateGridPoint(r, c);
                        Matrix3d mat = Matrix3d.Displacement(point1 - bounds.Value.LowerLeft);

                        var spres = Tools.GetAcadEditor().SelectWindowPolygon(((Polyline)rectg.GetTransformedCopy(mat)).GetPoints(),
                            new SelectionFilter(tvs));

                        //((Polyline)rectg.GetTransformedCopy(mat)).SaveToDatebase();

                        if (spres.Status == PromptStatus.OK)
                        {
                            DBText text = spres.Value.GetSelectedItems<DBText>().FirstOrDefault();
                            if (text != null)
                            {
                                text.UpgradeOpen();
                                text.ColorIndex = 181;
                                text.DowngradeOpen();
                            }
                        }


                        /*Entity[] marker = new[] { (Entity)new DBPoint(point1), (Entity)new Line(point1, point1.Add(bounds.Value.UpperRight - bounds.Value.LowerLeft)) };
                        marker.SaveToDatebase<Entity>();*/
                    }
                    
                }
            });
        }





        [RibbonCommandButton("Выбор текста по границе", RibbonPanelCategories.Test_Text)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmdTest_SelectTextAtBounds")]
        public void SelectTextAtBounds()
        {
            DBText sourceObj;
            if (!ObjectCollector.TrySelectAllowedClassObject<DBText>(out sourceObj))
                return;
            TypedValue[] tvs = new[] {
                new TypedValue((int)DxfCode.Start, "TEXT")};
            Tools.StartTransaction(() =>
            {
                Rectangle3d? bounds = sourceObj.GetTextBoxCorners();
                if (!bounds.HasValue)
                    return;

                Polyline pline = new Polyline(5);
                pline.AddVertexAt(0, bounds.Value.LowerLeft.Add((bounds.Value.LowerLeft - bounds.Value.UpperRight).MultiplyBy(0.1)));
                pline.AddVertexAt(1, bounds.Value.UpperLeft.Add((bounds.Value.UpperLeft - bounds.Value.LowerRight).MultiplyBy(0.1)));
                pline.AddVertexAt(2, bounds.Value.UpperRight.Add((bounds.Value.UpperRight - bounds.Value.LowerLeft).MultiplyBy(0.1)));
                pline.AddVertexAt(3, bounds.Value.LowerRight.Add((bounds.Value.LowerRight - bounds.Value.UpperLeft).MultiplyBy(0.1)));
                pline.AddVertexAt(4, bounds.Value.LowerLeft.Add((bounds.Value.LowerLeft - bounds.Value.UpperRight).MultiplyBy(0.1)));

                var spres = Tools.GetAcadEditor().SelectWindowPolygon(pline.GetPoints(), new SelectionFilter(tvs));

                pline.SaveToDatebase();

                if (spres.Status == PromptStatus.OK)
                {
                    DBText text = spres.Value.GetSelectedItems<DBText>().FirstOrDefault();
                    if (text != null)
                    {
                        text.UpgradeOpen();
                        text.ColorIndex = 181;
                        text.DowngradeOpen();
                    }
                }
            });
        }


        public enum PropertyTypeCodes:short
        {
            Mirror = 3
        }
#endif

        public class DBTextMirroringJig : DrawJig
        {
            private Point3d _position;
            public DBText _text;

            public DBTextMirroringJig(DBText text)
                :base()
            {
                _text = text;
            }

            protected override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts)
            {
                JigPromptPointOptions ppo = new JigPromptPointOptions("\nSelect point");
                ppo.UseBasePoint = true;
                ppo.BasePoint = _text.Position;

                ppo.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NoZeroResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);
                
                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                if (_position == ppr.Value)
                    return SamplerStatus.NoChange;

                _position = ppr.Value;

                return SamplerStatus.OK;
            }

            public PromptStatus Run()
            {
                PromptResult promptResult = Tools.GetAcadEditor().Drag(this);
                return promptResult.Status;
            }

            private DBText _getMirrorClone()
            {            
                var bounds = _text.GetTextBoxCorners();
                if (!bounds.HasValue)
                    return null;

                Line3d line = new Line3d(_text.Position, _position);

                Matrix3d mat = Matrix3d.Mirroring(line);
                //mat = mat.PreMultiplyBy(Matrix3d.Mirroring(line));

                DBText res = (DBText)_text.Clone();
                res.TransformBy(mat);

                return res;
            }

            protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
            {
                DBText inMemoryText = _getMirrorClone();

                draw.Geometry.Draw(inMemoryText);

                inMemoryText.Dispose();

                return true;
            }
        }
    }
}
