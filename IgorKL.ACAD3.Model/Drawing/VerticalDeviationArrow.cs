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

namespace IgorKL.ACAD3.Model.Drawing
{
    public class VerticalDeviationArrow:CustomObjects.MultiEntity
    {

        private Point3d _insertPointUcs;
        private Point3d _lowerPointUcs;
        private Point3d _lowerDestPointUcs;
        private Point3d _upperPointUcs;
        private Point3d _upperDestPointUcs;

        private Point3d _jigPoint;
        private bool _lowerComplite;
        private bool _upperComplite;
        private bool _lowerJigComplite;
        private bool _upperJigComplite;

        private Arrow _arrow;

        public VerticalDeviationArrow()
            :base(Point3d.Origin, Point3d.Origin, new List<Entity>(), AnnotativeStates.True, Matrix3d.Identity)
        {

        }

#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawVerticalArrows", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawArrows()
        {
            VerticalDeviationArrow mainMlock = new VerticalDeviationArrow();
            mainMlock._ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите положение знака");

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;

            mainMlock._insertPointUcs = ppr.Value;

            ppo = new PromptPointOptions("\nУкажите проектное положение низа");

            /*ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/

            mainMlock._lowerPointUcs = mainMlock._insertPointUcs; /*ppr.Value;*/

            ppo = new PromptPointOptions("\nУкажите фактическое положение низа");

            /*ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/

            /*_lowerDestPointUcs =*/ /*_insertPointUcs.CreateRandomCirclePoints(1, 0.005).ToArray()[0];*/ /*ppr.Value;*/

            ppo = new PromptPointOptions("\nУкажите проектное положение верха");

            /*ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/

            mainMlock._upperPointUcs = mainMlock._insertPointUcs; /*ppr.Value;*/

            ppo = new PromptPointOptions("\nУкажите фактическое положение верха");

            /*ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/

            mainMlock._upperDestPointUcs = mainMlock._upperPointUcs.CreateRandomCirclePoints(1, 0.020).ToArray()[0]; /*ppr.Value;*/


            mainMlock.JigDraw();

        }
#endif

        public override void Calculate()
        {
            Tools.StartTransaction(() =>
            {
                if (Entities.FirstOrDefault() != null)
                    ((BlockReference)Entities.First()).BlockTableRecord.GetObjectForWrite<BlockTableRecord>().DeepErase(true);
            });
            Entities.ForEach(ent => ent.Dispose());
            Entities.Clear();

            if (!_lowerComplite)
            {
                Matrix3d toLocalCs = Arrow.GetToLocalTransform(_lowerPointUcs, _ucs);
                _arrow = new Arrow();
                _arrow.CalculateLower(_jigPoint.TransformBy(toLocalCs));

                _setEntitiesToBlock(_arrow.Explode(Matrix3d.Identity), true);
            }
            else
            {
                if (!_lowerJigComplite)
                {
                    Matrix3d toLocalCs = Arrow.GetToLocalTransform(_lowerPointUcs, _ucs);
                    _arrow.CalculateJigLower(_jigPoint.TransformBy(toLocalCs));
                    _setEntitiesToBlock(_arrow.Explode(Matrix3d.Identity), true);
                }
            }
        }

        private void _setEntitiesToBlock(IEnumerable<Entity> entities, bool erase)
        {
            Tools.StartTransaction(() =>
            {
                var btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", Point3d.Origin, entities, AnnotativeStates.True);
                //var brId = AcadBlocks.BlockTools.AppendBlockItem(_lowerPointUcs.TransformBy(_ucs.Inverse()), btrId, null, Matrix3d.Identity);
                var brId = AcadBlocks.BlockTools.AppendBlockItem(_lowerPointUcs, btrId, null, Ucs);
                BlockReference br = brId.GetObjectForRead<BlockReference>();
                Entities.Add((Entity)br.Clone());

                if (erase)
                {
                    br.UpgradeOpen();
                    br.Erase();
                }
            });
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите точку определяющую положение");
            ppo.UseBasePoint = true;
            ppo.BasePoint = _insertPointUcs.TransformBy(_ucs);

            ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

            PromptPointResult ppr = prompts.AcquirePoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;
            //Tools.GetAcadEditor().DrawVector(ppo.BasePoint, ppr.Value, 1, true);

            /*if (_position == ppr.Value)
                return SamplerStatus.NoChange;*/

            _jigPoint = ppr.Value.TransformBy(_ucs.Inverse());



            return SamplerStatus.OK;
        }

        public override PromptStatus JigDraw()
        {
            PromptStatus res = PromptStatus.Cancel;
            if ((res = base.JigDraw()) != PromptStatus.OK)
                return PromptStatus.Cancel;
            if (!_lowerComplite)
                _lowerComplite = true;

            if ((res = base.JigDraw()) != PromptStatus.OK)
                return PromptStatus.Cancel;
            if (!_lowerJigComplite)
            {
                _lowerJigComplite = true;
                var ids = Tools.AppendEntity(Entities);
                Entities.Clear();

                var brId = ids.FirstOrDefault();
                if (brId != ObjectId.Null)
                {
                    Tools.StartTransaction(() =>
                        {
                            brId.GetObjectForRead<BlockReference>();
                            var xrecord = CustomObjects.Helpers.XRecordTools.GetSetExtensionDictionaryEntry(brId, "ARROW_JigPosition").GetObjectForRead<Xrecord>();
                            xrecord.UpgradeOpen();
                            
                            //Point3d p = _jigPoint.TransformBy(Arrow.GetToLocalTransform(_lowerPointUcs, _ucs));
                            Point3d p = _jigPoint.TransformBy(_ucs);
                            
                            ResultBuffer rb = new ResultBuffer(
                                    new TypedValue((int)DxfCode.Real, p.X),
                                    new TypedValue((int)DxfCode.Real, p.Y),
                                    new TypedValue((int)DxfCode.Real, p.Z)
                                );
                            xrecord.Append(rb);
                        });
                    
                }
            }

            /*if ((res = base.JigDraw()) != PromptStatus.OK)
                return PromptStatus.Cancel;
            if (!_upperComplite)
                _upperComplite = true;*/

            return res;
        }

        private class Arrow
        {

            private double _textHeight;
            private int _unitScale;
            private int _digitalCount;

            private SArrow _lowerSet;
            private SArrow _upperSet;

            public static Matrix3d GetToLocalTransform(Point3d pointUcs, Matrix3d ucs)
            {
                Matrix3d toLocalCs = ucs.Inverse();
                toLocalCs = toLocalCs.PostMultiplyBy(Matrix3d.Displacement(Point3d.Origin - pointUcs));
                return toLocalCs;
            }

            public Arrow()
            {
                _initDefValues();
            }

            private void _initDefValues()
            {
                _textHeight = 2.0d;
                _unitScale = 1000;
                _digitalCount = 0;
            }


            public void CalculateLower(Point3d destPoint)
            {
                _lowerSet = new SArrow();
                _calculateSet(destPoint, _lowerSet);
            }

            public void CalculateUpper(Point3d destPoint)
            {
                _upperSet = new SArrow();
                _calculateSet(destPoint, _upperSet);
            }

            public void CalculateJigLower(Point3d jigPoint)
            {
                Vector3d vector = jigPoint - Point3d.Origin;
                _lowerSet.RedirectHorizontalArrow(vector);
                _lowerSet.RedirectVerticalArrow(vector);
            }

            public List<Entity> Explode(Matrix3d transform)
            {
                List<Entity> res = new List<Entity>();
                if (_lowerSet != null)
                    res.AddRange(_lowerSet.Entities);
                if (_upperSet != null)
                    res.AddRange(_upperSet.Entities);
                //res.ForEach(ent => ent = ent.GetTransformedCopy(transform));
                res = res.Select(ent => ent.GetTransformedCopy(transform)).ToList();
                return res;
            }

            private void _calculateSet(Point3d destPoint, SArrow arrowSet)
            {
                Vector3d vector = destPoint - Point3d.Origin;

                if (vector.X < 0d && !arrowSet.IsHorizontalMirrored)
                    arrowSet.MirrorHorizontalArrow();
                if (vector.Y < 0d && !arrowSet.IsVerticalMirrored)
                    arrowSet.MirrorVerticalArrow();
            }





            private class SArrow
            {
                private double _length;
                private double _arrowBlug;
                private double _arrowLength;
                private double _spaceLength;

                private Matrix3d _horizontalTarnsform;
                private Matrix3d _verticalTarnsform;


                public SArrow()
                {
                    _length = 3.5d;
                    _arrowBlug = 0.4d;
                    _arrowLength = 1.5d;
                    _spaceLength = 1.0d;

                    HorizontalLine = _createLine();
                    VerticalLine = _createLine();

                    HorizontalLineSymbols = new List<Entity>();
                    VerticalLineSymbols = new List<Entity>();

                    _horizontalTarnsform = Matrix3d.Identity;
                    _verticalTarnsform = Matrix3d.Identity;
                    VerticalLine.TransformBy(new Matrix3d(new double[] { 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }));
                    //VerticalTarnsform = Matrix3d.Identity.PostMultiplyBy(Matrix3d.Rotation(Math.PI/2d, Matrix3d.Identity.CoordinateSystem3d.Zaxis, Point3d.Origin));
                    //VerticalTarnsform = new Matrix3d(new double[] { 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
                    Vector3d v = Matrix3d.Identity.Translation;
                }

                public Polyline HorizontalLine {get; private set;}
                public Polyline VerticalLine { get; private set; }
                public List<Entity> VerticalLineSymbols { get; private set; }
                public List<Entity> HorizontalLineSymbols { get; private set; }
                public Matrix3d HorizontalTarnsform
                {
                    get { return _horizontalTarnsform; }
                    set
                    {
                        HorizontalLine.TransformBy(value);
                        if (HorizontalLineSymbols != null)
                            HorizontalLineSymbols.ForEach(ent => ent.TransformBy(value));
                        _horizontalTarnsform = _horizontalTarnsform.PostMultiplyBy(value);
                    }
                }
                public Matrix3d VerticalTarnsform
                {
                    get { return _verticalTarnsform; }
                    set
                    {
                        VerticalLine.TransformBy(value);
                        if (VerticalLineSymbols != null)
                            VerticalLineSymbols.ForEach(ent => ent.TransformBy(value));
                        _verticalTarnsform = _verticalTarnsform.PostMultiplyBy(value);
                    }
                }
                public bool IsVerticalMirrored { get; private set; }
                public bool IsHorizontalMirrored { get; private set; }
                public bool IsHorizontalRedirected { get; private set; }
                public bool IsVerticalRedirected { get; private set; }
                public double Length
                {
                    get { return _spaceLength + _length + _arrowLength; }
                }
                public IEnumerable<Entity> Entities
                {
                    get
                    {
                        yield return HorizontalLine;
                        yield return VerticalLine;
                    }
                }

                public void AppendHorizontalSymbol(Entity entity)
                {
                    HorizontalLineSymbols.Add(entity.GetTransformedCopy(_horizontalTarnsform));
                }
                public void AppendVerticalSymbol(Entity entity)
                {
                    VerticalLineSymbols.Add(entity.GetTransformedCopy(_verticalTarnsform));
                }

                private Polyline _createLine()
                {
                    Point3d origin = Point3d.Origin;

                    Polyline pline = new Polyline(3);
                    pline.AddVertexAt(0, new Point3d(origin.X + _spaceLength, origin.Y, 0), 0, 0, 0);
                    pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + _length, pline.GetPoint2dAt(0).Y), 0, _arrowBlug, 0);
                    pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + _arrowLength, pline.GetPoint2dAt(1).Y), 0, 0, 0);
                    pline.LineWeight = LineWeight.LineWeight020;

                    return pline;
                }

                private DBText _createHorizontalText(SArrow arrow)
                {
                    DBText text = new DBText();
                    text.SetDatabaseDefaults();

                    text.Height = 2.0d;
                    text.HorizontalMode = TextHorizontalMode.TextCenter;
                    text.VerticalMode = TextVerticalMode.TextBase;
                    text.TextString = "";
                    text.Position = Point3d.Origin;

                    return text;
                }

                public void MirrorHorizontalArrow()
                {
                    Matrix3d mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, 
                        IsHorizontalMirrored ? _horizontalTarnsform.CoordinateSystem3d.Yaxis.Negate(): _horizontalTarnsform.CoordinateSystem3d.Yaxis,
                        _horizontalTarnsform.CoordinateSystem3d.Zaxis));
                    HorizontalTarnsform = mirror;
                    IsHorizontalMirrored = !IsHorizontalMirrored;
                }
                public void MirrorVerticalArrow()
                {
                    Matrix3d mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin,
                        IsVerticalMirrored ? _verticalTarnsform.CoordinateSystem3d.Xaxis.Negate() : _verticalTarnsform.CoordinateSystem3d.Xaxis,
                        _verticalTarnsform.CoordinateSystem3d.Zaxis));
                    VerticalTarnsform = mirror;
                    IsVerticalMirrored = !IsVerticalMirrored;
                }

                private Matrix3d _mirror(Matrix3d mat)
                {
                    CoordinateSystem3d cs = mat.CoordinateSystem3d;
                    Matrix3d res = mat.PreMultiplyBy(Matrix3d.Mirroring(
                        new Plane(cs.Origin, new Vector3d(Math.Abs(Math.Round(cs.Yaxis.X, 6)), Math.Abs(Math.Round(cs.Yaxis.Y, 6)), 
                            Math.Abs(Math.Round(cs.Yaxis.Z, 6))), cs.Zaxis)));
                    return res;
                }

                public void RedirectHorizontalArrow(Vector3d direction)
                {
                    double rd = 1;
                    if (IsHorizontalRedirected)
                        rd *= -1;
                    if (IsHorizontalMirrored)
                        rd *= -1;
                    if (rd * direction.X >= 0d)
                        return;

                    rd = IsHorizontalRedirected ? -1 : 1;

                    Point3d destPoint = Point3d.Origin.Add(_horizontalTarnsform.CoordinateSystem3d.Xaxis.MultiplyBy(-rd * (_spaceLength*2d + _length + _arrowLength) ));
                    Matrix3d mat = Matrix3d.Displacement(destPoint.GetAsVector());
                    HorizontalTarnsform = mat;
                    IsHorizontalRedirected = !IsHorizontalRedirected;
                }
                public void RedirectVerticalArrow(Vector3d direction)
                {
                    double rd = 1;
                    if (IsVerticalRedirected)
                        rd *= -1;
                    if (IsVerticalMirrored)
                        rd *= -1;
                    if (rd * direction.Y >= 0)
                        return;

                    rd = IsVerticalRedirected ? -1 : 1;

                    Point3d destPoint = Point3d.Origin.Add(_verticalTarnsform.CoordinateSystem3d.Yaxis.MultiplyBy(-rd * (_spaceLength*2d + _length + _arrowLength)));
                    Matrix3d mat = Matrix3d.Displacement(destPoint.GetAsVector());
                    VerticalTarnsform = mat;
                    IsVerticalRedirected = !IsVerticalRedirected;
                }
                
            }
        }

    }
}
