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
using acadApp = Autodesk.AutoCAD.ApplicationServices;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing.Views
{
    public partial class AnchorDeviationsCmdFormOld : Form
    {
        private Matrix3d _ucs;
        private double _angle;

        public AnchorDeviationsCmdFormOld()
        {
            InitializeComponent();
            this._ucs = Tools.GetActiveAcadDocument().Editor.CurrentUserCoordinateSystem;
            _angle = 0d;
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawAnchorDeviations_Old", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void DrawCmd()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle,this, false);
        }


        private void addOneItem_button_Click(object sender, EventArgs e)
        {
            this.Hide();
            while (DrawAnchorDeviations())
            {
                Tools.GetActiveAcadDocument().Editor.UpdateScreen();
            }
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
                fPoint = fPoint.RotateBy(-_angle, _ucs.CoordinateSystem3d.Zaxis, pPoint);

                var brHorizontalId = BlockTools.AddBlockRefToModelSpace(btrId, new[] { Math.Abs(Math.Round((fPoint.X - pPoint.X) * 1000d, 0)).ToString() }.ToList(), pPoint, _ucs);
                var brVerticalId = BlockTools.AddBlockRefToModelSpace(btrId, new[] { Math.Abs(Math.Round((fPoint.Y - pPoint.Y) * 1000d, 0)).ToString() }.ToList(), pPoint, _ucs);
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

                        br2.TransformBy(Matrix3d.Rotation(Math.PI / 2, _ucs.CoordinateSystem3d.Zaxis, br2.Position));
                        if (fPoint.X - pPoint.X < 0d)
                            BlockTools.MirroringBlockByYAxis(br2);
                        if (fPoint.Y - pPoint.Y < 0)
                        {
                            BlockTools.MirroringBlockByXAxis(br2);
                            BlockTools.MirroringBlockByYAxis(br2);
                        }

                        br1.TransformBy(Matrix3d.Rotation(_angle, _ucs.CoordinateSystem3d.Zaxis, br1.Position));
                        br2.TransformBy(Matrix3d.Rotation(_angle, _ucs.CoordinateSystem3d.Zaxis, br2.Position));

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

            _angle = CoordinateSystem.CoordinateTools.GetAngleFromUcsYAxis(secondYAxisPoint - firstYAxisPoint);

            Tools.GetActiveAcadDocument().Editor.UpdateScreen();

            this.Show();
        }

    }
}
