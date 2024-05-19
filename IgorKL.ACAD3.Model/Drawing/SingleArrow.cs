using Autodesk.AutoCAD.Geometry;

namespace IgorKL.ACAD3.Model.Drawing {
    public class SingleArrow {
        public double Length { get; set; } = 10d;
        public double High { get; set; } = 3d;
        public Matrix3d Matrix { get; set; }
        public double StartSpace { get; set; } = 1d;
        public Point3d StartPoint { get; set; } = Point3d.Origin;
    }
}
