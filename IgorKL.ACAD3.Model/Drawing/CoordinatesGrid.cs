using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class CoordinatesGrid:CustomObjects.MultiEntity
    {
        private double _step;
        private SimpleGride _innerGridUcs;
        private Rectangle3d _boundRectg;
        private Point3d _jigPositionUcs;

        public CoordinatesGrid()
            : this(Point3d.Origin, 0, Matrix3d.Identity)
        {

        }
        public CoordinatesGrid(Point3d insertPointUcs, double step, Matrix3d ucs)
            : base(insertPointUcs, Point3d.Origin, new List<Entity>(), AnnotativeStates.True, ucs)
        {
            _step = step;
        }

        [RibbonCommandButton("Сетка координат", RibbonPanelCategories.Coordinates_Scale)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawCoordinateGrid", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestDrawRec()
        {
            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            PromptIntegerOptions pio = new PromptIntegerOptions("\nУкажите шаг сетки");
            pio.AllowZero = false;
            pio.AllowNone = false;
            pio.AllowNegative = false;

            PromptIntegerResult pir = Tools.GetAcadEditor().GetInteger(pio);
            if (pir.Status != PromptStatus.OK)
                return;
            double step = pir.Value;

            PromptPointOptions ppo = new PromptPointOptions("\nУкажите начальную точку");

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;

            
            CoordinatesGrid grid = new CoordinatesGrid(ppr.Value, step, ucs);

            if (grid.JigDraw() == PromptStatus.OK)
            {
                grid.AddInnerEntitiesToDatabase(HostApplicationServices.WorkingDatabase);
            }
        }

        public void Calculate(Point3d upperRightPointUcs)
        {
            double dx = InsertPointUcs.X % _step;
            double dy = InsertPointUcs.Y % _step;
            Matrix3d mat = Matrix3d.Identity;
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(_ucs.CoordinateSystem3d.Xaxis.Negate().MultiplyBy(dx)))
                .PreMultiplyBy(Matrix3d.Displacement(_ucs.CoordinateSystem3d.Yaxis.Negate().MultiplyBy(dy)));

            _boundRectg = SimpleGride.CreateRectangle(InsertPointUcs.TransformBy(mat), upperRightPointUcs,  InnerBlockTransform.CoordinateSystem3d);

            SimpleGride grid = new SimpleGride(InsertPointUcs.TransformBy(mat), _boundRectg.UpperLeft - _boundRectg.LowerLeft,
                _boundRectg.LowerRight - _boundRectg.LowerLeft, _step, _step);
            _innerGridUcs = grid;
        }

        private void _setEntitiesFromInnerGrid()
        {
            int rowCount = SimpleGride.CalculateCeilingCount(_boundRectg.GetLeftVerticalVector(), _step) + 1;
            int columnCount = SimpleGride.CalculateCeilingCount(_boundRectg.GetLowertHorizontalVector(), _step) + 1;

            if (rowCount * columnCount > Math.Pow(50, 2))
                return;
            if (rowCount * columnCount == 0d)
                return;

            Entities.ForEach(ent => ent.Dispose());
            Entities.Clear();
            CoordinateLable lableFactory = new CoordinateLable();
            Tools.StartTransaction(() =>
            {
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        //Entities.Add(new DBPoint(_innerGridUcs.CalculateGridPoint(row, col)));
                        Point3d point = _innerGridUcs.CalculateGridPoint(row, col);
                        BlockReference br = lableFactory.CreateItem(point, Matrix3d.Identity).GetObjectForRead<BlockReference>();
                        Entities.Add((Entity)br.Clone());
                        br.Erase(true);
                    }
                }
                Tools.GetAcadDatabase().TransactionManager.QueueForGraphicsFlush();
            });
        }

        public override void Calculate()
        {
            Calculate(_jigPositionUcs);
            _setEntitiesFromInnerGrid();
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите точку");
            ppo.UseBasePoint = true;
            ppo.BasePoint = InsertPointUcs.TransformBy(ToWcsTransform);

            ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

            PromptPointResult ppr = prompts.AcquirePoint(ppo);

            if (ppr.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;

            if ((_jigPositionUcs - ppr.Value.TransformBy(ToUcsTransform)).Length < 0.1*_step)
                return SamplerStatus.NoChange;

            _jigPositionUcs = ppr.Value.TransformBy(ToUcsTransform);

            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            return base.WorldDraw(draw);
        }


        private class CoordinateLable
        {
            private double _size;
            private double _textHeight;
            private string _format;
            private System.Globalization.CultureInfo _culture;

            private Line _horizontalLine;
            private Line _verticalLine;
            AttributeDefinition _eastCoordText;
            AttributeDefinition _northCoordText;
            List<Entity> _entities;
            private Point3d _origin;
            private Matrix3d _blockTransform;
            private ObjectId _btrId;


            public CoordinateLable(double size = 5d, double textHeight = 2.5d)
            {
                _size = size;
                //_format = "##,#";
                _format = "#0";
                _textHeight = textHeight;
                _blockTransform = Matrix3d.Identity;
                _entities = new List<Entity>();
                _culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }

            public void _calculate()
            {
                _entities.ForEach(ent => ent.Dispose());
                _entities.Clear();

                _createLines();
                _createAttributes();

                _btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", _origin, _entities, AnnotativeStates.True);
            }

            public ObjectId CreateItem(Point3d insertPointUcs, Matrix3d ucs)
            {
                if (_btrId == ObjectId.Null)
                    _calculate();

                Matrix3d rot = ucs.PreMultiplyBy(Matrix3d.Displacement(_origin - ucs.CoordinateSystem3d.Origin));
                ObjectId brId = AcadBlocks.BlockTools.AppendBlockItem(insertPointUcs, _btrId, null, rot);
                Tools.StartTransaction(() =>
                    {
                        var br = brId.GetObjectForRead<BlockReference>();
                        var ar = br.GetAttributeByTag("Восток");
                        ar.UpgradeOpen();
                        ar.TextString = br.Position.X.ToString(_format, _culture);

                        ar = br.GetAttributeByTag("Север");
                        ar.UpgradeOpen();
                        ar.TextString = br.Position.Y.ToString(_format, _culture);

                        br.UpgradeOpen();
                        br.RecordGraphicsModified(true);
                    });

                return brId;
            }

            private void _createLines()
            {
                _horizontalLine = new Line(_origin, _origin.Add(_blockTransform.CoordinateSystem3d.Xaxis).MultiplyBy(_size));
                _horizontalLine.TransformBy(Matrix3d.Displacement(_blockTransform.CoordinateSystem3d.Xaxis.Negate().MultiplyBy(_size / 2d)));

                _verticalLine = new Line(_origin, _origin.Add(_blockTransform.CoordinateSystem3d.Yaxis).MultiplyBy(_size));
                _verticalLine.TransformBy(Matrix3d.Displacement(_blockTransform.CoordinateSystem3d.Yaxis.Negate().MultiplyBy(_size / 2d)));

                /*_horizontalLine.LineWeight = LineWeight.LineWeight015;
                _verticalLine.LineWeight = LineWeight.LineWeight015;*/

                _entities.AddRange(new[] { _horizontalLine, _verticalLine });
            }

            private void _createAttributes()
            {
                _eastCoordText = new AttributeDefinition();
                _eastCoordText.SetDatabaseDefaults();

                _eastCoordText.Verifiable = true;
                _eastCoordText.TextString = "";
                _eastCoordText.Tag = "Восток";
                _eastCoordText.Rotation = Math.PI / 2d;
                _eastCoordText.Height = _textHeight;
                _eastCoordText.Position = _origin.Add(_verticalLine.GetFirstDerivative(0).Normalize().MultiplyBy(0.3 * _textHeight).
                    Add(_verticalLine.GetFirstDerivative(0).GetPerpendicularVector().Normalize().MultiplyBy(0.1 * _textHeight)));
                _entities.Add(_eastCoordText);


                _northCoordText = new AttributeDefinition();
                _northCoordText.SetDatabaseDefaults();

                _northCoordText.Verifiable = true;
                _northCoordText.TextString = "";
                _northCoordText.Tag = "Север";
                _northCoordText.Rotation = 0d;
                _northCoordText.Height = _textHeight;
                _northCoordText.Position = _origin.Add(_horizontalLine.GetFirstDerivative(0).Normalize().MultiplyBy(0.3 * _textHeight).
                    Add(_horizontalLine.GetFirstDerivative(0).GetPerpendicularVector().Normalize().MultiplyBy(0.1 * _textHeight)));
                _entities.Add(_northCoordText);
            }
        }

        private class CoordinateLableEx:CustomObjects.MultiEntity
        {
            private double _size;
            private double _textHeight;
            private string _format;
            private System.Globalization.CultureInfo _culture;

            private Line _horizontalLine;
            private Line _verticalLine;
            DBText _northCoordText;
            DBText _eastCoordText;


            public CoordinateLableEx(Point3d insertPointUcs, double size = 2d, double textHeight = 2.5d)
                :base(Point3d.Origin, Point3d.Origin, new List<Entity>(), AnnotativeStates.True, Matrix3d.Identity)
            {
                _size = size;
                _format = "##,#";
                _textHeight = textHeight;
                _culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }

            public override void Calculate()
            {
                Entities.ForEach(ent => ent.Dispose());
                Entities.Clear();

                _createLines();
                _createText();
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                throw new NotImplementedException();
            }


            private void _createLines()
            {
                _horizontalLine = new Line(Origin, Origin.Add(InnerBlockTransform.CoordinateSystem3d.Xaxis).MultiplyBy(_size));
                _verticalLine = new Line(Origin, Origin.Add(InnerBlockTransform.CoordinateSystem3d.Yaxis).MultiplyBy(_size));
                Entities.AddRange(new[] { _horizontalLine, _verticalLine });;
            }

            private void _createText()
            {
                _northCoordText = new DBText();
                _northCoordText.SetDatabaseDefaults();

                _northCoordText.TextString = InsertPointUcs.Y.ToString(_format);
                _northCoordText.Rotation = Math.PI / 2d;
                _northCoordText.Height = _textHeight;
                _northCoordText.Position = Origin.Add(_verticalLine.GetFirstDerivative(0).Normalize().MultiplyBy(0.1*_textHeight).
                    Add(_verticalLine.GetFirstDerivative(0).GetPerpendicularVector().Normalize().MultiplyBy(0.1*_textHeight)));
                Entities.Add(_northCoordText);


                _eastCoordText = new DBText();
                _eastCoordText.SetDatabaseDefaults();

                _eastCoordText.TextString = InsertPointUcs.Y.ToString(_format);
                _eastCoordText.Rotation = 0d;
                _eastCoordText.Height = _textHeight;
                _eastCoordText.Position = Origin.Add(_horizontalLine.GetFirstDerivative(0).Normalize().MultiplyBy(0.1 * _textHeight).
                    Add(_horizontalLine.GetFirstDerivative(0).GetPerpendicularVector().Normalize().MultiplyBy(0.1 * _textHeight)));
                Entities.Add(_eastCoordText);
            }
        }
    }
}
