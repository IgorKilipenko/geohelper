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
    public class SimpleGride
    {
        private Rectangle3d _firstRectg;
        private Vector3d _toLeftUpVector;
        private Vector3d _toRightLowVector;
        private Point3d _insertPoint;
        private double _verticalStep;
        private double _horizontalStep;

        public double VerticalStep { get { return _verticalStep; } }
        public double HorizontalStep { get { return _horizontalStep; } }

        public SimpleGride(Rectangle3d firstRectg)
        {
            _firstRectg = firstRectg;
            _insertPoint = _firstRectg.LowerLeft;
            
            _toRightLowVector = firstRectg.LowerRight - firstRectg.LowerLeft;
            _toLeftUpVector = firstRectg.UpperLeft - firstRectg.LowerLeft;

            _verticalStep = _toLeftUpVector.Length;
            _horizontalStep = _toRightLowVector.Length;

            _toRightLowVector = _toRightLowVector.Normalize();
            _toLeftUpVector = _toLeftUpVector.Normalize();
        }

        public SimpleGride(Point3d insertPoint, Vector3d toLeftUpVector, Vector3d toRightLowVector, double verticalStep, double horizontalStep)
        {
            _toRightLowVector = toRightLowVector.Normalize();
            _toLeftUpVector = toLeftUpVector.Normalize();
            
            _verticalStep = verticalStep;
            _horizontalStep = horizontalStep;

            ///////////////////////////////////////////////////
            _insertPoint = insertPoint;
            ///////////////////////////////////////////////////

            _firstRectg = new Rectangle3d(
                lowerLeft: _insertPoint,
                upperLeft: _insertPoint.Add(_toLeftUpVector.MultiplyBy(verticalStep)),
                lowerRight: _insertPoint.Add(_toRightLowVector.MultiplyBy(horizontalStep)),
                upperRight: _insertPoint.Add(_toRightLowVector.MultiplyBy(horizontalStep)).Add(_toLeftUpVector.MultiplyBy(verticalStep))
                );

            _insertPoint = insertPoint;
        }


        public Vector3d HorizontalVector
        {
            get { return _firstRectg.LowerRight - _firstRectg.LowerLeft; }
        }

        public Vector3d VerticalVector
        {
            get { return _firstRectg.UpperLeft - _firstRectg.LowerLeft; }
        }

        public Polyline CalculateRectagle(int rowNumber, int columnNumber)
        {
            Matrix3d mat = Matrix3d.Displacement(_toRightLowVector.MultiplyBy(columnNumber*_horizontalStep));
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(_toLeftUpVector.MultiplyBy(rowNumber*_verticalStep)));

            Polyline pline = _firstRectg.ConvertToPolyline(mat);
            return pline;
        }
        public Polyline CalculateRectagle(int rowNumber, int columnNumber, Polyline polygon, bool allowInnerInside)
        {
            Matrix3d mat = Matrix3d.Displacement(_toRightLowVector.MultiplyBy(columnNumber * _horizontalStep));
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(_toLeftUpVector.MultiplyBy(rowNumber * _verticalStep)));

            Polyline pline = _firstRectg.ConvertToPolyline(mat);

            var points = pline.GetPoints().ToEnumerable();
            if (!points.Any(p => polygon.IsInsidePolygon(p)))
            {
                if (!allowInnerInside)
                    return null;
                else if (!polygon.GetPoints().ToEnumerable().Any(p => pline.IsInsidePolygon(p)))
                    return null;
            }
            return pline;
        }

        public Point3d CalculateGridPoint(int rowNumber, int columnNumber)
        {
            Matrix3d mat = Matrix3d.Displacement(_toRightLowVector.MultiplyBy(columnNumber * _horizontalStep));
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(_toLeftUpVector.MultiplyBy(rowNumber * _verticalStep)));

            Point3d point = _insertPoint.TransformBy(mat);
            return point;
        }

        public int GetRowNumber(Point3d point)
        {
            if (point.IsEqualTo(_insertPoint, Tolerance.Global))
                return 0;

            Vector3d vector = point - _insertPoint;
            double yval = VerticalVector.GetCos2d(vector) * vector.Length;
            return (int)(yval / _verticalStep);
        }

        public int GetColumnNumber(Point3d point)
        {
            if (point.IsEqualTo(_insertPoint, Tolerance.Global))
                return 0;

            Vector3d vector = point - _insertPoint;
            double xval = HorizontalVector.GetCos2d(vector) * vector.Length;
            return (int)(xval / _horizontalStep);
        }

        public static Rectangle3d CreateRectangle(Point3d leftLowPoint, Point3d rightHighPoint, CoordinateSystem3d cs)
        {
            Vector3d verticalVector = cs.Yaxis.MultiplyBy((rightHighPoint-leftLowPoint).Y);
            Vector3d horizontalVector = cs.Xaxis.MultiplyBy((rightHighPoint-leftLowPoint).X);

            Point3d lowerLeft = leftLowPoint;
            Point3d lowerRight = leftLowPoint.Add(horizontalVector);
            Point3d upperLeft = leftLowPoint.Add(verticalVector);
            Point3d upperRight = lowerRight.Add(verticalVector);

            Rectangle3d rectg = new Rectangle3d(
                lowerLeft: lowerLeft,
                lowerRight: lowerRight,
                upperLeft: upperLeft,
                upperRight: upperRight
                );

            return rectg;
        }

        public static int CalculateCeilingCount(Vector3d vector, double step)
        {
            return (int)Math.Ceiling(vector.Length / step);
        }
    }
}
