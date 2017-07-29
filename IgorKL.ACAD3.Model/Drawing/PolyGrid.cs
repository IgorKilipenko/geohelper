using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class PolyGrid:CustomObjects.SimpleEntityOverride
    {
        private double _verticalStep;
        private double _horizontalStep;
        private Rectangle3d _mainRec;
        private Point3d _jigPosition;

        public PolyGrid()
            : this(Point3d.Origin, 0,0, Matrix3d.Identity)
        {
        }

        public PolyGrid(Point3d origin, double hStep, double vStep, Matrix3d ucs)
            :base(origin, new List<Entity>(), ucs)
        {
            _horizontalStep = hStep;
            _verticalStep = vStep;
            _jigPosition = Point3d.Origin;
        }

#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_DrawRec", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TestDrawRec()
        {
            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите начальную точку");

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;

            Drawing.PolyGrid grid = new Drawing.PolyGrid(ppr.Value, 100, 50, ucs);

            /*ppo = new PromptPointOptions("\nУкажите конечную точку");
            ppo.UseBasePoint = true;
            ppo.BasePoint = grid.Origin;
            ppo.UseDashedLine = true;

            ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;*/

            //grid.Display(ppr.Value);

            if (grid.JigDraw() == PromptStatus.OK)
                return;
        }
#endif

        private void _createGrid(Point3d destPoint)
        {
            Entities.Clear();


            Vector3d vector = destPoint-Origin;

            /*Point3d leftTop = Origin.Add(_ucs.CoordinateSystem3d.Yaxis.MultiplyBy(vector.Y));
            Point3d leftLow = Origin;
            Point3d rightTop = destPoint;
            Point3d rightLow = Origin.Add(_ucs.CoordinateSystem3d.Xaxis.MultiplyBy(vector.X));*/

            Point3d leftTop = Origin.Add(Matrix3d.Identity.CoordinateSystem3d.Yaxis.MultiplyBy(vector.Y));
            Point3d leftLow = Origin;
            Point3d rightTop = destPoint;
            Point3d rightLow = Origin.Add(Matrix3d.Identity.CoordinateSystem3d.Xaxis.MultiplyBy(vector.X));

            _mainRec = new Rectangle3d(leftTop, rightTop, leftLow, rightLow);

            double hLength = Math.Abs((_mainRec.LowerRight - _mainRec.LowerLeft).X);
            double vLength = Math.Abs((_mainRec.UpperLeft - _mainRec.LowerLeft).Y);

            double hCount = hLength / _horizontalStep;
            double vCount = vLength / _verticalStep;

            int rowsCount = (int)Math.Ceiling(vCount);
            int columnsCount = (int)Math.Ceiling(hCount);

            Rectangle3d current = new Rectangle3d(
                upperLeft: _mainRec.LowerLeft.Add((_mainRec.UpperLeft-_mainRec.LowerLeft).Normalize().MultiplyBy(_verticalStep)),
                upperRight: _mainRec.LowerLeft.Add((_mainRec.LowerRight - _mainRec.LowerLeft).Normalize().MultiplyBy(_horizontalStep))
                    .Add((_mainRec.UpperLeft-_mainRec.LowerLeft).Normalize().MultiplyBy(_verticalStep)),
                lowerLeft: _mainRec.LowerLeft,
                lowerRight: _mainRec.LowerLeft.Add((_mainRec.LowerRight-_mainRec.LowerLeft).Normalize().MultiplyBy(_horizontalStep))
                );

            SimpleGride table = new SimpleGride(current);
            for (int r = 0; r < rowsCount; r++)
            {
                for (int c= 0; c < columnsCount; c++)
                {
                    var rec = table.CalculateRectagle(r, c);
                    rec.TransformBy(_ucs);
                    this.Entities.Add(rec);
                }
            }

        }


        public void Display(Point3d destPoint)
        {
            _createGrid(destPoint);

            base.Display();
        }

        protected override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите точку");
            ppo.UseBasePoint = true;
            ppo.BasePoint = this.Origin;

            ppo.UserInputControls =  UserInputControls.NoZeroResponseAccepted;

            PromptPointResult ppr = prompts.AcquirePoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;

            if (_jigPosition == ppr.Value.TransformBy(_ucs.Inverse()))
                return SamplerStatus.NoChange;

            _jigPosition = ppr.Value.TransformBy(_ucs.Inverse());

            lock (Entities)
            {
                _createGrid(_jigPosition);
            }

            return SamplerStatus.OK;
        }

    }
}
