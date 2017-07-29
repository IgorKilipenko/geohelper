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
    public class WallDeviationArrows2 : CustomObjects.EntityDrawer
    {
        private Vector3d _axisVector;
        private Point3d _insertPointUcs;
        private Point3d _destLowerPointUcs;
        //private Point3d _jigLowerPointUcs;
        private Point3d _destUpperPointUcs;
        private Point3d _jigUpperPointUcs;
        private Arrow _arrowLower;
        private Arrow _arrowUpper;

        private bool _destLowerPointComplite;
        //private bool _jigLowerPointComplite;
        private bool _destUpperPointComplite;
        private bool _jigUpperPointComplite;

        #region Ctors
        public WallDeviationArrows2()
            : this(Matrix3d.Identity.CoordinateSystem3d.Xaxis, Matrix3d.Identity)
        {
        }

        public WallDeviationArrows2(Vector3d axisVector, Matrix3d ucs)
            : base(new List<Entity>(), AnnotativeStates.True, ucs)
        {
            _axisVector = axisVector;
        }
        #endregion

        public Matrix3d TransformToArrowBlock
        {
            get { return Ucs.PreMultiplyBy(Matrix3d.Displacement(Point3d.Origin - _insertPointUcs.TransformBy(Ucs))); }
        }

#if DEBUG


        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_DrawWallArrows2", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawWallArrows()
        {
            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();

            Point3d[] axisVectorPoints = GetAxisVectorCmd(ucs.CoordinateSystem3d);
            if (axisVectorPoints == null)
                return;

            axisVectorPoints[0] = axisVectorPoints[0].TransformBy(ucs);
            axisVectorPoints[1] = axisVectorPoints[1].TransformBy(ucs);

            Vector3d axisVector = axisVectorPoints[1] - axisVectorPoints[0];
            axisVector = _calculateVector(axisVector, ucs.Inverse(), false);
            Point3d[] transientPoints = _vectorToScreen(axisVectorPoints[0], axisVector);

            CustomObjects.EntityDrawer grphic = new WallDeviationArrows();
            //grphic.TrasientDisplay(new[] { new Line(axisVectorPoints[0], axisVectorPoints[1]) });
            if (transientPoints != null && transientPoints.Length >= 2)
                grphic.TrasientDisplay(new[] { new Line(transientPoints[0], transientPoints[1]) });
            DrawWallArrows(axisVector, ucs);

            grphic.Dispose();
        }
#endif

        /// <summary>
        /// Основной метод выводв данных
        /// </summary>
        /// <param name="axisVector"></param>
        /// <param name="ucs"></param>
        /// <param name="onlyOnce"></param>
        /// <returns></returns>
        public static PromptStatus DrawWallArrows(Vector3d axisVector, Matrix3d ucs, bool onlyOnce = false)
        {
            object mirrorTextValue = SetMirrorTextValue(1);
            try
            {
                Point3d? insertPoint = Point3d.Origin;
                while ((insertPoint = GetInsertPoint(axisVector, ucs)).HasValue)
                {
                    PromptStatus res = PromptStatus.Cancel;

                    WallDeviationArrows2 mainBlock = new WallDeviationArrows2(axisVector, ucs);
                    mainBlock._insertPointUcs = insertPoint.Value;
                    if ((res = mainBlock.JigDraw()) != PromptStatus.OK)
                        return res;
                    if (onlyOnce)
                        break;
                }

                return PromptStatus.OK;
            }
            catch (Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage(ex.Message);
                return PromptStatus.Error;
            }
            finally
            {
                SetMirrorTextValue(mirrorTextValue);
            }
        }

        /// <summary>
        /// Включает/выключает механизм аввтоматического преобразовании текста при зеркальном отражении (0 - вкл / 1 - выкл)
        /// </summary>
        /// <param name="value">0 - вкл / 1 - выкл</param>
        /// <returns>значение предопределенное в среде</returns>
        public static object SetMirrorTextValue(object value)
        {
            const string MIRRTEXT = "MIRRTEXT";
            object defVal = Application.GetSystemVariable(MIRRTEXT);
            Application.SetSystemVariable(MIRRTEXT, value);
            return defVal;
        }

        /// <summary>
        /// Основной метод расчета
        /// </summary>
        public override void Calculate()
        {
            Tools.StartTransaction(() =>
            {
                if (Entities.FirstOrDefault(ent => ent is BlockReference) != null)
                    ((BlockReference)Entities.First()).BlockTableRecord.GetObjectForWrite<BlockTableRecord>().DeepErase(true);
            });
            Entities.ForEach(ent => ent.Dispose());
            Entities.Clear();

            if (!_destLowerPointComplite)
            {
                DisplayLowerArrow(_destLowerPointUcs);
            }
            else
            {
                if (!_destUpperPointComplite)
                    DisplayUpperArrow(_destUpperPointUcs);
                else
                {
                    if (!_jigUpperPointComplite)
                        DisplayRedirectedUpperArrow(_jigUpperPointUcs);
                }
            }
        }




        #region Jig Overrides
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите фактическое положение низа");
            ppo.UseBasePoint = true;
            ppo.BasePoint = _insertPointUcs.TransformBy(_ucs);
            ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

            if (!_destLowerPointComplite)
            {
                PromptPointResult ppr = prompts.AcquirePoint(ppo);
                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                if ((_destLowerPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.00001)
                    return SamplerStatus.NoChange;
                _destLowerPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                return SamplerStatus.OK;
            }

            else
            {

                if (!_destUpperPointComplite)
                {
                    ppo.Message = "\nУкажите фактическое положение верха";
                    PromptPointResult ppr = prompts.AcquirePoint(ppo);
                    if (ppr.Status != PromptStatus.OK)
                        return SamplerStatus.Cancel;

                    if ((_destUpperPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.01)
                        return SamplerStatus.NoChange;
                    _destUpperPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                    return SamplerStatus.OK;
                }
                else
                {
                    if (!_jigUpperPointComplite)
                    {
                        ppo.Message = "\nУкажите место отрисовки";
                        PromptPointResult ppr = prompts.AcquirePoint(ppo);
                        if (ppr.Status != PromptStatus.OK)
                            return SamplerStatus.Cancel;

                        if ((_jigUpperPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.01)
                            return SamplerStatus.NoChange;
                        _jigUpperPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                        return SamplerStatus.OK;
                    }
                    return SamplerStatus.Cancel;
                }

            }

        }
        public override PromptStatus JigDraw()
        {
            if (base.JigDraw() != PromptStatus.OK)
                return PromptStatus.Cancel;
            _destLowerPointComplite = true;

            if (base.JigDraw() != PromptStatus.OK)
                return PromptStatus.Cancel;
            _destUpperPointComplite = true;

            if (_arrowUpper.IsCodirectional)
            {
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
            }
            _jigUpperPointComplite = true;

            Tools.AppendEntity(Entities);

            Tools.GetAcadDatabase().TransactionManager.QueueForGraphicsFlush();
            Tools.GetAcadEditor().UpdateScreen();
            return PromptStatus.OK;
        }
        #endregion



        #region Acad I/O Methods

        public static Point3d[] GetAxisVectorCmd(CoordinateSystem3d ucs)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите первую точку положения оси/грани");
            ppo.Keywords.Add("Xaxis", "ОсьХ", "Ось Х", true, true);
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return null;

            ppo = new PromptPointOptions("\nУкажите вторую точку положения оси/грани");
            ppo.UseBasePoint = true;
            ppo.BasePoint = ppr.Value;
            ppo.UseDashedLine = true;
            ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return null;

            var res = new[] { ppo.BasePoint, ppr.Value };
            return res;
        }
        public static Point3d[] GetAxisVectorCmd2(CoordinateSystem3d ucs)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку на проектной оси/грани [ОсьХ/ОсьУ/Указать/Выход]<X>:");
            ppo.Keywords.Add("Xaxis", "ОсьХ", "Ось Х", true, true);
            ppo.Keywords.Add("Yaxis", "ОсьУ", "Ось У", true, true);
            ppo.Keywords.Add("Enter", "Указать", "Указать", true, true);
            ppo.Keywords.Add("Exit", "Выход", "Выход", true, true);
            ppo.AllowArbitraryInput = true;

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status == PromptStatus.Keyword)
            {
                switch (ppr.StringResult)
                {
                    case "Xaxis":
                        {
                            return null;
                        }
                    case "Yaxis":
                        {
                            return null;
                        }
                    case "Enter":
                        {
                            ppo = new PromptPointOptions("\nУкажите вторую точку положения оси/грани");
                            ppo.UseBasePoint = true;
                            ppo.BasePoint = ppr.Value;
                            ppo.UseDashedLine = true;
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            if (ppr.Status != PromptStatus.OK)
                                return null;

                            var res = new[] { ppo.BasePoint, ppr.Value };
                            return res;
                        }
                    case "Exit":
                        {
                            return null;
                        }
                }
            }
            return null;
        }
        public void DisplayLowerArrow(Point3d destationPointUcs)
        {
            _arrowLower = new Arrow(_axisVector);
            double value = _arrowLower.Calculate(destationPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttrute(_arrowLower.ArrowLine.GetCenterPoint(), "Н", _arrowLower.LineTarnsform).ToList();

            _arrowLower.AppendArrowSymbols(symbs);

            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((value * 1000)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);
        }

        public void DisplayUpperArrow(Point3d destationPointUcs)
        {
            _arrowUpper = new Arrow(_arrowLower);
            double value = _arrowUpper.Calculate(destationPointUcs.TransformBy(TransformToArrowBlock));

            var symbs = _createAttrute(_arrowUpper.ArrowLine.GetCenterPoint(), "В", _arrowUpper.LineTarnsform).ToList();
            symbs.Add((Entity)_arrowUpper.ArrowLine.Clone());

            symbs.AddRange(_arrowLower.ArrowSymbols.Select(ent => (Entity)ent.Clone()));
            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((_arrowLower.LastValue.Value * 1000)).ToString("#0"));    //
            attrInfo.Add("отклонение_В", Math.Abs((value * 1000)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);
        }
        public void DisplayRedirectedUpperArrow(Point3d jigPointUcs)
        {
            _arrowUpper.Redirect(jigPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttrute(_arrowUpper.ArrowLine.GetCenterPoint(), "В", _arrowUpper.LineTarnsform).ToList();
            symbs.Add((Entity)_arrowUpper.ArrowLine.Clone());

            symbs.AddRange(_arrowUpper.BaseArrow.ArrowSymbols.Select(ent => (Entity)ent.Clone()));
            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((_arrowLower.LastValue.Value * 1000)).ToString("#0"));    //
            attrInfo.Add("отклонение_В", Math.Abs((_arrowUpper.LastValue.Value * 1000)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);
        }


        public static Point3d? GetInsertPoint(Vector3d axisVector, Matrix3d ucs)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку вставки/проектное положение");
            ppo.Keywords.Add("Perpendicular", "Перпендикуляр", "Перпендикуляр", true, true);
            ppo.Keywords.Add("Exit", "Выход", "Выход", true, true);
            ppo.AllowArbitraryInput = true;
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status == PromptStatus.Keyword)
            {
                if (ppr.StringResult == "Exit")
                    return null;
                if (ppr.StringResult == "Perpendicular")
                {
                    if (DrawWallArrows(_calculateVector(axisVector, ucs, true), ucs, true) == PromptStatus.OK)
                        ppr = Tools.GetAcadEditor().GetPoint(ppo);
                }
            }
            if (ppr.Status != PromptStatus.OK)
                return null;
            return ppr.Value;
        }

        #endregion




        #region Helpers
        private void _setEntitiesToBlock(Point3d insertPointUcs, IEnumerable<Entity> entities, Dictionary<string, string> attrInfo, bool erase)
        {
            ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", Point3d.Origin, entities, Annotative);
            ObjectId brId = AcadBlocks.BlockTools.AppendBlockItem(insertPointUcs.TransformBy(Ucs), btrId, attrInfo);

            Tools.StartTransaction(() =>
            {
                BlockReference br = brId.GetObjectForRead<BlockReference>();
                br.UpgradeOpen();
                if (_arrowUpper != null)
                    _arrowUpper.SaveToEntity(br);
                br.RecordGraphicsModified(true);
                Entities.Add((Entity)br.Clone());
                if (erase)
                {
                    br.Erase(true);
                }
            });

        }
        private IEnumerable<Entity> _createAttrute(Point3d alignmentPoint, string prefix, Matrix3d transform)
        {
            alignmentPoint = alignmentPoint.TransformBy(Matrix3d.Displacement(transform.CoordinateSystem3d.Yaxis.MultiplyBy(2.0d * 0.2)));

            AttributeDefinition ad = new AttributeDefinition();
            ad.SetDatabaseDefaults(Tools.GetAcadDatabase());
            ad.Verifiable = true;
            ad.Tag = "отклонение_" + prefix;
            ad.TextString = "12";
            ad.Annotative = AnnotativeStates.False;
            ad.Height = 2.0d;
            ad.HorizontalMode = TextHorizontalMode.TextLeft;
            ad.VerticalMode = TextVerticalMode.TextBottom;
            ad.Position = Point3d.Origin;
            ad.TransformBy(transform);
            ad.Position = alignmentPoint;
            ad.AlignmentPoint = alignmentPoint;
            ad.AdjustAlignment(Tools.GetAcadDatabase());

            DBText adPrefix = new DBText();
            adPrefix.SetDatabaseDefaults(Tools.GetAcadDatabase());
            adPrefix.TextString = prefix;
            adPrefix.Height = 2.0d;
            adPrefix.Annotative = AnnotativeStates.False;
            adPrefix.HorizontalMode = TextHorizontalMode.TextRight;
            adPrefix.VerticalMode = TextVerticalMode.TextBottom;
            adPrefix.Position = Point3d.Origin;
            adPrefix.TransformBy(transform);
            adPrefix.AlignmentPoint = alignmentPoint;
            adPrefix.AdjustAlignment(Tools.GetAcadDatabase());

            Rectangle3d? rectg = adPrefix.GetTextBoxCorners();
            Polyline bound = new Polyline(5);
            bound.AddVertexAt(0, rectg.Value.LowerLeft.Add((rectg.Value.UpperLeft - rectg.Value.LowerLeft).Normalize().Negate().MultiplyBy(adPrefix.Height * 0.1)));
            bound.AddVertexAt(1, rectg.Value.UpperLeft.Add((rectg.Value.UpperLeft - rectg.Value.LowerLeft).Normalize().MultiplyBy(adPrefix.Height * 0.1)));
            bound.AddVertexAt(2, rectg.Value.UpperRight.Add((rectg.Value.UpperRight - rectg.Value.LowerRight).Normalize().MultiplyBy(adPrefix.Height * 0.1)));
            bound.AddVertexAt(3, rectg.Value.LowerRight.Add((rectg.Value.UpperRight - rectg.Value.LowerRight).Normalize().Negate().MultiplyBy(adPrefix.Height * 0.1)));
            bound.AddVertexAt(4, rectg.Value.LowerLeft.Add((rectg.Value.UpperLeft - rectg.Value.LowerLeft).Normalize().Negate().MultiplyBy(adPrefix.Height * 0.1)));

            Vector3d vector = rectg.Value.LowerRight - rectg.Value.LowerLeft;
            if (!transform.CoordinateSystem3d.Xaxis.IsCodirectionalTo(vector))
            {
                Plane plane = new Plane(alignmentPoint, transform.CoordinateSystem3d.Yaxis, transform.CoordinateSystem3d.Zaxis);
                Matrix3d mat = Matrix3d.Mirroring(plane);
                ad.TransformBy(mat);
                ad.AdjustAlignment(Tools.GetAcadDatabase());
                adPrefix.TransformBy(mat);
                adPrefix.AdjustAlignment(Tools.GetAcadDatabase());
                bound.TransformBy(mat);
            }

            yield return ad;
            yield return adPrefix;
            yield return bound;
        }

        /// <summary>
        /// Пересчитвыет вектор оси для только положительных направлений
        /// </summary>
        /// <param name="vector">Пересчитываемый вектор в системе ucs</param>
        /// <param name="ucs">Система координат определяющая положительные направления осей</param>
        /// <param name="getNormal">Получить ось до правой</param>
        /// <returns>Вектор по направлению сонаправленный с одной из осей системы ucs</returns>
        private static Vector3d _calculateVector(Vector3d vector, Matrix3d ucs, bool getNormal)
        {
            double angle = vector.GetAngleTo(ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Zaxis.Negate());
            Vector3d resVector = vector;
            if (angle >= Math.PI)
            {
                Matrix3d rot = Matrix3d.Rotation(-Math.PI, ucs.CoordinateSystem3d.Zaxis, ucs.CoordinateSystem3d.Origin);
                resVector = resVector.TransformBy(rot);
                angle = angle - Math.PI;
            }
            if (getNormal)
            {
                if (angle > Math.PI / 2d)
                    angle = -Math.PI / 2d;
                else
                    if (angle <= Math.PI / 2d)
                        angle = Math.PI / 2d;
                Matrix3d rot = Matrix3d.Rotation(angle, ucs.CoordinateSystem3d.Zaxis, ucs.CoordinateSystem3d.Origin);
                resVector = resVector.TransformBy(rot);
            }
            return resVector;
        }
        private static Point3d[] _vectorToScreen(Point3d point, Vector3d vector)
        {
            //int nCurVport = System.Convert.ToInt32(Application.GetSystemVariable("CVPORT"));
            Point3d[] res = null;
            Tools.StartTransaction(() =>
            {
                using (var view = Tools.GetAcadEditor().GetCurrentView())
                {

                    Vector3d viewDirection = view.ViewDirection;
                    Point2d viewCenter = view.CenterPoint;
                    Point3d viewTarget = view.Target;
                    double viewTwist = view.ViewTwist;
                    double viewHeight = view.Height;
                    double viewWidth = view.Width;

                    Matrix3d matDcsToWcs = Matrix3d.PlaneToWorld(viewDirection)
                                                    .PreMultiplyBy(Matrix3d.Displacement(viewTarget - Point3d.Origin))
                                                    .PreMultiplyBy(Matrix3d.Rotation(-viewTwist, viewDirection, viewTarget));

                    Point2d topLeft = viewCenter + new Vector2d(-viewWidth /*/ 2*/, viewHeight /*/ 2*/);
                    Point2d topRight = viewCenter + new Vector2d(viewWidth /*/ 2*/, viewHeight /*/ 2*/);
                    Point2d bottomLeft = viewCenter + new Vector2d(-viewWidth /*/ 2*/, -viewHeight /*/ 2*/);
                    Point2d bottomRight = viewCenter + new Vector2d(viewWidth /*/ 2*/, -viewHeight /*/ 2)*/);

                    Polyline vpRectg = new Polyline(5);
                    vpRectg.AddVertexAt(0, bottomLeft, 0, 0, 0);
                    vpRectg.AddVertexAt(1, bottomRight, 0, 0, 0);
                    vpRectg.AddVertexAt(2, topRight, 0, 0, 0);
                    vpRectg.AddVertexAt(3, topLeft, 0, 0, 0);
                    vpRectg.AddVertexAt(4, bottomLeft, 0, 0, 0);

                    vpRectg.TransformBy(matDcsToWcs);

                    Polyline line = new Polyline(2);
                    Matrix3d mat = Matrix3d.Displacement(viewCenter.Convert3d().TransformBy(matDcsToWcs) - point);
                    point = point.TransformBy(mat);
                    line.AddVertexAt(0, point, 0, 0, 0);
                    line.AddVertexAt(1, point.Add(vector), 0, 0, 0);

                    var intersects = line.IntersectWith(vpRectg, Intersect.ExtendThis);
                    intersects = intersects.Select(p => p.TransformBy(mat.Inverse()));
                    res = intersects.ToArray();
                    //Tools.AppendEntity(new[] { vpRectg, line});
                }
            });

            if (res == null || res.Length != 2)
                return null;
            return res;
        }
        #endregion





        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        private class Arrow : CustomObjects.Helpers.CustomObjectSerializer
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
}
