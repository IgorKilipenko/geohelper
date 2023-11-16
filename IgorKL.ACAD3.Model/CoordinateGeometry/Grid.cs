using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IgorKL.ACAD3.Model.CoordinateGeometry.Helpers {
    public class CoordinateGrid {
        public static IEnumerable<double> GenSeries(double startValue, double step, int count = int.MaxValue) {
            double prev = startValue;
            while (count-- > 0) {
                yield return prev;
                prev += step;
            }
        }

        public CoordinateGrid(double originRow, double originColumn, double step) {
            OriginRow = originRow;
            OriginColumn = originColumn;
            Step = step;
        }

        private List<double> _rows = new List<double>();
        private List<double> _columns = new List<double>();

        public double Step { get; private set; }
        public double OriginRow { get; private set; }
        public double OriginColumn { get; private set; }

        public IEnumerable<double> Rows { get => _rows; }
        public IEnumerable<double> Columns { get => _columns; }

        public int RowsCount { get => _rows.Count; }
        public int ColumnsCount { get => _columns.Count; }

        public void Build(int rowsCount, int columnsCount) {
            _rows.AddRange(GenSeries((_rows?.Count ?? 0) == 0 ? OriginRow : _rows.Last(), Step, rowsCount).ToList());
            _columns = GenSeries((_columns?.Count ?? 0) == 0 ? OriginColumn : _columns.Last(), Step, columnsCount).ToList();
        }
    }
}

namespace IgorKL.ACAD3.Model.CoordinateGeometry.Helpers.UnitTests {
    public class CoordinateGridTester {

        const string _prefix = "CoordinateGrid";

        public void Test1() {
            int maxCount = 5;
            var rows = CoordinateGrid.GenSeries(10d, 10d, maxCount).ToList<double>();
            rows.AddRange(CoordinateGrid.GenSeries(rows.Last() + 5.5, 10d, 1));

            List<double> expected = new List<double> { 10d, 20d, 30d, 40d, 50d, 65.5 };

            bool condition = rows.Count == expected.Count || rows.SequenceEqual(expected);
            if (!condition) {
                Tools.Write($"{_prefix}_Test1 -> Failed");
                //~ System.Diagnostics.Debug.Assert(false);
            } else {
                Tools.Write($"{_prefix}_Test1 -> Pass");
            }
        }

        public void Test2() {
            var grid = new CoordinateGrid(10d, 10d, 10d);
            grid.Build(5, 5);

            List<double> expected = new List<double> { 10d, 20d, 30d, 40d, 50d };
            bool condition = grid.RowsCount == expected.Count || grid.Rows.SequenceEqual(expected);
            condition &= grid.ColumnsCount == expected.Count || grid.Columns.SequenceEqual(expected);
            if (!condition) {
                Tools.Write($"\n{_prefix}_Test2 -> Failed");
            } else {
                Tools.Write($"\n{_prefix}_Test2 -> Pass");
            }
        }

        public void Test3() {
            var res = Tools.GetAcadEditor().GetPoint(new Autodesk.AutoCAD.EditorInput.PromptPointOptions("\nSelect point"));
            if (res.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) {
                return;
            }

            var grid = new CoordinateGrid(res.Value.Y, res.Value.X, 10d);
            grid.Build(5, 5);

            foreach (var row in grid.Rows) {
                foreach (var col in grid.Columns) {
                    Tools.AppendEntity(new List<DBPoint> { new DBPoint(res.Value) });
                }

            }
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("ICmd_UnitTest_CoordinateGrid")]
        public void Run() {
            Test1();
            Test2();
        }
    }
}