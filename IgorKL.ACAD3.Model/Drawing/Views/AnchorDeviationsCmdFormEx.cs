using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using IgorKL.ACAD3.Model.AcadBlocks;
using display = IgorKL.ACAD3.Model.Helpers.Display;
using acadApp = Autodesk.AutoCAD.ApplicationServices;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing.Views
{
    public partial class AnchorDeviationsCmdFormEx : Form
    {
        public AnchorDeviationsCmdFormEx()
        {
            InitializeComponent();
            //this.ucsTemp = Tools.GetActiveAcadDocument().Editor.CurrentUserCoordinateSystem;
            this.ucs = Tools.GetActiveAcadDocument().Editor.CurrentUserCoordinateSystem;
            //this.plane = new Plane();
            angle = 0d;
        }

        //Matrix3d ucsTemp;
        Matrix3d ucs;
        //Plane plane;
        double angle;

        ArrowDirectional arrowVertical;
        ArrowDirectional arrowHorizontal;
        ObjectId arrowVerticalId;
        ObjectId arrowHorizontalId;
        display.DynamicTransient transient;

        private void addOneItem_button_Click(object sender, EventArgs e)
        {
            this.Hide();
            /*while (DrawAnchorDeviations())
            {
                Tools.GetActiveAcadDocument().Editor.UpdateScreen();
            }*/

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите проектное положение");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d pPoint = ppr.Value;

            ppo = new PromptPointOptions("\nУкажите фактическое положение");
            ppo.BasePoint = pPoint;
            ppo.UseBasePoint = true;
            ppo.UseDashedLine = true;

            ppr = Tools.GetAcadEditor().GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d fPoint = new Point3d(ppr.Value.X, ppr.Value.Y, pPoint.Z);

            var ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            arrowVertical = new ArrowDirectional(ucs, ArrowDirectional.DirectionMode.Vertical);
            arrowHorizontal = new ArrowDirectional(ucs, ArrowDirectional.DirectionMode.Horizontal);
            
            arrowHorizontal.CreateOrGetBlockRecord();
            arrowHorizontalId = arrowHorizontal.AppendBlock(pPoint, fPoint);
            //arrowHorizontalId = arrowHorizontal.DisplayArrow(pPoint, fPoint, arrowHorizontal.BlockName);

            arrowVertical.CreateOrGetBlockRecord();
            arrowVerticalId = arrowVertical.AppendBlock(pPoint, fPoint);
            //arrowVerticalId = arrowVertical.DisplayArrow(pPoint, fPoint, arrowHorizontal.BlockName);

            ppo = new PromptPointOptions("\nУкажите точку определяющую положение стрелок");
            ppo.UseBasePoint = true;
            ppo.BasePoint = pPoint;
            ppo.UseDashedLine = false;

            Tools.GetAcadEditor().PromptingForPoint += AnchorDeviationsCmdForm_PromptingForPoint;
            Tools.GetAcadEditor().PromptedForPoint += AnchorDeviationsCmdForm_PromptedForPoint;    

            ppr = Tools.GetAcadEditor().GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return;

            this.Show();
        }

        void AnchorDeviationsCmdForm_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            //e.AppendToolTipText("Игорь - это проверка)))");
            BlockReference hBr;
            BlockReference vBr;
            using (Transaction trans = Tools.StartTransaction())
            {
                hBr = (BlockReference)arrowHorizontalId.GetObject(OpenMode.ForRead, true, true);
                vBr = (BlockReference)arrowVerticalId.GetObject(OpenMode.ForRead, true, true);
            }
            BlockReference copyHBr = this.arrowHorizontal.GetRedirectBlockReferenceCopy(hBr, e.Context.ComputedPoint);
            BlockReference copyVBr = this.arrowVertical.GetRedirectBlockReferenceCopy(vBr, e.Context.ComputedPoint);

            if (transient == null)
                transient = new display.DynamicTransient();
            transient.ClearTransientGraphics();

            if (copyHBr != null)
                transient.AddMarker(copyHBr, arrowHorizontal.BlockName);
            if (copyVBr != null)
                transient.AddMarker(copyVBr, arrowVertical.BlockName);

            transient.Display();

        }

        void AnchorDeviationsCmdForm_PromptedForPoint(object sender, PromptPointResultEventArgs e)
        {
            Tools.GetAcadEditor().PointMonitor -= AnchorDeviationsCmdForm_PointMonitor;
            Tools.GetAcadEditor().PromptingForPoint -= AnchorDeviationsCmdForm_PromptingForPoint;
            Tools.GetAcadEditor().PromptedForPoint -= AnchorDeviationsCmdForm_PromptedForPoint;  
            if (transient != null)
            {
                /*List<DBObject> markers = transient.GetClonedMarkers();
                using (Transaction trans = Tools.StartTransaction())
                {
                    foreach (BlockReference br in markers)
                    {

                        if (br.Name == this.arrowHorizontal.BlockName)
                        {
                            trans.GetObject(this.arrowHorizontalId, OpenMode.ForWrite).Erase();
                            Tools.AppendEntity(trans, br, false);
                        }
                        if (br.Name == this.arrowVertical.BlockName)
                        {
                            trans.GetObject(this.arrowVerticalId, OpenMode.ForWrite).Erase();
                            Tools.AppendEntity(trans, br, false);
                        }
                    }
                    trans.Commit();
                }*/



                using (Transaction trans = Tools.StartTransaction())
                {
                    var brs = transient.FindAllAtTag<BlockReference>(this.arrowHorizontal.BlockName);
                    foreach (var br in brs)
                    {
                        trans.GetObject(this.arrowHorizontalId, OpenMode.ForWrite).Erase();
                        Tools.AppendEntityEx(trans, (BlockReference)br.Clone(), false);
                        //br.GetAnonymClone(br.Position);
                    }

                    brs = transient.FindAllAtTag<BlockReference>(this.arrowVertical.BlockName);
                    foreach (var br in brs)
                    {
                        trans.GetObject(this.arrowVerticalId, OpenMode.ForWrite).Erase();
                        Tools.AppendEntityEx(trans, (BlockReference)br.Clone(), false);
                    }

                    trans.Commit();
                }

                transient.Dispose();
                transient = null;


            }
        }

        void AnchorDeviationsCmdForm_PromptingForPoint(object sender, PromptPointOptionsEventArgs e)
        {
            Tools.GetAcadEditor().PointMonitor += AnchorDeviationsCmdForm_PointMonitor;
        }

        public bool DrawAnchorDeviations()
        {
            Polyline pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(3, 0), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(4, 0), 0, 0.4, 0);
            pline.AddVertexAt(3, new Point2d(5.5, 0), 0, 0.4, 0);
            pline.LineWeight = LineWeight.LineWeight015;

            AttributeDefinition acAttDef = new AttributeDefinition();

            acAttDef.Verifiable = true;
            acAttDef.Height = 1.8;
            //acAttDef.Justify = AttachmentPoint.BaseMid;
            acAttDef.Prompt = "Deviation #: ";
            acAttDef.Tag = "Deviation#";
            acAttDef.TextString = "0";
            acAttDef.Position = new Point3d((pline.Length) / 2d - acAttDef.Height / 2d, acAttDef.Height * 0.1, 0);

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите проектное положение");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return false;
            Point3d pPoint = ppr.Value;

            ppo = new PromptPointOptions("\nУкажите фактическое положение");
            ppo.BasePoint = pPoint;
            ppo.UseBasePoint = true;
            ppo.UseDashedLine = true;

            ppr = Tools.GetAcadEditor().GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return false;
            Point3d fPoint = new Point3d(ppr.Value.X, ppr.Value.Y, pPoint.Z);

            List<Entity> ents = new List<Entity>(new[] { pline, (Entity)acAttDef });

            var btrId = BlockTools.CreateBlockTableRecordEx(Point3d.Origin, "_DrawAnchorDeviations", ents, AnnotativeStates.True);

            if (btrId != ObjectId.Null)
            {
                fPoint = fPoint.RotateBy(-angle, ucs.CoordinateSystem3d.Zaxis, pPoint);

                var brHorizontalId = BlockTools.AddBlockRefToModelSpace(btrId, new[] { Math.Abs(Math.Round((fPoint.X - pPoint.X) * 1000d, 0)).ToString() }.ToList(), pPoint, ucs);
                var brVerticalId = BlockTools.AddBlockRefToModelSpace(btrId, new[] { Math.Abs(Math.Round((fPoint.Y - pPoint.Y) * 1000d, 0)).ToString() }.ToList(), pPoint, ucs);
                using (Transaction trans = Tools.StartTransaction())
                {
                    BlockReference br1 = (BlockReference)trans.GetObject(brHorizontalId, OpenMode.ForWrite);
                    BlockReference br2 = (BlockReference)trans.GetObject(brVerticalId, OpenMode.ForWrite);

                    const string MIRRTEXT = "MIRRTEXT";
                    object defVal = acadApp.Application.GetSystemVariable(MIRRTEXT);
                    acadApp.Application.SetSystemVariable(MIRRTEXT, 0);

                    try
                    {
                        if (fPoint.X - pPoint.X < 0d)
                        {
                            BlockTools.MirroringBlockByYAxis(br1);
                        }

                        br2.TransformBy(Matrix3d.Rotation(Math.PI / 2, ucs.CoordinateSystem3d.Zaxis, br2.Position));
                        if (fPoint.X - pPoint.X < 0d)
                            BlockTools.MirroringBlockByYAxis(br2);
                        if (fPoint.Y - pPoint.Y < 0)
                        {
                            BlockTools.MirroringBlockByXAxis(br2);
                            BlockTools.MirroringBlockByYAxis(br2);
                        }

                        br1.TransformBy(Matrix3d.Rotation(angle, ucs.CoordinateSystem3d.Zaxis, br1.Position));
                        br2.TransformBy(Matrix3d.Rotation(angle, ucs.CoordinateSystem3d.Zaxis, br2.Position));

                        using (TransientGraphicsTools.SelectableTransient _transient =
                            new TransientGraphicsTools.SelectableTransient(new List<Entity>(new[] { br1, br2 })))
                        {
                            _transient.Display();

                            ppo = new PromptPointOptions("\nУкажите точку определяющую сторону отобажения");
                            ppo.UseBasePoint = true;
                            ppo.BasePoint = pPoint;
                            ppo.UseDashedLine = true;

                            ppr = Tools.GetAcadEditor().GetPoint(ppo);

                            if (ppr.Status == PromptStatus.OK)
                            {
                                Point3d point = ppr.Value;

                                Polyline transPline = null;

                                if (pline.Id != ObjectId.Null)
                                    pline = (Polyline)trans.GetObject(pline.Id, OpenMode.ForRead);
                                transPline = (Polyline)pline.Clone();

                                transPline.TransformBy(br1.BlockTransform);
                                double ang = CoordinateGeometry.Helper.GetAngle(transPline.StartPoint, transPline.EndPoint, point);
                                if (Math.Abs(ang) > Math.PI/2d)
                                {
                                    Matrix3d mat = Matrix3d.Displacement(transPline.StartPoint - transPline.EndPoint);
                                    br1.TransformBy(mat);
                                }

                                transPline = (Polyline)pline.Clone();

                                transPline.TransformBy(br2.BlockTransform);
                                ang = CoordinateGeometry.Helper.GetAngle(transPline.StartPoint, transPline.EndPoint, point);
                                if (Math.Abs(ang) > Math.PI / 2d)
                                {
                                    Matrix3d mat = Matrix3d.Displacement(transPline.StartPoint - transPline.EndPoint);
                                    br2.TransformBy(mat);
                                }
                            }

                            _transient.StopDisplaying();
                        }

                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        acadApp.Application.SetSystemVariable(MIRRTEXT, defVal);
                    }
                }
            }
            return false;
        }

        private void getYAxis_button_Click(object sender, EventArgs e)
        {
            this.Hide();

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите первую точку определяющую ось Y");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d firstYAxisPoint = ppr.Value;

            ppo = new PromptPointOptions("\nУкажите вторую точку определяющую ось Y");
            ppo.UseBasePoint = true;
            ppo.BasePoint = firstYAxisPoint;
            ppo.UseDashedLine = true;

            ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d secondYAxisPoint = ppr.Value;

            angle = CoordinateSystem.CoordinateTools.GetAngleFromUcsYAxis(secondYAxisPoint - firstYAxisPoint);

            Tools.GetActiveAcadDocument().Editor.UpdateScreen();

            this.Show();
        }

        public class ArrowDirectional
        {
            private double defLength = 3d;
            private double defArrowBlug = 0.4d;
            private double defArrowLength = 1.5d;
            private double defSpaceLength = 1d;
            private string anchorTag = "Отклонение";

            public Matrix3d Matrix {get; private set;}
            public string BlockName { get; private set; }
            public Point3d Origin { get; private set; }
            public ObjectId BlockTableRecordId { get; private set; }
            public DirectionMode Mode { get; private set; }
            public double SpecifyAngle { get; private set; }
            public Polyline ArrowLine { get; private set; }
            public AttributeDefinition BaseAttribute { get; private set; }
            public display.DynamicTransient DynTransient { get; private set; }

            public ArrowDirectional(Matrix3d matrix, DirectionMode mode, double angle = 0d)
            {
                this.BlockTableRecordId = ObjectId.Null;
                this.Origin = new Point3d(0, 0, 0);
                this.Matrix = matrix;
                this.Mode = mode;
                this.BlockName = "_DrawAnchorDeviations_" + this.Mode.ToString();
                this.DynTransient = new display.DynamicTransient();
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
                    Matrix3d mat = Matrix3d.Rotation(Math.PI/2, new Vector3d(0,0,1), this.Origin);
                    pline.TransformBy(mat);
                }

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
                //acAttDef.HorizontalMode = TextHorizontalMode.TextCenter;
                //acAttDef.VerticalMode = TextVerticalMode.TextBase;
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
                acAttDef.Position = position;
                acAttDef.AlignmentPoint = acAttDef.Position.TransformBy(Matrix3d.Displacement(ArrowLine.GetPointAtDist(defLength / 2d) - acAttDef.Position));
                acAttDef.AlignmentPoint = acAttDef.AlignmentPoint.TransformBy(Matrix3d.Displacement((ArrowLine.GetFirstDerivative(acAttDef.AlignmentPoint).GetPerpendicularVector()).MultiplyBy(acAttDef.Height * 0.1)));


                return acAttDef;
            }

            public ObjectId DisplayArrow(Point3d position, Point3d valuePoint, string tag, Autodesk.AutoCAD.GraphicsInterface.TransientDrawingMode viewMod = Autodesk.AutoCAD.GraphicsInterface.TransientDrawingMode.DirectShortTerm)
            {
                ObjectId brId = AppendBlock(position, valuePoint);
                BlockReference br;
                using (Transaction trans = Tools.StartTransaction())
                {
                    br = (BlockReference)brId.GetObject(OpenMode.ForWrite, false, true);
                    br.Erase(true);
                    trans.Commit();
                }
                using (Transaction trans = Tools.StartTransaction())
                {
                    ObjectId btrId = BlockTools.GetAnonymCopy(brId, trans, false);
                    brId = BlockTools.AddBlockRefToModelSpace(btrId, new[] {"N"}.ToList(), br.Position, this.Matrix);
                    trans.Commit();
                }

                using (Transaction trans = Tools.StartTransaction())
                {
                    br = (BlockReference)brId.GetObject(OpenMode.ForWrite, false, true);
                    br.Erase(true);
                    trans.Commit();
                }

                using (Transaction trans = Tools.StartTransaction())
                {
                    br = (BlockReference)brId.GetObject(OpenMode.ForWrite, true, true);
                    this.DynTransient.ClearTransientGraphics();
                    this.DynTransient.AddMarker((DBObject)br, tag, viewMod);
                    this.DynTransient.Display();
                }

                return brId;
            }

            public BlockReference StopDysplaying()
            {
                var res = new List<BlockReference>(this.DynTransient.GetClonedMarkers().Cast<BlockReference>());
                this.DynTransient.ClearTransientGraphics();
                if (res.Count > 0)
                    return res.First();
                else
                    return null;
            }

            public ObjectId CreateOrGetBlockRecord()
            {
                this.ArrowLine = _createPLine(new Point3d(this.Origin.X + defSpaceLength, this.Origin.Y, this.Origin.Z));
                this.BaseAttribute = _createAttribute(this.Origin);


                List<Entity> ents = new List<Entity>(new[] { ArrowLine, (Entity)this.BaseAttribute });

                this.BlockTableRecordId = BlockTools.CreateBlockTableRecordEx(Point3d.Origin, "*U", ents, AnnotativeStates.True);

                return this.BlockTableRecordId;
            }

            [Obsolete]
            public ObjectId CreateOrGetBlockRecordEx()
            {
                if (this.BlockTableRecordId == ObjectId.Null)
                {
                    this.ArrowLine = _createPLine(new Point3d(this.Origin.X +defSpaceLength, this.Origin.Y, this.Origin.Z));
                    this.BaseAttribute = _createAttribute(this.Origin);


                    List<Entity> ents = new List<Entity>(new[] { ArrowLine, (Entity)this.BaseAttribute });

                    this.BlockTableRecordId = BlockTools.CreateBlockTableRecordEx(Point3d.Origin, this.BlockName, ents, AnnotativeStates.True);
                }

                return this.BlockTableRecordId;
            }

            [Obsolete]
            public ObjectId AppendBlockEx(Point3d position, Point3d valuePoint)
            {
                Matrix3d rot = Matrix3d.Rotation(this.SpecifyAngle, this.Matrix.CoordinateSystem3d.Zaxis, position);
                if (this.SpecifyAngle != 0d)
                {
                    position = position.TransformBy(rot);
                    valuePoint = valuePoint.TransformBy(rot);
                }

                double value = Mode == DirectionMode.Horizontal ?
                    _calcValue(position, valuePoint).X :
                    _calcValue(position, valuePoint).Y;

                ObjectId brId = BlockTools.AddBlockRefToModelSpace(this.BlockTableRecordId, 
                    new List<string>(new[] { Math.Abs(Math.Round(value*1000d, 0)).ToString() }), position, Matrix);
                
                BlockReference br;
                
                using (Transaction trans = Tools.StartTransaction())
                {
                    br = (BlockReference)trans.GetObject(brId, OpenMode.ForRead);
                    br.UpgradeOpen();
                    if (value < 0)
                        Mirror(br);
                    if (this.SpecifyAngle != 0d)
                        br.TransformBy(rot);
                    trans.Commit();
                }

                /*using (Transaction trans = Tools.StartTransaction())
                {
                    ObjectId anyBtrId = BlockTools.GetAnonymCopy(br.Id, trans, false);
                    AttributeReference ar = br.GetAttributeByTag(this.anchorTag, trans);
                    br = (BlockReference)trans.GetObject(brId, OpenMode.ForWrite);
                    br.Erase(true);
                    brId = BlockTools.AddBlockRefToModelSpace(anyBtrId, new[] { ar.TextString }.ToList(), br.Position, Matrix, trans, false);
                    trans.Commit();
                }*/

                return brId;
            }

            
            public ObjectId AppendBlock(Point3d position, Point3d valuePoint)
            {
                Matrix3d rot = Matrix3d.Rotation(this.SpecifyAngle, this.Matrix.CoordinateSystem3d.Zaxis, position);
                if (this.SpecifyAngle != 0d)
                {
                    position = position.TransformBy(rot);
                    valuePoint = valuePoint.TransformBy(rot);
                }

                double value = Mode == DirectionMode.Horizontal ?
                    _calcValue(position, valuePoint).X :
                    _calcValue(position, valuePoint).Y;

                ObjectId brId = BlockTools.AddBlockRefToModelSpace(this.BlockTableRecordId,
                    new List<string>(new[] { Math.Abs(Math.Round(value * 1000d, 0)).ToString() }), position, Matrix);
                using (Transaction trans = Tools.StartTransaction())
                {
                    BlockReference br = (BlockReference)trans.GetObject(brId, OpenMode.ForRead);
                    br.UpgradeOpen();
                    if (value < 0)
                        Mirror(br);
                    if (this.SpecifyAngle != 0d)
                        br.TransformBy(rot);
                    trans.Commit();
                }
                return brId;
            }



            private Vector3d _calcValue(Point3d nomPoint, Point3d factPoint)
            {
                nomPoint.TransformBy(this.Matrix);
                factPoint.TransformBy(this.Matrix);

                Vector3d res = factPoint - nomPoint;
                return res;
            }

            public void Mirror (BlockReference br)
            {
                using (Transaction trans = Tools.StartTransaction())
                {
                    Line3d line = new Line3d(this.Matrix.CoordinateSystem3d.Origin, this.Matrix.CoordinateSystem3d.Yaxis);

                    br = (BlockReference)trans.GetObject(br.Id, OpenMode.ForRead);
                    Matrix3d mat = br.BlockTransform;
                    if (Mode == DirectionMode.Horizontal)
                        mat = Matrix3d.Mirroring(new Plane(br.Position, br.BlockTransform.CoordinateSystem3d.Yaxis, br.Normal));
                    else
                        mat = Matrix3d.Mirroring(new Plane(br.Position, br.BlockTransform.CoordinateSystem3d.Xaxis, br.Normal));
                    br.UpgradeOpen();
                    br.TransformBy(mat);
                    trans.Commit();
                }
            }

            public void Mirror(ObjectId brId)
            {
                BlockReference br = null;
                using (Transaction trans = Tools.StartOpenCloseTransaction())
                {
                    br = (BlockReference)trans.GetObject(brId, OpenMode.ForRead);
                }
                Mirror(br);
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


                    if (Math.Abs(ang) > Math.PI/2d)
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
                        }
                        else
                        {
                            Vector3d vector;
                            if (br.BlockTransform.CoordinateSystem3d.Yaxis.Y >= 0)
                                vector = br.Position - arrow.EndPoint;
                            else
                                vector = arrow.EndPoint - br.Position;
                            mat = Matrix3d.Displacement(vector.MultiplyBy(br.BlockTransform.CoordinateSystem3d.Yaxis.Y));
                        }
                        //var anBr = br.GetAnonymClone(br.Position.TransformBy(mat)).GetObjectForWrite<BlockReference>();
                        //br = (BlockReference)anBr.Clone();
                        //anBr.Erase(true);
                        //br.InnerTransform(mat);
                        //return (BlockReference)br.GetTransformedCopy(mat);
                        return (BlockReference)br.InnerTransform2(mat);
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
                Vertical =1
            }

            private double _getArrowBlockLength()
            {
                return (this.ArrowLine.EndPoint - this.Origin).Length;
            }

        }
    }
}
