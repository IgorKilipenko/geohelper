using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing.Helpers {

    public class Arrow : DrawJig {
        private double _length;
        private double _arrowBlug;
        private double _arrowLength;
        private double _spaceLength;
        private double _textHeight;
        private int _unitScale;
        private int _digitalCount;
        private string _numFormat;

        private Polyline _verticalPline;
        private Polyline _horizontPline;
        private DBText _verticalText;
        private DBText _horizontalText;
        private Point3d _origin;
        private Matrix3d _ucs;
        private Point3d _destPoint;
        private Point3d _position;
        private Point3d _jigPoint;
        private bool _pasted;


        private SafeObject _safeObject;

        public Point3d InsertPoint { get { return _position; } }

        public Arrow(Point3d position, Matrix3d ucs, double? arrowBlug = null)
            : base() {
            _initDefValues();
            _arrowBlug = arrowBlug.HasValue && arrowBlug.Value > 0d ? arrowBlug.Value : _arrowBlug;

            _numFormat = "#0" + (_digitalCount > 0 ? "0".TakeWhile((x, i) => i++ < _digitalCount) : "");

            _origin = Point3d.Origin;
            _position = position;
            _ucs = ucs;
            _pasted = false;

            _safeObject = new SafeObject(this);
        }

        private void _initDefValues() {
            _length = 3.5d;
            _arrowBlug = 0.7d;
            _arrowLength = 1.5d;
            _spaceLength = 1.0d;
            _textHeight = 2.0d;
            _unitScale = 1000;
            _digitalCount = 0;
        }

        private void _createPLine(Point3d origin) {
            Polyline pline = new Polyline(3);
            pline.AddVertexAt(0, new Point3d(origin.X + _spaceLength, origin.Y, 0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + _length, pline.GetPoint2dAt(0).Y), 0, _arrowBlug, 0);
            pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + _arrowLength, pline.GetPoint2dAt(1).Y), 0, 0, 0);
            pline.LineWeight = LineWeight.LineWeight020;

            _horizontPline = pline;
            _verticalPline = (Polyline)_horizontPline.GetTransformedCopy(
                Matrix3d.Rotation(Math.PI / 2, Matrix3d.Identity.CoordinateSystem3d.Zaxis, origin));
        }

        public void Calculate(Point3d destPoint) {
            _createPLine(_origin);
            _createText();

            if (_destPoint != destPoint)
                _destPoint = destPoint;

            Vector3d vector = destPoint - _position;

            Matrix3d hMat = Matrix3d.Identity;
            Matrix3d vMat = Matrix3d.Identity;

            if (vector.X < 0)
                hMat = hMat.PreMultiplyBy(Matrix3d.Mirroring(new Plane(_origin, Matrix3d.Identity.CoordinateSystem3d.Yaxis, Matrix3d.Identity.CoordinateSystem3d.Zaxis)));
            if (vector.Y < 0)
                vMat = vMat.PreMultiplyBy(Matrix3d.Mirroring(new Plane(_origin, Matrix3d.Identity.CoordinateSystem3d.Xaxis, Matrix3d.Identity.CoordinateSystem3d.Zaxis)));

            _horizontalText.TransformBy(hMat);
            _horizontalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            _horizontPline.TransformBy(hMat);

            _verticalText.TransformBy(vMat);
            _verticalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            _verticalPline.TransformBy(vMat);

            _horizontalText.TextString = Math.Round(Math.Abs(vector.X) * _unitScale, _digitalCount).ToString(_numFormat);
            _verticalText.TextString = Math.Round(Math.Abs(vector.Y) * _unitScale, _digitalCount).ToString(_numFormat);

            _transformThisBy(Matrix3d.Identity.PostMultiplyBy(Matrix3d.Displacement(_position - _origin)));
        }

        public void Redirect(Point3d jigPoint) {
            if (_jigPoint != jigPoint)
                _jigPoint = jigPoint;

            Matrix3d hMat = Matrix3d.Identity;
            Matrix3d vMat = Matrix3d.Identity;
            Vector3d vector = jigPoint - _position;
            vector.TransformBy(_ucs.Inverse());

            if (vector.X * (_destPoint - _position).X <= 0 && vector.X != 0d)
                hMat = hMat.PreMultiplyBy(Matrix3d.Displacement(_position - _horizontPline.EndPoint.Add(_horizontPline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength))));
            else
                hMat = hMat.PreMultiplyBy(Matrix3d.Displacement(_position.Add(_horizontPline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength)) - _horizontPline.StartPoint));

            if (vector.Y * (_destPoint - _position).Y <= 0d && vector.Y != 0d)
                vMat = vMat.PreMultiplyBy(Matrix3d.Displacement(_position - _verticalPline.EndPoint.Add(_verticalPline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength))));
            else
                vMat = vMat.PreMultiplyBy(Matrix3d.Displacement(_position.Add(_verticalPline.GetFirstDerivative(0).Normalize().MultiplyBy(_spaceLength)) - _verticalPline.StartPoint));



            _horizontPline.TransformBy(hMat);
            _verticalPline.TransformBy(vMat);

            _horizontalText.TransformBy(hMat);
            _verticalText.TransformBy(vMat);

            _horizontalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);
            _verticalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);
        }

        private void _createText() {
            DBText hText = new DBText();
            hText.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
            hText.Annotative = AnnotativeStates.False;
            hText.Height = _textHeight;
            hText.HorizontalMode = TextHorizontalMode.TextCenter;
            hText.VerticalMode = TextVerticalMode.TextBase;
            hText.Position = _horizontPline.StartPoint;
            hText.AlignmentPoint = hText.Position.Add(_horizontPline.GetFirstDerivative(0).Normalize().MultiplyBy(_length / 2d));
            hText.AlignmentPoint = hText.AlignmentPoint.Add(_horizontPline.GetFirstDerivative(0).GetPerpendicularVector().MultiplyBy(hText.Height * 0.1));

            _horizontalText = hText;
            _horizontalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);


            DBText vText = new DBText();
            vText.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
            vText.Annotative = AnnotativeStates.False;
            vText.Height = _textHeight;

            vText.HorizontalMode = TextHorizontalMode.TextCenter;
            vText.VerticalMode = TextVerticalMode.TextBase;
            vText.Rotation = Math.PI / 2d;

            vText.Position = _verticalPline.StartPoint;
            vText.AlignmentPoint = vText.Position.Add(_verticalPline.GetFirstDerivative(0).Normalize().MultiplyBy(_length / 2d));
            vText.AlignmentPoint = vText.AlignmentPoint.Add(_verticalPline.GetFirstDerivative(0).GetPerpendicularVector().MultiplyBy(vText.Height * 0.1));

            _verticalText = vText;
            _verticalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

        }

        private void _transformThisBy(Matrix3d transform) {
            _horizontPline.TransformBy(transform);
            _verticalPline.TransformBy(transform);

            _verticalText.TransformBy(transform);
            _verticalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            _horizontalText.TransformBy(transform);
            _horizontalText.AdjustAlignment(HostApplicationServices.WorkingDatabase);
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw) {
            List<Entity> inMemorySet = Explode();

            foreach (var ent in inMemorySet) {
                draw.Geometry.Draw(ent);
            }

            foreach (var ent in inMemorySet) {
                ent.Dispose();
            }

            inMemorySet.Clear();

            return true;
        }

        public List<Entity> Explode() {
            List<Entity> inMemorySet = new List<Entity>();

            inMemorySet.Add((Entity)_horizontPline.Clone());
            inMemorySet.Add((Entity)_horizontalText.Clone());
            inMemorySet.Add((Entity)_verticalPline.Clone());
            inMemorySet.Add((Entity)_verticalText.Clone());

            return inMemorySet;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts) {
            if (!_pasted) {
                JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите фактическое положение");
                ppo.UseBasePoint = true;
                ppo.BasePoint = _position;

                ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                _destPoint = ppr.Value.TransformBy(_ucs.Inverse());

                Calculate(_destPoint);

                return SamplerStatus.OK;
            } else {
                JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите положение отрисовки");
                ppo.UseBasePoint = true;
                ppo.BasePoint = _position;

                ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                _jigPoint = ppr.Value.TransformBy(_ucs.Inverse());

                Redirect(_jigPoint);

                return SamplerStatus.OK;
            }
        }

        public PromptStatus DrawJig() {
            _pasted = false;
            PromptResult promptResult = Tools.GetAcadEditor().Drag(this);
            if (promptResult.Status == PromptStatus.OK) {
                _pasted = true;
                promptResult = Tools.GetAcadEditor().Drag(this);

            }
            return promptResult.Status;
        }

        public void SerializeTo(Entity ent) {
            _safeObject.SaveToEntity(ent);
        }

        [Serializable]
        public class SafeObject : CustomObjects.Helpers.CustomObjectSerializer {
            private Arrow _arrow;
            public SafeObject(Arrow arrow) {
                _arrow = arrow;
            }

            public static string AppName {
                get { return "ICmdExtensions_AnchorArrow"; }
            }

            public override string ApplicationName {
                get { return AppName; }
            }

            public Arrow Object { get { return _arrow; } }

            [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand,
            Flags = System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
            public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
                info.AddValue("Ucs", _arrow._ucs.ToArray());
                info.AddValue("DestPoint", _arrow._destPoint.ToArray());
                info.AddValue("InsertPoint", _arrow.InsertPoint.ToArray());
                info.AddValue("Position", _arrow._position.ToArray());
                info.AddValue("JigPoint", _arrow._jigPoint.ToArray());
                info.AddValue("HorizontalText", _arrow._horizontalText.TextString);
                info.AddValue("VerticalText", _arrow._verticalText.TextString);
                info.AddValue("HorizontPline", _arrow._horizontPline.GetPoints().ToEnumerable().Select(p => p.ToArray()).ToArray());
                info.AddValue("VerticalPline", _arrow._verticalPline.GetPoints().ToEnumerable().Select(p => p.ToArray()).ToArray());
            }

            protected SafeObject(
          System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
                if (info == null)
                    throw new System.ArgumentNullException("info");

                Matrix3d ucs = new Matrix3d((double[])info.GetValue("Ucs", typeof(double[])));
                Point3d position = new Point3d((double[])info.GetValue("Position", typeof(double[])));
                _arrow = new Arrow(position, ucs);
                _arrow._destPoint = new Point3d((double[])info.GetValue("DestPoint", typeof(double[])));
                _arrow._jigPoint = new Point3d((double[])info.GetValue("JigPoint", typeof(double[])));

                _arrow.Calculate(_arrow._destPoint);
                _arrow.Redirect(_arrow._jigPoint);
            }
        }
    }
}
