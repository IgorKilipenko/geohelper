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

namespace IgorKL.ACAD3.Model.Drawing.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Arrow : CustomObjects.Helpers.CustomObjectSerializer
    {
        private double _length;
        private double _arrowBlug;
        private double _arrowLength;
        private double _spaceLength;

        private Matrix3d _lineTarnsform;

        public Arrow(Vector3d axisVector)
        {
            _length = 4.0d;
            _arrowBlug = 0.4d;
            _arrowLength = 1.5d;
            _spaceLength = 3.0d;

            this.AxisVector = axisVector;

            this.ArrowSymbols = new List<Entity>();

            this.ArrowLine = _createLine();
            _lineTarnsform = Matrix3d.Identity;

            Matrix3d rotation = _getMainRotation();
            this.LineTarnsform = rotation;

            this.BaseArrow = null;
        }
        public Arrow(Arrow baseArrow)
            : this(baseArrow.AxisVector)
        {
            this.BaseArrow = baseArrow;
        }

        public Polyline ArrowLine { get; private set; }
        public List<Entity> ArrowSymbols { get; private set; }
        public Vector3d AxisVector { get; private set; }
        public bool IsRedirected { get; private set; }
        public bool IsMirrored { get; private set; }
        public bool IsSymbolsMirrored { get; private set; }
        public bool IsBottomDisplacemented { get; private set; }
        public bool IsTopDisplacemented { get; private set; }
        public double? LastValue { get; private set; }
        public IEnumerable<Entity> Entities
        {
            get
            {
                yield return (Entity)this.ArrowLine.Clone();
                foreach (var ent in this.ArrowSymbols)
                    yield return ent;
            }
        }

        public Matrix3d LineTarnsform
        {
            get { return _lineTarnsform; }
            set
            {
                this.ArrowLine.TransformBy(value);
                this.ArrowSymbols.ForEach(ent =>
                {
                    ent.TransformBy(value);
                    if (ent is DBText)
                        ((DBText)ent).AdjustAlignment(Tools.GetAcadDatabase());
                });
                _lineTarnsform = _lineTarnsform.PreMultiplyBy(value);
            }
        }

        public Arrow BaseArrow { get; private set; }
        public bool IsCodirectional
        {
            get
            {
                if (BaseArrow == null)
                    return false;
                return _lineTarnsform.CoordinateSystem3d.Xaxis.IsCodirectionalTo(
                       BaseArrow._lineTarnsform.CoordinateSystem3d.Xaxis, Tolerance.Global);
            }
        }

        public void AppendArrowSymbolsWithTransform(Entity entity)
        {
            this.ArrowSymbols.Add(entity.GetTransformedCopy(_lineTarnsform));
            if (entity is DBText)
                ((DBText)this.ArrowSymbols.Last()).AdjustAlignment(Tools.GetAcadDatabase());
        }

        public void AppendArrowSymbols(Entity entity)
        {
            this.ArrowSymbols.Add((Entity)entity.Clone());
            if (entity is DBText)
                ((DBText)this.ArrowSymbols.Last()).AdjustAlignment(Tools.GetAcadDatabase());
        }

        public void AppendArrowSymbols(IEnumerable<Entity> entities)
        {
            entities.ToList().ForEach(ent => AppendArrowSymbols(ent));
        }

        public void Mirror()
        {
            Matrix3d mirror = _lineTarnsform;

            mirror = _mirror(this.ArrowLine, _lineTarnsform);
            //Matrix3d mirror = _mirror(this.ArrowLine);

            if (IsCodirectional && this.BaseArrow.IsSymbolsMirrored)
                this.BaseArrow.MirrorSymbols();

            LineTarnsform = mirror;
            IsMirrored = !IsMirrored;

        }

        private Matrix3d _mirror(Entity entity, Matrix3d transform)
        {
            //Plane plane = new Plane(Point3d.Origin, transform.CoordinateSystem3d.Yaxis, transform.CoordinateSystem3d.Zaxis);
            Plane plane = new Plane(Point3d.Origin, Matrix3d.Identity.CoordinateSystem3d.Yaxis, Matrix3d.Identity.CoordinateSystem3d.Zaxis);
            plane.TransformBy(_lineTarnsform);
            Matrix3d mat = Matrix3d.Mirroring(plane);
            return mat;
        }

        private Matrix3d _mirror(Entity entity)
        {
            Plane plane = new Plane(Point3d.Origin, this.AxisVector, Matrix3d.Identity.CoordinateSystem3d.Zaxis);
            Matrix3d mat = Matrix3d.Mirroring(plane);
            return mat;
        }

        public void Redirect(Point3d point)
        {
            Vector3d directionVector = _lineTarnsform.CoordinateSystem3d.Xaxis.Negate();
            if (IsRedirected)
                directionVector = directionVector.Negate();

            point = new Point3d(point.X, point.Y, 0);
            point = point.TransformBy(_getMainRotation().Inverse());
            Vector3d vector = (point - Point3d.Origin).Normalize();

            if (vector.X * directionVector.X <= 0)
                return;

            /*Point3d destPoint = Point3d.Origin.Add(directionVector.MultiplyBy(_spaceLength * 2d + _length + _arrowLength));
            Matrix3d mat = Matrix3d.Displacement(destPoint.GetAsVector());
            LineTarnsform = mat;
            IsRedirected = !IsRedirected;*/
            Redirect();
        }

        public void Redirect()
        {
            if (IsCodirectional || this.BaseArrow == null)
            {
                Point3d destPoint = Point3d.Origin.Add(_lineTarnsform.CoordinateSystem3d.Xaxis.Negate()
                    .MultiplyBy((IsRedirected ? -1 : 1) * (_spaceLength * 2d + _length + _arrowLength)));
                Matrix3d mat = Matrix3d.Displacement(destPoint.GetAsVector());
                LineTarnsform = mat;
                IsRedirected = !IsRedirected;

                if (IsCodirectional)
                    this.BaseArrow.Redirect();
            }
        }

        private Polyline _createLine()
        {
            Point3d origin = Point3d.Origin;

            Polyline pline = new Polyline(3);
            pline.AddVertexAt(0, new Point3d(origin.X + _spaceLength, origin.Y, 0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(pline.GetPoint2dAt(0).X + _length, pline.GetPoint2dAt(0).Y), 0, _arrowBlug, 0);
            pline.AddVertexAt(2, new Point2d(pline.GetPoint2dAt(1).X + _arrowLength, pline.GetPoint2dAt(1).Y), 0, 0, 0);
            //pline.AddVertexAt(3, new Point3d(pline.GetPoint2dAt(2).X, pline.GetPoint2dAt(2).Y + 1d, 0), 0, 0, 0);
            pline.LineWeight = LineWeight.LineWeight020;

            return pline;
        }

        private Matrix3d _getMainRotation()
        {
            double angle = Matrix3d.Identity.CoordinateSystem3d.Xaxis.GetAngleTo(this.AxisVector.GetPerpendicularVector().Negate(),
                Matrix3d.Identity.CoordinateSystem3d.Zaxis.Negate());
            /*double angle = Matrix3d.Identity.CoordinateSystem3d.Yaxis.GetAngleTo(this.AxisVector,
                Matrix3d.Identity.CoordinateSystem3d.Zaxis);*/
            Matrix3d rotation = Matrix3d.Rotation(angle,
            Matrix3d.Identity.CoordinateSystem3d.Zaxis.Negate(), Point3d.Origin);

            return rotation;
        }

        [Obsolete("изм. разделил логику на два метода")]
        public double CalculateEx(Point3d pointLocal)
        {
            Point3d point2d = new Point3d(pointLocal.X, pointLocal.Y, 0d);
            Vector3d perp = point2d - point2d.Add(this.AxisVector.GetPerpendicularVector());
            Vector3d project = perp.ProjectTo(this.AxisVector.GetNormal(), this.AxisVector);

            Line3d line3d = new Line3d(point2d, project);
            var points = line3d.IntersectWith(new Line3d(Point3d.Origin, this.AxisVector), Tolerance.Global);

            if (points != null && points.Length > 0)
            {
                Line line = new Line(point2d, points[0]);
                this.LastValue = line.Length * (this.AxisVector.GetPerpendicularVector().IsCodirectionalTo(line.GetFirstDerivative(0d)) ? 1 : -1);
                if (LastValue < 0d)
                    this.Mirror();

                if (this.IsCodirectional)
                {
                    if (BaseArrow.IsRedirected && !this.IsRedirected)
                        this.Redirect();
                    if (!this.IsSymbolsMirrored && !BaseArrow.IsSymbolsMirrored)
                        BaseArrow.MirrorSymbols();
                }
                else
                {
                    if (this.BaseArrow != null)
                    {
                        if (this.BaseArrow.IsRedirected)
                        {
                            this.BaseArrow.Redirect();
                            if (this.BaseArrow.IsSymbolsMirrored)
                                this.BaseArrow.MirrorSymbols();
                        }
                        else
                        {
                            if (this.BaseArrow.IsSymbolsMirrored)
                                this.BaseArrow.MirrorSymbols();
                        }
                    }
                }

                return LastValue.Value;
            }

            return 0d;
        }

        public double Calculate(Point3d destPointLocal)
        {
            this.LastValue = _calculateValue(destPointLocal);
            if (this.LastValue.Value < 0d)
                this.Mirror();

            if (this.IsCodirectional)
            {
                if (BaseArrow.IsRedirected && !this.IsRedirected)
                    this.Redirect();
                if (!this.IsSymbolsMirrored && !BaseArrow.IsSymbolsMirrored)
                    BaseArrow.MirrorSymbols();
            }
            else
            {
                if (this.BaseArrow != null)
                {
                    if (this.BaseArrow.IsRedirected)
                    {
                        /////////////////////////////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!///////////////////////////
                        /*this.BaseArrow.Redirect();
                        if (this.BaseArrow.IsSymbolsMirrored)
                            this.BaseArrow.MirrorSymbols();*/
                        ///////////////////////////////////////////////////////!!!!!!!!!!!!!!!!!!!!//////////////////////////////////////////////
                        if (!this.BaseArrow.IsBottomDisplacemented)
                            this.BaseArrow.MoveToBottom();
                        if (!this.IsTopDisplacemented)
                            this.MoveToTop();
                    }
                    else
                    {
                        if (this.BaseArrow.IsSymbolsMirrored)
                            this.BaseArrow.MirrorSymbols();
                    }
                }
            }

            return LastValue.Value;
        }

        private double _calculateValue(Point3d destPoint)
        {
            Point3d point2d = new Point3d(destPoint.X, destPoint.Y, 0d);
            Vector3d perp = point2d - point2d.Add(this.AxisVector.GetPerpendicularVector());
            Vector3d project = perp.ProjectTo(this.AxisVector.GetNormal(), this.AxisVector);

            Line3d line3d = new Line3d(point2d, project);
            var points = line3d.IntersectWith(new Line3d(Point3d.Origin, this.AxisVector), Tolerance.Global);

            if (points != null && points.Length > 0)
            {
                Line line = new Line(point2d, points[0]);
                double res = line.Length * (this.AxisVector.GetPerpendicularVector().IsCodirectionalTo(line.GetFirstDerivative(0d)) ? 1 : -1);
                return res;
            }
            throw new ArgumentOutOfRangeException();
        }

        public void MirrorSymbols()
        {
            Plane plane = new Plane(Point3d.Origin, _lineTarnsform.CoordinateSystem3d.Xaxis, _lineTarnsform.CoordinateSystem3d.Zaxis);
            Matrix3d mat = Matrix3d.Mirroring(plane);

            Extents3d? bounds = _getSymbolsBounds(this.ArrowSymbols);
            if (bounds.HasValue)
            {
                Point3d max = bounds.Value.MaxPoint.TransformBy(_lineTarnsform.Inverse());
                max = new Point3d(0, max.Y, 0).TransformBy(_lineTarnsform);
                Point3d min = bounds.Value.MinPoint.TransformBy(_lineTarnsform.Inverse());
                min = new Point3d(0, min.Y, 0).TransformBy(_lineTarnsform);

                Vector3d vector = (max - min).DivideBy(2d).Add(min - Point3d.Origin);

                Point3d point = Point3d.Origin.Add(vector.Negate());
                plane = new Plane(point,
                    _lineTarnsform.CoordinateSystem3d.Xaxis, _lineTarnsform.CoordinateSystem3d.Zaxis);
                mat = mat.PreMultiplyBy(Matrix3d.Mirroring(plane));
            }

            this.ArrowSymbols.ForEach(ent =>
            {
                ent.TransformBy(mat);
                if (ent is DBText)
                    ((DBText)ent).AdjustAlignment(Tools.GetAcadDatabase());
            });

            IsSymbolsMirrored = !IsSymbolsMirrored;
        }

        public bool MoveToBottom()
        {
            if (this.IsBottomDisplacemented)
                return false;

            Matrix3d? displacement = _movetAtYaxis(-1);
            if (!displacement.HasValue)
                return false;

            this.LineTarnsform = displacement.Value;
            this.IsBottomDisplacemented = !this.IsBottomDisplacemented;
            return true;
        }

        public bool MoveToTop()
        {
            if (this.IsTopDisplacemented)
                return false;

            Matrix3d? displacement = _movetAtYaxis(1);
            if (!displacement.HasValue)
                return false;

            this.LineTarnsform = displacement.Value;
            this.IsTopDisplacemented = !this.IsTopDisplacemented;
            return true;
        }

        private Matrix3d? _movetAtYaxis(int sign)
        {
            Extents3d? bounds = _getSymbolsBounds(this.ArrowSymbols);
            if (!bounds.HasValue)
                return null;

            Vector3d direction = new Vector3d();
            if (sign < 0)
                direction = _lineTarnsform.CoordinateSystem3d.Yaxis.Negate();
            else
                direction = _lineTarnsform.CoordinateSystem3d.Yaxis;

            Point3d max = bounds.Value.MaxPoint.TransformBy(_lineTarnsform.Inverse());
            max = new Point3d(0, max.Y, 0).TransformBy(_lineTarnsform);
            Point3d min = bounds.Value.MinPoint.TransformBy(_lineTarnsform.Inverse());
            min = new Point3d(0, min.Y, 0).TransformBy(_lineTarnsform);

            Matrix3d mat = Matrix3d.Displacement(direction.MultiplyBy(Math.Abs((max.Y - min.Y) / 2d)));

            return mat;
        }

        private Extents3d? _getSymbolsBounds(IEnumerable<Entity> symbols)
        {
            if (symbols.Count() < 1)
                return null;
            Extents3d res = new Extents3d();
            symbols.ToList().ForEach(ent =>
            {
                /*var clone = ent.GetTransformedCopy(_lineTarnsform.Inverse());
                if (clone.Bounds.HasValue)
                    res.AddExtents(ent.Bounds.Value);*/
                if (ent is DBText)
                {
                    var rectg = ((DBText)ent).GetTextBoxCorners();
                    if (rectg.HasValue)
                    {
                        Point3d lowerLeft = rectg.Value.LowerLeft.TransformBy(_lineTarnsform.Inverse());
                        Point3d upperRight = rectg.Value.UpperRight.TransformBy(_lineTarnsform.Inverse());

                        ///////////////////////////////////////////////////////////!!!!!!!!!!//////////////////////////
                        if (lowerLeft.X > upperRight.X)
                            lowerLeft = new Point3d(upperRight.X - 1d, lowerLeft.Y, lowerLeft.Z);
                        ///////////////////////////////////////////////////////////!!!!!!!!!!//////////////////////////

                        try
                        {
                            Extents3d ext = new Extents3d(lowerLeft, upperRight);
                            res.AddExtents(ext);
                        }
                        catch { }
                    }
                }
            });
            if (res == null)
                return null;
            res.TransformBy(_lineTarnsform);
            return res;
        }

        public event EventHandler<ArrowEventArgs> ArrowChanging;
        protected virtual void On_ArrowChanging(object sender, ArrowEventArgs e)
        {
            if (ArrowChanging != null)
                ArrowChanging(sender, e);
        }

        public class ArrowEventArgs : EventArgs
        {
            public ArrowEventArgs()
                : base()
            {

            }

            public ArrowActions Action { get; set; }
            public object Tag { get; set; }
        }

        public enum ArrowActions
        {
            Moved,
            Mirrowed,
            Redirected
        }


        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand,
           Flags = System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("AxisVector", this.AxisVector.ToArray());
            //info.AddValue("BaseArrow", this.BaseArrow);
        }

        protected Arrow(
         System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");

            this.AxisVector = new Vector3d((double[])info.GetValue("AxisVector", typeof(double[])));
            //this.BaseArrow = (Arrow)info.GetValue("BaseArrow", typeof(Arrow));
        }

        public override string ApplicationName
        {
            get
            {
                return "Icmd_WallArrow_Data";
            }
        }
    }
}
