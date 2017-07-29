using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class PolylineExtensions
    {

        public static Point3dCollection GetPoints(this Polyline pline)
        {
            Point3dCollection points = new Point3dCollection();
            int count = pline.NumberOfVertices;
            for (int i = 0; i < count; i++)
                points.Add(pline.GetPointAtParameter(i));
            return points;
        }

        public static Point3dCollection GetPoints(this Curve curve)
        {
            Point3dCollection points = new Point3dCollection();
            int count = curve.GetNumberOfVertices();
            for (int i = 0; i < count; i++)
                points.Add(curve.GetPointAtParameter(i));
            return points;
        }

        public static int GetNumberOfVertices(this Curve curve)
        {
            return Convert.ToInt32(curve.EndParam)+1;
        }

        public static void AddVertexAt(this Polyline pline, int index, Point3d point, double bulge, double startWidth, double endWidth)
        {
            pline.AddVertexAt(index, new Point2d(point.X, point.Y), bulge, startWidth, endWidth);
        }

        public static void AddVertexAt(this Polyline pline, int index, Point3d point)
        {
            pline.AddVertexAt(index, point, 0d, 0d, 0d);
        }

        public static Point3d? IntersectWithOnePoint(this Polyline source, Polyline distance, Intersect intersectType)
        {
            Point3dCollection points = new Point3dCollection();
            source.IntersectWith(distance, intersectType, points, new IntPtr(1), new IntPtr(0));
            if (points.Count == 0)
                return null;
            else
                return points[0];
        }
        public static IEnumerable<Point3d> IntersectWith(this Polyline source, Polyline distance, Intersect intersectType)
        {
            Point3dCollection points = new Point3dCollection();
            source.IntersectWith(distance, intersectType, points, new IntPtr(1), new IntPtr(0));
            if (points.Count > 0)
            {
                foreach (Point3d p in points)
                    yield return p;
            }
        }

        public static IEnumerable<Point3d> GetPointsAtStep(this Polyline pline, double step, double start, double length = -1d)
        {
            if (length < 0d)
                length = pline.Length - start;
            int count = Convert.ToInt32(Math.Truncate(length / (step)));
            for (int i = 0; i <= count; i++)
            {
                yield return pline.GetPointAtDist(i * step);
            }
        }

        public static IEnumerable<Point3d> GetPointsAtStep(this Polyline pline, double step)
        {
            return pline.GetPointsAtStep(step, 0d);
        }

        public static Point3d? GetNormalPointEx(this Polyline sourceLine, Point3d point, Polyline distanceLine)
        {
            Autodesk.AutoCAD.Geometry.Vector3d normal = sourceLine.GetFirstDerivative(point);
            normal = normal.GetPerpendicularVector();

            Autodesk.AutoCAD.Geometry.Point3dCollection intersects = new Autodesk.AutoCAD.Geometry.Point3dCollection();
            distanceLine.IntersectWith(new Line(point, new Autodesk.AutoCAD.Geometry.Point3d(point.X + normal.X, point.Y + normal.Y, 0d)), Intersect.ExtendBoth, new Autodesk.AutoCAD.Geometry.Plane(), intersects, new IntPtr(1), new IntPtr(0));
            if (intersects.Count == 0)
                return null;
            else
                return intersects[0];
        }

        public static Polyline GetPartOfLine(this Polyline sourcePline, Point3d startPoint, Point3d endPoint, bool copyStyle = false)
        {
            Polyline res = new Polyline();

            double param = sourcePline.GetParameterAtPoint(startPoint);
            int startIndex = Convert.ToInt32(Math.Truncate(param));
            res.AddVertexAt(0, startPoint, /*sourcePline.GetBulgeAt(startIndex)*/sourcePline.GetBulgeAt(param), sourcePline.GetStartWidthAt(startIndex), sourcePline.GetEndWidthAt(startIndex));

            param = sourcePline.GetParameterAtPoint(endPoint);
            int endIndex = Convert.ToInt32(Math.Truncate(param));

            if (startIndex == endIndex)
                res.SetBulgeAt(0, sourcePline.GetBulgeAt(param, false));

            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                res.AddVertexAt(i - startIndex, sourcePline.GetPoint2dAt(i), sourcePline.GetBulgeAt(i), sourcePline.GetStartWidthAt(i), sourcePline.GetEndWidthAt(i));
            }

            if (param > (double)endIndex)
                res.AddVertexAt(endIndex + 1 - startIndex, endPoint, /*sourcePline.GetBulgeAt(endIndex)*/sourcePline.GetBulgeAt(param), sourcePline.GetStartWidthAt(endIndex), sourcePline.GetEndWidthAt(endIndex));

            if (copyStyle)
                res.CopyFrom(sourcePline);

            return res;
        }

        /// <summary>
        /// Получает выпуклость сегмента пересчитанного на длину (определяемую по параметру)
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="param">Индекс сегмента</param>
        /// <param name="leftToRight">Определяет способ расчета длины участка сегмента</param>
        /// <returns>выпуклость пересчитаная на участок сегмента</returns>
        public static double GetBulgeAt(this Polyline pline, double param, bool leftToRight = true)
        {
            int stIndex = Convert.ToInt32(Math.Truncate(param));
            if ((double)stIndex == param)
                return pline.GetBulgeAt(stIndex);

            int endIndex = stIndex + 1;
            SegmentType stype = pline.GetSegmentType(stIndex);

            double length = 0;
            double segmentLength = 0;
            switch (stype)
            {
                case SegmentType.Arc:
                    {
                        length = pline.GetArcSegment2dAt(stIndex).GetLength(stIndex, param);
                        segmentLength = pline.GetArcSegment2dAt(stIndex).GetLength(stIndex, endIndex);
                        break;
                    }
                case SegmentType.Line:
                    {
                        return 0d;
                    }
                default:
                    {
                        throw new ArgumentNullException("SegmentType", "Unknown type :" + stype);
                    }
            }

            double bulge = pline.GetBulgeAt(stIndex) * (leftToRight ? (1d - length / segmentLength) : (length / segmentLength));
            return bulge;
        }

        public static double GetBulgeAt(this Polyline pline, Point3d point, bool leftToRight = true)
        {
            double param = pline.GetParameterAtPoint(point);
            return pline.GetBulgeAt(param, leftToRight);
        }

        public static void RemoveVertexAt(this Polyline pline, int startIndex, int count)
        {
            for (int i = count + startIndex - 1; i >= startIndex; i--)
                pline.RemoveVertexAt(i);
        }

        public static void RemoveVertexAt(this Polyline pline, Point3d startPoint, Point3d endPoint)
        {
            double param = pline.GetParameterAtPoint(startPoint);
            int startIndex = Convert.ToInt32(Math.Ceiling(param));

            if (startIndex - param > 0d)
                pline.ReplaceVertexAt(startIndex - 1, pline.GetPoint2dAt(startIndex - 1).Convert3d(), pline.GetBulgeAt(param, false), pline.GetStartWidthAt(startIndex - 1), pline.GetEndWidthAt(startIndex - 1));

            param = pline.GetParameterAtPoint(endPoint);
            int endIndex = Convert.ToInt32(Math.Truncate(param));

            if (param - endIndex > 0d)
                pline.ReplaceVertexAt(endIndex + 1, pline.GetPoint2dAt(endIndex + 1).Convert3d(), pline.GetBulgeAt(param, true), pline.GetStartWidthAt(endIndex + 1), pline.GetEndWidthAt(endIndex + 1));

            pline.AddVertexAt(startIndex, startPoint);
            startIndex++;
            endIndex++;

            pline.RemoveVertexAt(startIndex, endIndex - startIndex + 1);

            pline.AddVertexAt(startIndex, endPoint);
        }

        public static void ReplaceVertexAt(this Polyline pline, int index, Point3d point, double bulge, double startWidth, double endWidth)
        {
            if (pline.NumberOfVertices <= index)
                return;
            pline.AddVertexAt(index + 1, point.Convert2d(), bulge, startWidth, endWidth);
            pline.RemoveVertexAt(index);
        }

        public static void ReplaceVertexAt(this Polyline pline, int index, Point3d point)
        {
            double bulge = pline.GetBulgeAt(index);
            double startWidth = pline.GetStartWidthAt(index);
            double endWidth = pline.GetEndWidthAt(index);
            pline.ReplaceVertexAt(index, point, bulge, startWidth, endWidth);
        }

        public static double GetAngleAt(this Polyline pline, Point3d point)
        {
            var fd = pline.GetFirstDerivative(point);
            return fd.AngleOnPlane(new Plane());
        }

        public static bool IsInsidePolygon(this Polyline polygon, Point3d pt, double tolerencePct = 0.001)
        {
            int n = polygon.NumberOfVertices;
            double angle = 0;
            Point pt1, pt2;
            double tolerence = System.Math.PI * tolerencePct/100d;

            for (int i = 0; i < n; i++)
            {
                pt1.X = polygon.GetPoint2dAt(i).X - pt.X;
                pt1.Y = polygon.GetPoint2dAt(i).Y - pt.Y;
                pt2.X = polygon.GetPoint2dAt((i + 1) % n).X - pt.X;
                pt2.Y = polygon.GetPoint2dAt((i + 1) % n).Y - pt.Y;
                angle += Angle2D(pt1.X, pt1.Y, pt2.X, pt2.Y);
            }

            if (System.Math.Abs(angle) - System.Math.PI < -tolerence)
                return false;
            else
                return true;
        }
        public static Point3d GetCenterPoint(this Polyline pline)
        {
            return pline.GetPointAtDist(pline.Length / 2d);
        }

        public static IEnumerable<Point2d> GetPoints2d(this Polyline line)
        {
            int count = line.NumberOfVertices;
            for (int i = 0; i < count; i++)
            {
                yield return line.GetPoint2dAt(i);
            }
        }
        public static IEnumerable<Point3d> GetPoints3d(this Polyline line)
        {
            int count = line.NumberOfVertices;
            for (int i = 0; i < count; i++)
            {
                yield return line.GetPoint3dAt(i);
            }
        }
        public static void EditPolylineIneertPoints(this Polyline pline, IEnumerable<Point3d> points)
        {
            int count = Convert.ToInt32(pline.GetParameterAtDistance(pline.Length)) + 1;
            for (int i = 0; i < count && i < points.Count(); i++)
                pline.ReplaceVertexAt(i, points.ElementAt(i));
        }

        public static void AddVertexes(this Polyline pline, IEnumerable<Point3d> points)
        {
            int count = pline.NumberOfVertices;
            foreach (var p in points)
                pline.AddVertexAt(count++, p);
        }

        public static IEnumerable<Curve3d> GetSegments(this Polyline pline)
        {
            List<Curve3d> res = new List<Curve3d>();
            int count = pline.NumberOfVertices/*-1*/;
            for (int i = 0; i < count; i++)
            {
                SegmentType type = pline.GetSegmentType(i);
                switch (type)
                {
                    case SegmentType.Arc:
                        {
                            res.Add(pline.GetArcSegmentAt(i));
                            break;
                        }
                    case SegmentType.Line:
                        {
                            res.Add(pline.GetLineSegmentAt(i));
                            break;
                        }
                }
            }

            return res;
        }


        public static Line GetPerpendicularFromPoint(this Line line, Point3d point)
        {
            double k = 0;
            double x1 = line.StartPoint.X;
            double y1 = line.StartPoint.Y;
            double x2 = line.EndPoint.X;
            double y2 = line.EndPoint.Y;
            double x3 = point.X;
            double y3 = point.Y;

            k = ((y2 - y1) * (x3 - x1) - (x2 - x1) * (y3 - y1)) / (Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));
            double x4 = x3 - k * (y2 - y1);
            double y4 = y3 + k * (x2 - x1);

            return new Line(point, new Point3d(x4, y4, point.Z));
        }



        #region Ortho Normal Methods
        public static Point3d? GetOrthoNormalPointEx(this Polyline pline, Point3d point)
        {
            point = point.OrthoProject(pline.GetPlane());

            List<Point3d> res = new List<Point3d>();
            var segments = pline.GetSegments();
            foreach (var s in segments)
            {
                Vector3d vector = point - s.StartPoint;
                Vector3d segmVector = s.EndPoint - s.StartPoint;
                double cos = segmVector.GetCos2d(vector);

                double length = s.GetLength(s.GetParameterOf(s.StartPoint),
                    s.GetParameterOf(s.EndPoint), Tolerance.Global.EqualPoint);

                
                if (cos >= 0d && cos * vector.Length <= segmVector.Length)
                {
                    if (s is CircularArc3d)
                    {
                        CircularArc3d arc = (CircularArc3d)s;
                        Vector3d central = point - arc.Center;
                        PointOnCurve3d pointOn = null;
                        try
                        {
                            pointOn = s.GetNormalPoint(point, Tolerance.Global);
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        if (pointOn != null)
                        {
                            res.Add(pointOn.Point);
                        }
                    }
                    else if (s is LineSegment3d)
                    {
                        LineSegment3d line = (LineSegment3d)s;
                        Line buffer = new Line(line.StartPoint, line.EndPoint);
                        res.Add(buffer.GetPointAtDist(cos * vector.Length));
                    }
                }
            }
            if (res.Count == 0)
                return null;
            else
            {
                res.Sort((p1, p2) => Comparer<double>.Default.Compare((point - p1).Length, (point - p2).Length));
                return res[0];
            }
        }

        public static Point3d? GetOrthoNormalPoint(this Polyline pline, Point3d point, Plane plane ,bool nullForOutOfRange = true)
        {
            point = point.OrthoProject(pline.GetPlane());

            List<Point3d> res = new List<Point3d>();
            var segments = pline.GetSegments();
            int i = 0;
            foreach (var s in segments)
            {
                if (point.IsEqualTo(s.StartPoint, Tolerance.Global))
                    return point;

                Vector3d vector = point - s.StartPoint;
                Vector3d segmVector = s.EndPoint - s.StartPoint;
                double cos = segmVector.GetCos2d(vector);

                double length = s.GetLength(s.GetParameterOf(s.StartPoint),
                    s.GetParameterOf(s.EndPoint), Tolerance.Global.EqualPoint);

                ///////////////////////////////////////////////
                if (!nullForOutOfRange)
                {
                    if (i == 0 && cos < 0d)
                    {
                        return s.StartPoint;
                    }
                    if (i == segments.Count()-1 && cos > 0 && cos*vector.Length > segmVector.Length)
                    {
                        return s.EndPoint;
                    }
                    i++;
                }
                ///////////////////////////////////////////////

                if (cos >= 0d && cos * vector.Length <= segmVector.Length)
                {
                    if (s is CircularArc3d)
                    {
                        CircularArc3d arc3d = (CircularArc3d)s;
                        Arc arc = arc3d.ConvertToArc();
                        Point3d? normalPoint = arc.GetOrthoNormalPoint(point, nullForOutOfRange);
                        if (normalPoint.HasValue)
                            res.Add(normalPoint.Value);
                    }
                    else if (s is LineSegment3d)
                    {
                        LineSegment3d line3d = (LineSegment3d)s;
                        Line line = line3d.ConvertToLine();
                        Point3d? normalPoint = line.GetOrthoNormalPoint(point, plane, nullForOutOfRange);
                        if (normalPoint.HasValue)
                            res.Add(normalPoint.Value);
                    }
                }
            }
            if (res.Count == 0)
                return null;
            else
            {
                res.Sort((p1, p2) => Comparer<double>.Default.Compare((point - p1).Length, (point - p2).Length));
                return res[0];
            }
        }
        [Obsolete("use getClosestPoint")]
        public static Line GetOrthoNormalLine(this Polyline pline, Point3d point, Plane plane = null ,bool nullForOutOfRange = true)
        {
            point = point.OrthoProject(pline.GetPlane());

            Point3d? normalPoint = pline.GetOrthoNormalPoint(point, null ,nullForOutOfRange);
            if (!normalPoint.HasValue)
                return null;
            else if (nullForOutOfRange)
                return new Line(normalPoint.Value, point);

            int param = 0;
            try
            {
                if (normalPoint.Value.IsEqualTo(pline.EndPoint, Tolerance.Global))
                    param = (int)pline.EndParam;
                else if (normalPoint.Value.IsEqualTo(pline.StartPoint, Tolerance.Global))
                    param = (int)pline.StartParam;
                else
                    param = (int)pline.GetParameterAtPoint(normalPoint.Value);
            }
            catch
            {
                return new Line(normalPoint.Value, point);
            }
            var sType = pline.GetSegmentType(param);
            while (sType != SegmentType.Line && sType != SegmentType.Arc)
            {
                if (--param < pline.StartParam)
                    throw new ArgumentException();
                sType = pline.GetSegmentType(param);
            }
            Curve3d segment = sType == SegmentType.Line ? (Curve3d)pline.GetLineSegmentAt(param) : (Curve3d)pline.GetArcSegmentAt(param);

            if (segment is LineSegment3d)
                return ((LineSegment3d)segment).ConvertToLine().GetOrthoNormalLine(point, plane ,false);
            else if (segment is CircularArc3d)
                return ((CircularArc3d)segment).ConvertToArc().GetOrthoNormalLine(point ,false);
            else
                throw new ArgumentException();
        }

        [Obsolete("use getClosestPoint")]
        public static Point3d? GetOrthoNormalPoint(this Line line, Point3d point, Plane plane = null, bool nullForOutOfRange = true)
        {
            if (plane == null)
                plane = new Plane(line.StartPoint, Matrix3d.Identity.CoordinateSystem3d.Zaxis.Negate());

            line = (Line)line.GetOrthoProjectedCurve(plane);
            point = point.OrthoProject(line.GetPlane());

            Vector3d vector = point - line.StartPoint;
            Vector3d lineVector = line.EndPoint - line.StartPoint;

            double cos = lineVector.GetCos2d(vector);
            if (cos >= 0d && cos * vector.Length <= lineVector.Length)
            {
                Point3d p = line.GetPointAtDist(cos * vector.Length);
                Point3d perpendicularPoint = p;
                return perpendicularPoint;
            }
            else if (!nullForOutOfRange)
            {
                if (cos > 0d)
                    return line.EndPoint;
                else
                    return line.StartPoint;
            }
            return null;
        }
        public static Line GetOrthoNormalLine(this Line line, Point3d point, Plane plane = null ,bool nullForOutOfRange = true)
        {
            if (plane == null)
                plane = new Plane(line.StartPoint, Matrix3d.Identity.CoordinateSystem3d.Zaxis.Negate());
            
            line = (Line)line.GetOrthoProjectedCurve(plane);
            point = point.OrthoProject(line.GetPlane());

            Point3d? normalPoint = line.GetOrthoNormalPoint(point, plane, nullForOutOfRange);
            if (!normalPoint.HasValue)
                return null;
            
            Point3d destPoint = point;
            if (!nullForOutOfRange)
            {
                if (normalPoint.Value.IsEqualTo(line.StartPoint, Tolerance.Global) ||
                    normalPoint.Value.IsEqualTo(line.EndPoint, Tolerance.Global))
                {
                    Vector3d lineVector = line.EndPoint - line.StartPoint;
                    Vector3d vector = point - normalPoint.Value;
                    double sin = lineVector.GetSin2d(vector);
                    destPoint = normalPoint.Value.Add(lineVector.GetPerpendicularVector().MultiplyBy(sin * vector.Length));
                }
            }

            Line perpendicular = new Line(normalPoint.Value, destPoint);
            return perpendicular;
        }


        #endregion




        #region Edit Elevation
        public static Line GetWithNewElevation(this Line line, double elevation)
        {
            return new Line(new Point3d(line.StartPoint.X, line.StartPoint.Y, elevation), new Point3d(line.EndPoint.X, line.EndPoint.Y, elevation));
        }

        public static Polyline GetWithNewElevation(this Polyline pline, double elevation)
        {
            Polyline pline2d = ((Polyline)pline.Clone());
            pline2d.Elevation = elevation;
            return pline;
        }
        #endregion




        #region Convertor
        public static Line ConvertToLine(this LineSegment3d line3d)
        {
            return new Line(line3d.StartPoint, line3d.EndPoint);
        }
        public static Polyline ConvertToPolyline(this Polyline3d line3d)
        {
            var points = line3d.GetPoints().ToEnumerable().ToList();
            Polyline pline = new Polyline(points.Count());
            int i = 0;
            points.ForEach(p =>
            {
                pline.AddVertexAt(i++, p);
            });
            return pline;

            /*Polyline pline = new Polyline();
            pline.ConvertFrom(line3d, false);
            return pline*/
        }
        public static Polyline ConvertToPolyline(this Polyline2d line2d)
        {
            /*Polyline pline = new Polyline();
            pline.ConvertFrom(line2d, false);
            return pline;*/

            Polyline pline = new Polyline();
            pline.ConvertFrom(line2d, false);
            return pline;

        }
        public static Polyline ConvertToPolyline(this Line line)
        {
            Polyline pline = new Polyline(2);
            pline.AddVertexes(new[] { line.StartPoint, line.EndPoint });
            return pline;
        }
        
        #endregion





        #region Helpers
        public struct Point
        {
            public double X, Y;
        };

        public static double Angle2D(double x1, double y1, double x2, double y2)
        {
            double dtheta, theta1, theta2;

            theta1 = System.Math.Atan2(y1, x1);
            theta2 = System.Math.Atan2(y2, x2);
            dtheta = theta2 - theta1;
            while (dtheta > System.Math.PI)
                dtheta -= (System.Math.PI * 2);
            while (dtheta < -System.Math.PI)
                dtheta += (System.Math.PI * 2);
            return (dtheta);
        }
        #endregion




    }


}
