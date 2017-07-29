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
    public static class PointExtensions
    {
        public static Point2d Convert2d(this Point3d p3d)
        {
            return new Point2d(p3d.X, p3d.Y);
        }

        public static Point3d Convert3d(this Point2d p2d)
        {
            return new Point3d(p2d.X, p2d.Y, 0d);
        }

        public static Point3d GetPointTo(this Point3d point, Vector3d vector, double dist)
        {
            Line3d line = new Line3d(point, vector);
            Line line2 = new Line(line.StartPoint, line.EndPoint);
            return line2.GetPointAtDist(dist);
        }

        public static Vector3d Normalize(this Vector3d vector)
        {
            return vector.DivideBy(vector.Length);
        }

        public static Vector3d PositiveOnly(this Vector3d vector)
        {
            Vector3d result = new Vector3d(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
            return result;
        }

        public static Point3d CreateRandomRectgPoints(this Point3d point, double radius, double elevationBound = 0d)
        {
            Point3d randomPoint = new Point3d(
                point.X + (Helpers.Math.Randoms.RandomGen.NextDouble()-0.5d)*radius*2d,
                point.Y + (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * radius * 2d,
                point.Z + (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d)*elevationBound *2d
                );
            return point;
        }

        public static IEnumerable<Point3d> CreateRandomRectgPoints(this Point3d point, int count ,double radius, double elevationBound = 0d)
        {
            while (count-- > 0)
            {
                Point3d randomPoint = new Point3d(
                    point.X + (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * radius * 2d,
                    point.Y + (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * radius * 2d,
                    point.Z + (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * elevationBound * 2d
                    );
                yield return randomPoint;
            }
        }
        public static IEnumerable<Point3d> CreateRandomCirclePoints(this Point3d point, int count, double radius)
        {
            
            while (count-- > 0)
            {
                Vector3d vector = new Vector3d(
                (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * 2d,
                (Helpers.Math.Randoms.RandomGen.NextDouble() - 0.5d) * 2d,
                0
                );
                Point3d randomPoint = point.Add(vector.MultiplyBy(radius));
                yield return randomPoint;
            }
        }

        public static bool IsInsidePolygon(this IEnumerable<Point2d> polygon, Point3d pt, double tolerencePct = 0.001)
        {
            int n = polygon.Count();
            double angle = 0;
            Point pt1, pt2;
            double tolerence = System.Math.PI * tolerencePct / 100d;

            for (int i = 0; i < n; i++)
            {
                pt1.X = polygon.ElementAt(i).X - pt.X;
                pt1.Y = polygon.ElementAt(i).Y - pt.Y;
                pt2.X = polygon.ElementAt((i + 1) % n).X - pt.X;
                pt2.Y = polygon.ElementAt((i + 1) % n).Y - pt.Y;
                angle += Angle2D(pt1.X, pt1.Y, pt2.X, pt2.Y);
            }

            if (System.Math.Abs(angle) - System.Math.PI < -tolerence)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Проверяет находится ли точка внутри полигона
        /// </summary>
        /// <param name="polygon">Исходный контур (полигон)</param>
        /// <param name="pt">Определяемая точка</param>
        /// <param name="tolerencePct">Допустимая погрешность расчета</param>
        /// <returns></returns>
        public static bool IsInsidePolygon(this IEnumerable<Point3d> polygon, Point3d pt, double tolerencePct = 0.001)
        {
            var polygon2d =  polygon.Select(p3d => p3d.Convert2d());
            return IsInsidePolygon(polygon2d, pt, tolerencePct);
        }
        private struct Point
        {
            public double X, Y;
        };

        private static double Angle2D(double x1, double y1, double x2, double y2)
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

        public static Polyline CreatePolyline (this IEnumerable<Point3d> points)
        {
            Polyline pline = new Polyline(points.Count());
            int i = 0;
            foreach (var p in points)
                pline.AddVertexAt(i, p);
            return pline;
        }

        /// <summary>
        /// Возвращает угол между векторами отсчитываемый против часовой стрелки от базового вектора до определяемого
        /// </summary>
        /// <param name="vector">Базовый вектор (катет)</param>
        /// <param name="destanationVector">Определяемый вектор (гипотенуза)</param>
        /// <returns>Угол в радианах</returns>
        public static double GetAngle2d(this Vector3d vector, Vector3d destanationVector)
        {
            double cos = vector.GetCos2d(destanationVector);
            double sin = new Vector3d(vector.X, vector.Y, 0d).GetPerpendicularVector().GetCos2d(destanationVector);
            double ang = Math.Acos(cos);
            /*if (sin < 0d && cos < 0d)
                ang += Math.PI / 2;
            else if (sin < 0d && cos > 0d)
                ang = Math.PI * 2d - ang;*/

            if (sin < 0d)
                ang = Math.PI * 2d - ang;

            return ang;

        }

        /// <summary>
        /// Просто отбрасывает Z
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2d Convert2d(this Vector3d vector)
        {
            return new Vector2d(vector.X, vector.Y);
        }

        /// <summary>
        /// В значение Z ставит 0 по умолчанию
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3d Convert3d(this Vector2d vector, double z = 0d)
        {
            return new Vector3d(vector.X, vector.Y, z);
        }

        /// <summary>
        /// Возвращает косинус угла от текущего вектора (катета) до указанного (гипотенузы) в 2д
        /// </summary>
        /// <remarks>
        /// Результат не зависит от длинны векторов, только от направлений векторов
        /// </remarks>
        /// <param name="vector">Базовый вектор (прилежащий катет)</param>
        /// <param name="destanationVector">Вектор до которого выполняется расчет (гипотенуза)</param>
        /// <returns>Косинус угла между векторами</returns>
        public static double GetCos2d(this Vector3d vector, Vector3d destanationVector)
        {
            var v2d = vector.Normalize().Convert2d();
            var destv2d = destanationVector.Normalize().Convert2d();

            return v2d.DotProduct(destv2d);
        }
        public static double GetSin2d(this Vector3d vector, Vector3d destanationVector)
        {
            var v2d = vector.Normalize().GetPerpendicularVector().Convert2d();
            var destv2d = destanationVector.Normalize().Convert2d();

            return v2d.DotProduct(destv2d);
        }

        public static Point3d GetWithNewElevation(this Point3d point, double elevation)
        {
            return new Point3d(point.X, point.Y, 0d);
        }
    }
}
