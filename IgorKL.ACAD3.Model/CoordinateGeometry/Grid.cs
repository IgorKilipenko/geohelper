using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model.CoordinateGeometry.Helpers {
    public class CoordinateGrid {
        public static IEnumerable<double> GenSeries(double startValue, double step, int count = int.MaxValue) {
            double prev = startValue;
            while (count-- > 0) {
                yield return prev + step;
            }
        }
    }
}

namespace IgorKL.ACAD3.Model.CoordinateGeometry.Helpers.UnitTests {
    public class CoordinateGridTests {
        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmd_UnitTest_CoordinateGrid1")]
        public void UnitTest_CoordinateGrid1() {
            var rows = CoordinateGrid.GenSeries(0d, 10d);
            int maxCount = 5;
            foreach (var row in rows) {
                if (maxCount-- == 0)
                    return;
                Tools.Write($"{row:#.000}, ");
            }
        }
    }
}