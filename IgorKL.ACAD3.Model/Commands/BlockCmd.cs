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

using IgorKL.ACAD3.Model.AcadBlocks;
using wnd = System.Windows.Forms;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Commands
{
    public class BlockCmd
    {
#if DEBUG

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestAnonyBlock", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TestAnonyBlock()
        {
            Polyline pline = new Polyline(3);
            pline.AddVertexAt(0, new Point2d(0,0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + 3, pline.GetPoint2dAt(0).Y), 0, 0.4, 0);
            pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + 1.5, pline.GetPoint2dAt(1).Y), 0, 0, 0);
            pline.LineWeight = LineWeight.LineWeight020;

            AttributeDefinition ad = _createAttribute(pline.StartPoint, pline);

            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            var btrId = BlockTools.CreateBlockTableRecordEx(Point3d.Origin ,"test11111", new[] { (Entity)pline, (Entity)ad }.ToList(), AnnotativeStates.True);
            var brId = BlockTools.AddBlockRefToModelSpace(btrId, new[] {"3"}.ToList(), new Point3d(10, 10, 10), ucs);

            using (Transaction trans = Tools.StartTransaction())
            {
                BlockReference br = (BlockReference)brId.GetObject(OpenMode.ForRead, false, true);
                br.UpgradeOpen();
                br.TransformBy(Matrix3d.Rotation(Math.PI/2d, ucs.CoordinateSystem3d.Zaxis, br.Position));
                //br.Erase(true);
                trans.Commit();
            }

            AttributeReference ar;
            using (Transaction trans = Tools.StartTransaction())
            {
                BlockReference br = (BlockReference)brId.GetObject(OpenMode.ForRead, true, true);
                ar = br.GetAttributeByTag(ad.Tag, trans);
                ar = (AttributeReference)ar.Id.GetObject(OpenMode.ForRead, true,true);
                var anBtrId = BlockTools.GetAnonymCopy(brId, trans, false);
                var anBrId = BlockTools.AddBlockRefToModelSpace(anBtrId, new[] {ar.TextString}.ToList(), new Point3d(0, 0, 0), ucs);

                trans.Commit();
            }
        }


        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestAnonyBlock2", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TestAnonyBlock2()
        {
            

            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            ArrowDirectional2 adHorizontal = new ArrowDirectional2(ucs, ArrowDirectional2.DirectionMode.Horizontal);

            Point3d insertPoint = new Point3d(0, 0, 0);

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите втору точку");
            ppo.BasePoint = insertPoint;
            ppo.UseDashedLine = true;
            ppo.UseBasePoint = true;

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;

            var res = adHorizontal.PastNewArrowItem(insertPoint, ppr.Value);

            ppo = new PromptPointOptions("\nУкажите сторону отрисовки");
            ppo.BasePoint = insertPoint;
            ppo.UseDashedLine = true;
            ppo.UseBasePoint = true;

            ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            
            BlockReference br = null;
            Tools.StartTransaction(() =>
            {
                br = res.ArrowId.GetObjectForRead<BlockReference>();
                BlockReference cloneBr = adHorizontal.GetRedirectBlockReferenceCopy(br, ppr.Value);
                var id = br.GetAnonymClone(cloneBr.Position);
                if (cloneBr != null)
                {
                    br.UpgradeOpen();
                    br.Erase(true);
                    br = cloneBr;
                }
                else
                    br = null;
            });
            if (br != null)
                Tools.AppendEntityEx(br);
        }

        private AttributeDefinition _createAttribute(Point3d position, Polyline pline)
        {
            AttributeDefinition acAttDef = new AttributeDefinition();

            acAttDef.Verifiable = true;
            acAttDef.Height = 1.8;
            //acAttDef.Justify = AttachmentPoint.BaseMid;
            acAttDef.Prompt = "arrowPrompt";
            acAttDef.Tag = "arrowTag";
            acAttDef.TextString = "NaN";
            acAttDef.HorizontalMode = TextHorizontalMode.TextCenter;
            acAttDef.VerticalMode = TextVerticalMode.TextBase;
            acAttDef.HorizontalMode = TextHorizontalMode.TextCenter;
            acAttDef.VerticalMode = TextVerticalMode.TextBase;
            acAttDef.Position = position;
            acAttDef.AlignmentPoint = acAttDef.Position.TransformBy(Matrix3d.Displacement(pline.GetPointAtDist(pline.Length / 2d) - acAttDef.Position));
            acAttDef.AlignmentPoint = acAttDef.AlignmentPoint.TransformBy(Matrix3d.Displacement((pline.GetFirstDerivative(acAttDef.AlignmentPoint).GetPerpendicularVector()).MultiplyBy(acAttDef.Height * 0.1)));

            return acAttDef;
        }


        public class ArrowDirectional2
        {
            private double defLength = 3d;
            private double defArrowBlug = 0.4d;
            private double defArrowLength = 1.5d;
            private double defSpaceLength = 1d;
            private string anchorTag = "Отклонение";
            private Matrix3d _blockTransform;
            private List<Entity> _entities;

            public Matrix3d UcsMatrix { get; private set; }
            public string BlockName { get; private set; }
            public Point3d Origin { get; private set; }
            public ObjectId BlockTableRecordId { get; private set; }
            public DirectionMode Mode { get; private set; }
            public Polyline ArrowLine { get; private set; }
            public AttributeDefinition BaseAttribute { get; private set; }
            public Helpers.Display.DynamicTransient DynTransient { get; private set; }
            public ObjectId ArrowId { get; private set; }

            public ArrowDirectional2(Matrix3d matrix, DirectionMode mode, double angle = 0d)
                : base()
            {
                this.BlockTableRecordId = ObjectId.Null;
                this.Origin = new Point3d(0, 0, 0);
                this.UcsMatrix = matrix;
                this.Mode = mode;
                this.BlockName = "_DrawAnchorDeviations_" + this.Mode.ToString();
                this.DynTransient = new Helpers.Display.DynamicTransient();
                _entities = new List<Entity>();
                _blockTransform = Matrix3d.Identity;
            }

            private Polyline _createPLine(Point3d origin)
            {
                Polyline pline = new Polyline(3);
                pline.AddVertexAt(0, origin, 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + (defLength), pline.GetPoint2dAt(0).Y), 0, defArrowBlug, 0);
                pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + (defArrowLength), pline.GetPoint2dAt(1).Y), 0, 0, 0);
                pline.LineWeight = LineWeight.LineWeight020;

                if (Mode == DirectionMode.Vertical)
                {
                    Matrix3d mat = Matrix3d.Rotation(Math.PI / 2, new Vector3d(0, 0, 1), this.Origin);
                    pline.TransformBy(mat);
                }

                _entities.Add(pline);
                
                return pline;
            }

            private AttributeDefinition _createAttribute(Point3d position)
            {
                AttributeDefinition acAttDef = new AttributeDefinition();

                acAttDef.Verifiable = true;
                acAttDef.Height = 1.8;
                //acAttDef.Justify = AttachmentPoint.BaseMid;
                acAttDef.Prompt = anchorTag;
                acAttDef.Tag = anchorTag;
                acAttDef.TextString = "0";

                /*if (Mode == DirectionMode.Horizontal)
                {
                    acAttDef.HorizontalMode = TextHorizontalMode.TextCenter;
                    acAttDef.VerticalMode = TextVerticalMode.TextBase;
                }
                else
                {
                    acAttDef.HorizontalMode = TextHorizontalMode.TextRight;
                    acAttDef.VerticalMode = TextVerticalMode.TextVerticalMid;
                }
                acAttDef.Position = position;
                acAttDef.AlignmentPoint = acAttDef.Position.TransformBy(Matrix3d.Displacement(ArrowLine.GetPointAtDist(defLength / 2d) - acAttDef.Position));
                acAttDef.AlignmentPoint = acAttDef.AlignmentPoint.TransformBy(Matrix3d.Displacement((ArrowLine.GetFirstDerivative(acAttDef.AlignmentPoint).GetPerpendicularVector()).MultiplyBy(acAttDef.Height * 0.1)));*/

                acAttDef.Position = position;
                _appenedAttriuteDef(ref acAttDef);

                this._entities.Add(acAttDef);

                return acAttDef;
            }

            private ObjectId _createBlockRecord()
            {
                this._eraseBolckTableRecord();

                this.ArrowLine = _createPLine(new Point3d(this.Origin.X + defSpaceLength, this.Origin.Y, this.Origin.Z));
                this.BaseAttribute = _createAttribute(this.Origin);

                //List<Entity> ents = new List<Entity>(new[] { ArrowLine, (Entity)this.BaseAttribute });

                this.BlockTableRecordId = BlockTools.CreateBlockTableRecordEx(Point3d.Origin, "*U", _entities, AnnotativeStates.True);

                return this.BlockTableRecordId;
            }


            public ArrowDirectional2 PastNewArrowItem(Point3d position, Point3d valuePoint)
            {
                _createBlockRecord();

                double value = Mode == DirectionMode.Horizontal ?
                    _calcValue(position, valuePoint).X :
                    _calcValue(position, valuePoint).Y;

                if (value < 0)
                {
                    _mirror();
                }

                ObjectId brId = BlockTools.AddBlockRefToModelSpace(this.BlockTableRecordId,
                    new List<string>(new[] { Math.Abs(Math.Round(value * 1000d, 0)).ToString() }), position, UcsMatrix);

                /*using (Transaction trans = Tools.StartTransaction())
                {
                    if (value < 0)
                    {
                        BlockTableRecord btr = this.BlockTableRecordId.GetObjectForRead<BlockTableRecord>();
                        btr.UpgradeOpen();
                        //btr.UpdateAnonymousBlocks();

                        _arrowBlockRef = (BlockReference)trans.GetObject(brId, OpenMode.ForRead);
                        _arrowBlockRef.UpgradeOpen();
                        _mirror();
                        _arrowBlockRef.RecordGraphicsModified(true);
  

                        if (_arrowBlockRef.AttributeCollection != null)
                        {
                            foreach (ObjectId arId in _arrowBlockRef.AttributeCollection)
                            {
                                AttributeReference ar = (AttributeReference)arId.GetObject(OpenMode.ForRead, false, true);
                                ar.UpgradeOpen();
                                ar.RecordGraphicsModified(true);
                            }
                        }
                    }
                }*/


                Tools.GetActiveAcadDocument().TransactionManager.FlushGraphics();
                this.ArrowId = brId;

                return this;
            }

            private Vector3d _calcValue(Point3d nomPoint, Point3d factPoint)
            {
                nomPoint.TransformBy(this.UcsMatrix);
                factPoint.TransformBy(this.UcsMatrix);

                Vector3d res = factPoint - nomPoint;
                return res;
            }

            private void _mirror()
            {
                Matrix3d mat;
                if (Mode == DirectionMode.Horizontal)
                    mat = Matrix3d.Mirroring(new Plane(this.Origin, this.UcsMatrix.CoordinateSystem3d.Yaxis, this.UcsMatrix.CoordinateSystem3d.Zaxis));
                else
                    mat = Matrix3d.Mirroring(new Plane(this.Origin, this.UcsMatrix.CoordinateSystem3d.Xaxis, this.UcsMatrix.CoordinateSystem3d.Zaxis));
                _transformBy(mat);
            }


            public BlockReference GetRedirectBlockReferenceCopy(BlockReference br, Point3d directionPoint)
            {
                using (Transaction trans = Tools.StartTransaction())
                {
                    br = (BlockReference)br.Id.GetObject(OpenMode.ForRead, true, true);
                    double ang = 0d;
                    if (Mode == DirectionMode.Horizontal)
                        ang = br.BlockTransform.CoordinateSystem3d.Xaxis.GetAngleTo(br.Position.GetVectorTo(directionPoint));
                    else
                        ang = br.BlockTransform.CoordinateSystem3d.Yaxis.GetAngleTo(br.Position.GetVectorTo(directionPoint));

                    Polyline arrow = (Polyline)this.ArrowLine.Clone();
                    arrow.AddVertexAt(0, this.Origin);
                    arrow.AddVertexAt(4, new Point3d(
                        arrow.EndPoint.X + arrow.GetFirstDerivative(0d).X * defSpaceLength,
                        arrow.EndPoint.Y + arrow.GetFirstDerivative(0d).Y * defSpaceLength,
                        arrow.EndPoint.Z));
                    arrow.TransformBy(br.BlockTransform);


                    if (Math.Abs(ang) > Math.PI / 2d)
                    {
                        Matrix3d mat;
                        if (this.Mode == DirectionMode.Horizontal)
                        {
                            Vector3d vector;
                            if (br.BlockTransform.CoordinateSystem3d.Xaxis.X >= 0)
                                vector = br.Position - arrow.EndPoint;
                            else
                                vector = arrow.EndPoint - br.Position;
                            mat = Matrix3d.Displacement(vector.MultiplyBy(br.BlockTransform.CoordinateSystem3d.Xaxis.X));
                            _transformBy(mat);
                        }
                        else
                        {
                            Vector3d vector;
                            if (br.BlockTransform.CoordinateSystem3d.Yaxis.Y >= 0)
                                vector = br.Position - arrow.EndPoint;
                            else
                                vector = arrow.EndPoint - br.Position;
                            mat = Matrix3d.Displacement(vector.MultiplyBy(br.BlockTransform.CoordinateSystem3d.Yaxis.Y));
                            _transformBy(mat);
                        }

                        return (BlockReference)br.Clone();
                    }
                    else
                    {
                        return null;
                    }

                }
            }

            public enum DirectionMode
            {
                Horizontal = 0,
                Vertical = 1
            }

            private void _transformBy(Matrix3d matrix, Transaction trans, bool commit)
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity ent = _entities[i];
                    if (ent.ObjectId != ObjectId.Null)
                    {
                        ent = (Entity)ent.Id.GetObject(OpenMode.ForWrite, false, true);
                    }
                    
                    ent.TransformBy(matrix);
                    if (ent is AttributeDefinition)
                    {
                        try
                        {
                            AttributeDefinition ad = (AttributeDefinition)ent;
                            //_appenedAttriuteDef(ref ad);
                            ad.AdjustAlignment(Tools.GetAcadDatabase());
                        }
                        catch { }
                    }
                }
                if (commit)
                {
                    /*BlockTableRecord btr = null;
                    if (this.BlockTableRecordId != ObjectId.Null)
                    {
                        btr = (BlockTableRecord)this.BlockTableRecordId.GetObject(OpenMode.ForRead, false, true);
                    }*/
                    trans.Commit();

                    /*if (btr != null)
                    {
                        btr.UpdateAnonymousBlocks();
                    }*/
                }
            }

            private void _transformBy(Matrix3d matrix)
            {
                using (Transaction trans = Tools.StartTransaction())
                {
                    _transformBy(matrix, trans, true);
                }
            }

            private Matrix3d _fromUcskToBlock()
            {
                Matrix3d mat = _blockTransform.PreMultiplyBy(this.UcsMatrix);
                return mat;
            }

            private Matrix3d _fromBlockToUcs()
            {
                Matrix3d mat = this.UcsMatrix.PreMultiplyBy(_blockTransform);
                return mat;
            }

            private void _eraseBolckTableRecord()
            {
                using (Transaction trans = Tools.StartTransaction())
                {
                    if (this.BlockTableRecordId != ObjectId.Null)
                    {
                        BlockTableRecord btr = (BlockTableRecord)this.BlockTableRecordId.GetObject(OpenMode.ForRead, true, true);
                        if (!btr.IsErased)
                        {
                            btr.UpgradeOpen();
                            using (BlockTableRecordEnumerator enumerator = btr.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    enumerator.Current.GetObject((OpenMode)OpenMode.ForWrite).Erase();
                                }
                            }
                            trans.Commit();
                            _entities = new List<Entity>();
                        }
                    }
                }
            }

            private void _appenedAttriuteDef(ref AttributeDefinition acAttDef)
            {
                if (Mode == DirectionMode.Horizontal)
                {
                    acAttDef.HorizontalMode = TextHorizontalMode.TextCenter;
                    acAttDef.VerticalMode = TextVerticalMode.TextBase;
                }
                else
                {
                    acAttDef.HorizontalMode = TextHorizontalMode.TextRight;
                    acAttDef.VerticalMode = TextVerticalMode.TextVerticalMid;
                }
                acAttDef.AlignmentPoint = acAttDef.Position.TransformBy(Matrix3d.Displacement(ArrowLine.GetPointAtDist(defLength / 2d) - acAttDef.Position));
                acAttDef.AlignmentPoint = acAttDef.AlignmentPoint.TransformBy(Matrix3d.Displacement((ArrowLine.GetFirstDerivative(acAttDef.AlignmentPoint).GetPerpendicularVector()).MultiplyBy(acAttDef.Height * 0.1)));
            }

        }




        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TransTest1", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TransTest1()
        {
            Point3d startPoint = new Point3d(0,0,0);
            double x = startPoint.X + Helpers.Math.Randoms.RandomGen.Next(20);
            double y = startPoint.Y + Helpers.Math.Randoms.RandomGen.Next(20);
            
            Line line = new Line(startPoint, new Point3d(x,y,0d));

            IgorKL.ACAD3.Model.Drawing.TransientGraphicsTools.SelectableTransient st = new Drawing.TransientGraphicsTools.SelectableTransient(new List<Entity>(new[] {line}));
            st.Display();

            System.Threading.Timer timer =new System.Threading.Timer(
                delegate(object state)
                {
                    line.EndPoint = new Point3d(startPoint.X + Helpers.Math.Randoms.RandomGen.Next(20),
                        startPoint.Y + Helpers.Math.Randoms.RandomGen.Next(20), 0);
                    Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager.UpdateTransient(
                      st, new IntegerCollection()
                    );
                });

            timer.Change(1000, 1000);
        }


        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TransTest2", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TransTest2()
        {
            Point3d startPoint = new Point3d(0, 0, 0);
            double x = startPoint.X + Helpers.Math.Randoms.RandomGen.Next(20);
            double y = startPoint.Y + Helpers.Math.Randoms.RandomGen.Next(20);

            Line line = new Line(startPoint, new Point3d(x, y, 0d));

            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            CustomObjects.SimpleEntityOverrideEx block = new CustomObjects.SimpleEntityOverrideEx(new Point3d(0,0,0), AnnotativeStates.True, new[] {(Entity)line}.ToList(), ucs);
            //block.AppendEntity(line);
            BlockReference br = block.GetObject(startPoint, null);
            //Tools.AppendEntity(br);

            System.Threading.Timer timer = new System.Threading.Timer(
                delegate(object state)
                {
                    using (Transaction trans = Tools.StartTransaction())
                    {
                        lock (trans)
                        {
                            line = line.Id.GetObjectForRead<Line>(false);
                            /*line.UpgradeOpen();
                            line.EndPoint = new Point3d(startPoint.X + Helpers.Math.Randoms.RandomGen.Next(20),
                                startPoint.Y + Helpers.Math.Randoms.RandomGen.Next(20), 0);
                            block.Update();*/
                            Matrix3d mat = Matrix3d.Displacement(new Point3d(startPoint.X + Helpers.Math.Randoms.RandomGen.Next(20),
                                startPoint.Y + Helpers.Math.Randoms.RandomGen.Next(20), 0) - line.StartPoint);
                            block.AppendEntity(line.GetTransformedCopy(mat));
                            //trans.Commit();
                        }
                    }
                });

            timer.Change(1000, 2000);

            block.Update();
        }
#endif

    }
}
