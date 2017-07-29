using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using display = IgorKL.ACAD3.Model.Helpers.Display;
using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class OneArrow
    {
        
#if DEBUG
        [RibbonCommandButton("Одна стрелка", "Стрелки")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_DrawOneArrow", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawOneArrow()
        {
            Curve curve = PromptCurve();
            if (curve == null)
                return;
            Polyline pline = null;
            Tools.StartTransaction(() =>
            {
                pline = curve.ConvertToPolyline();
            });
            PromptStatus res = PromptStatus.OK;
            while (res == PromptStatus.OK)
            {
                var pos = PromptPosition();
                //var vec = PromptVector();
                if (!pos.HasValue)
                    return;

                Point3d? normalPoint = pline.GetOrthoNormalPoint(pos.Value, null, false);
                if (normalPoint == null)
                    return;
                Vector3d vec = pline.GetFirstDerivative(normalPoint.Value);

                Arrow arrow = new Arrow(pos.Value, vec, Matrix3d.Identity);

                Tools.StartTransaction(() =>
                {
                    if ((res = arrow.DrawJig()) == PromptStatus.OK)
                    {
                        arrow.SaveToDatabase();
                    }
                });
            }
        }
#endif

        public static Vector3d? PromptVector()
        {
            Curve curve;
            if (!ObjectCollector.TrySelectAllowedClassObject(out curve))
                return null;
            else
                return curve.EndPoint - curve.StartPoint;
        }

        public static Curve PromptCurve()
        {
            Curve curve;
            if (!ObjectCollector.TrySelectAllowedClassObject(out curve))
                return null;
            else
                return curve;
        }

        public static Point3d? PromptPosition(KeywordCollection keywords = null, ObjectCollector.PromptAction promptAction = null)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите проектное положение: ");
            ppo.UseBasePoint = false;
            if (keywords != null)
                foreach (Keyword key in keywords)
                    ppo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);

            var ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status == PromptStatus.Keyword)
            {
                if (promptAction != null)
                {
                    PromptStatus res = promptAction(ppr);
                    if (res != PromptStatus.OK)
                        return null;
                    ppr = Tools.GetAcadEditor().GetPoint(ppo);
                }
            }

            if (ppr.Status != PromptStatus.OK)
                return null;
            else
                return ppr.Value;
        }

        public class Arrow : DrawJig
        {
            private double _length;
            private double _arrowBlug;
            private double _arrowLength;
            private double _spaceLength;
            private double _textHeight;
            private int _unitScale;
            private int _digitalCount;
            private string _numFormat;
            
            private Polyline _pline;
            private DBText _text;
            private Point3d _position;
            private Point3d _destPoint;
            private Point3d _jigPoint;
            private Vector3d _vector;
            private Matrix3d _ucs;
            private bool _pasted;

            List<Entity> _inMemorySet;

            public Arrow(Point3d position, Vector3d vector, Matrix3d ucs)
            {
                _initDefValues();
                _numFormat = "#0" + (_digitalCount > 0 ? "0".TakeWhile((x, i) => i++ < _digitalCount) : "");
                _position = position;
                _vector = vector;
                _ucs = ucs;
                _pasted = false;
                _inMemorySet = new List<Entity>();
            }


            private void _initDefValues()
            {
                _length = 3.5d;
                _arrowBlug = 0.7d;
                _arrowLength = 1.5d;
                _spaceLength = 1.0d;
                _textHeight = 2.0d;
                _unitScale = 1000;
                _digitalCount = 0;
            }

            private void _createPLine(Point3d origin)
            {
                Polyline pline = new Polyline(3);
                pline.AddVertexAt(0, new Point3d(origin.X + _spaceLength, origin.Y, 0), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + _length, pline.GetPoint2dAt(0).Y), 0, _arrowBlug, 0);
                pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + _arrowLength, pline.GetPoint2dAt(1).Y), 0, 0, 0);
                pline.LineWeight = LineWeight.LineWeight020;

                _pline = pline;
            }

            public void Calculate(Point3d destPoint)
            {
                _createPLine(Point3d.Origin);
                _createText();

                if (_destPoint != destPoint)
                    _destPoint = destPoint;

                double ang = Vector3d.YAxis.GetAngle2d(_vector);

                Vector3d vector = destPoint - _position;
                //vector.TransformBy(_ucs.Inverse());

                Matrix3d hMat = Matrix3d.Identity;
                hMat = hMat.PreMultiplyBy(Matrix3d.Rotation(ang, Vector3d.ZAxis, Point3d.Origin));
                vector = vector.TransformBy(hMat.Inverse());

                if (vector.X < 0)
                    //hMat = hMat.PreMultiplyBy(Matrix3d.Mirroring(new Plane(Point3d.Origin, hMat.CoordinateSystem3d.Yaxis, Matrix3d.Identity.CoordinateSystem3d.Zaxis)));
                    hMat = hMat.PreMultiplyBy(Matrix3d.Mirroring(new Plane(Point3d.Origin, _pline.Normal)));

                _text.TransformBy(hMat);
                _text.AdjustAlignment(HostApplicationServices.WorkingDatabase);
                _pline.TransformBy(hMat);

                _text.TextString = Math.Round(Math.Abs(vector.X) * _unitScale, _digitalCount).ToString(_numFormat);

                //_transformThisBy(_ucs.PostMultiplyBy(Matrix3d.Displacement(_position -_origin)));
                _transformThisBy(Matrix3d.Identity.PostMultiplyBy(Matrix3d.Displacement(_position - Point3d.Origin)));
            }

            public void Redirect(Point3d jigPoint)
            {
                if (_jigPoint != jigPoint)
                    _jigPoint = jigPoint;
                
                double ang = Vector3d.YAxis.GetAngle2d(_vector);
                Matrix3d hMat = Matrix3d.Identity;
                Vector3d vector = jigPoint - _position;

                vector = vector.TransformBy(hMat.PreMultiplyBy(Matrix3d.Rotation(-ang, Vector3d.ZAxis, Point3d.Origin)));

                if (vector.X * (_destPoint - _position).X <= 0 && vector.X != 0d)
                    hMat = hMat.PreMultiplyBy(Matrix3d.Displacement(_position - _pline.EndPoint.Add(_pline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength))));
                else
                    hMat = hMat.PreMultiplyBy(Matrix3d.Displacement(_position.Add(_pline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength)) - _pline.StartPoint));



                _pline.TransformBy(hMat);

                _text.TransformBy(hMat);

                _text.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            }

            private void _createText()
            {
                DBText hText = new DBText();
                hText.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
                hText.Annotative = AnnotativeStates.False;
                hText.Height = _textHeight;
                hText.HorizontalMode = TextHorizontalMode.TextCenter;
                hText.VerticalMode = TextVerticalMode.TextBase;
                hText.Position = _pline.StartPoint;
                hText.AlignmentPoint = hText.Position.Add(_pline.GetFirstDerivative(0).Normalize().MultiplyBy(_length / 2d));
                hText.AlignmentPoint = hText.AlignmentPoint.Add(_pline.GetFirstDerivative(0).GetPerpendicularVector().MultiplyBy(hText.Height * 0.1));

                _text = hText;
                _text.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            }

            private void _transformThisBy(Matrix3d transform)
            {
                _pline.TransformBy(transform);
                _text.TransformBy(transform);
                _text.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            }

            protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
            {
                _inMemorySet.Add(_setToBlock(Explode()));
                foreach (var ent in _inMemorySet)
                {
                    draw.Geometry.Draw(ent);
                }

                foreach (var ent in _inMemorySet)
                {
                    /*ent.Dispose();*/
                    var btrId = ((BlockReference)ent).BlockTableRecord;
                    BlockTableRecord btr = btrId.GetObjectForRead<BlockTableRecord>();
                    BlockReference br = ent.Id.GetObjectForRead<BlockReference>();
                    ent.UpgradeOpen();
                    ent.Erase(true);
                    ent.DowngradeOpen();

                    //btr.UpgradeOpen();
                    foreach (var item in btr)
                    {
                        DBObject obj = item.GetObjectForWrite<DBObject>();
                        obj.Erase(true);
                    }
                    //btr.DowngradeOpen();
                }
                _inMemorySet.Clear();
                return true;
            }

            public List<Entity> Explode()
            {
                List<Entity> inMemorySet = new List<Entity>();

                inMemorySet.Add((Entity)_pline.Clone());
                inMemorySet.Add((Entity)_text.Clone());

                return inMemorySet;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                if (!_pasted)
                {
                    JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите фактическое положение");
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = _position;

                    ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                    PromptPointResult ppr = prompts.AcquirePoint(ppo);

                    if (ppr.Status != PromptStatus.OK)
                        return SamplerStatus.Cancel;

                    /*if (_position == ppr.Value)
                        return SamplerStatus.NoChange;*/

                    _destPoint = ppr.Value;

                    Calculate(_destPoint);

                    return SamplerStatus.OK;
                }
                else
                {
                    JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите положение отрисовки");
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = _position;

                    ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                    PromptPointResult ppr = prompts.AcquirePoint(ppo);

                    if (ppr.Status != PromptStatus.OK)
                        return SamplerStatus.Cancel;

                    /*if (_position == ppr.Value)
                        return SamplerStatus.NoChange;*/

                    _jigPoint = ppr.Value;

                    Redirect(_jigPoint);

                    return SamplerStatus.OK;
                }
            }


            public PromptStatus DrawJig()
            {
                _pasted = false;
                PromptResult promptResult = Tools.GetAcadEditor().Drag(this);
                if (promptResult.Status == PromptStatus.OK)
                {
                    _pasted = true;
                    promptResult = Tools.GetAcadEditor().Drag(this);

                }

                return promptResult.Status;
            }

            private AttributeDefinition _convertToAttribute(DBText text)
            {
                AttributeDefinition ad = new AttributeDefinition();
                ad.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
                ad.TextString = text.TextString;
                ad.HorizontalMode = text.HorizontalMode;
                ad.VerticalMode = text.VerticalMode;
                ad.Height = text.Height;
                ad.Annotative = text.Annotative;
                ad.Constant = false;
                ad.Verifiable = true;
                ad.Rotation = text.Rotation;
                ad.Tag = "стрелка_отклонение";
                ad.Prompt = "Отклонение";
                ad.Position = text.Position;
                ad.AlignmentPoint = text.AlignmentPoint;
                ad.AdjustAlignment(HostApplicationServices.WorkingDatabase);

                return ad;
            }

            public void SaveToDatabase()
            {
                List<Entity> entities = new List<Entity>();
                var items = Explode();
                entities.AddRange(items);
                _setToBlock(entities);
            }

            private BlockReference _setToBlock(IEnumerable<Entity> entities)
            {
                List<Entity> ents = new List<Entity>(entities.Count());
                var text = entities.FirstOrDefault(x => x is DBText);
                if (text != null)
                {
                    var attr = _convertToAttribute((DBText)text);
                    foreach (var e in entities)
                    {
                        if (e  != text)
                            ents.Add(e);
                        else
                            ents.Add(attr);
                    }
                }
                else
                {
                    ents.AddRange(entities);
                }


                ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", _position, ents, AnnotativeStates.True);
                ObjectId brId = AcadBlocks.BlockTools.AppendBlockItem(_position, btrId, null);
                //HostApplicationServices.WorkingDatabase.TransactionManager.QueueForGraphicsFlush();
                return brId.GetObjectForRead<BlockReference>();
            }
        }
    }
}
