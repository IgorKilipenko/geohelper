using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using display = IgorKL.ACAD3.Model.Helpers.Display;
using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing.Arrows
{
    public class ValueArrow : CustomObjects.EntityDrawer
    {


        private static MainMenu.HostProvider _dataProvider = new MainMenu.HostProvider(new ValueArrow());

        private Vector3d _axisVector;
        private Point3d _insertPointUcs;
        private Point3d _destLowerPointUcs;
        private Point3d _jigLowerPointUcs;
        private WallDeviationArrows.Arrow _arrow;

        private bool _destLowerPointComplete;
        private bool _jigLowerPointComplete;

        private double _toleranceBottom;
        private bool _isToleranceOnly;

        /*private Polyline _baseLine;*/

        #region Cotors
        public ValueArrow()
            : this(Matrix3d.Identity.CoordinateSystem3d.Xaxis, Matrix3d.Identity)
        {
        }
        public ValueArrow(Vector3d axisVector, Matrix3d ucs)
            :base(new List<Entity>(), AnnotativeStates.True, ucs)
        {
            _axisVector = axisVector;

            if (_dataProvider == null)
                _dataProvider = new MainMenu.HostProvider(this);
            _toleranceBottom = _dataProvider.Read("tolerance", 0.005d);
            _isToleranceOnly = _dataProvider.Read("isToleranceOnly", false);
        }
        #endregion

        #region Properties
        public Matrix3d TransformToArrowBlock
        {
            get { return Ucs.PreMultiplyBy(Matrix3d.Displacement(Point3d.Origin - _insertPointUcs.TransformBy(Ucs))); }
        }
        /// <summary>
        /// Определяет метод отрисовки стрелок, если значение ИСТИНА величина отклонений будет ограничена допустимым пределом
        /// </summary>
        public bool IsToleranceOnly
        {
            get { return _isToleranceOnly; }
        }
        #endregion

        #region Commands
        [RibbonCommandButton("Стрелки отклонения", "Стрелки")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawWallValueArrows", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
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

            using (CustomObjects.EntityDrawer grphic = new ValueArrow())
            {
                //grphic.TrasientDisplay(new[] { new Line(axisVectorPoints[0], axisVectorPoints[1]) });
                if (transientPoints != null && transientPoints.Length >= 2)
                    grphic.TrasientDisplay(new[] { new Line(transientPoints[0], transientPoints[1]) });
                DrawWallArrows(axisVector, ucs);
            }
            //grphic.Dispose();
        }
        /*Переход на полилинию на перспективу
        [RibbonCommandButton("Стрелки отклонения", "Стрелки")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawValueArrows", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawValueArrows()
        {
            Matrix3d ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            double tolerance = _dataProvider.Read("tolerance", 0.005d);
            bool isToleranceOnly = _dataProvider.Read("isToleranceOnly", false);

            Polyline pline;
            if (!ObjectCollector.TrySelectAllowedClassObject(out pline))
                return;


            PromptPointOptions ppo = new PromptPointOptions("\nУкажите фактическое положение:");
            ppo.AllowArbitraryInput = false;
            ppo.Keywords.Add("Tolerance", "ДОПуск", "ДОПуск", true, true);
            ppo.Keywords.Add("IsToleranceOnlyTrue", "ТОЛькоВДопуске", "ТОЛько В Допуске", true, true);
            ppo.Keywords.Add("IsToleranceOnlyFalse", "ФАКТически", "ФАКТические данные", true, true);
            ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);

            PromptPointResult ppr;
            while ((ppr = Tools.GetAcadEditor().GetPoint(ppo)).Status == PromptStatus.OK || ppr.Status == PromptStatus.Keyword)
            {
                var insertPoint = pline.GetOrthoNormalPoint(ppr.Value, new Plane());
                if (insertPoint == null || !insertPoint.HasValue)
                    continue;
                try
                {
                    Vector3d axisVector = pline.GetFirstDerivative(insertPoint.Value);
                    ValueArrow mainBlock = new ValueArrow(axisVector, ucs);
                    mainBlock._isToleranceOnly = isToleranceOnly;
                    mainBlock._toleranceBottom = tolerance;
                    mainBlock._insertPointUcs = insertPoint.Value;

                    PromptStatus res = PromptStatus.Cancel;
                    if ((res = mainBlock.JigDraw()) != PromptStatus.OK)
                        return;

                    _dataProvider.Write("tolerance", mainBlock._toleranceBottom);
                    _dataProvider.Write("isToleranceOnly", mainBlock._isToleranceOnly);
                }
                catch (Exception ex)
                {
                    Tools.GetAcadEditor().WriteMessage($"\n\aОшибка \n{ex.Message}");
                    return;
                }
            }

        }
        */
        #endregion

        /// <summary>
        /// Основной метод вывода данных. Рисует стрелки по полученным с экрана точкам
        /// </summary>
        /// <param name="axisVector">Вектор направления оси/грани (перпендикулярно стрелкам)</param>
        /// <param name="ucs">Текущая ПСК</param>
        /// <param name="onlyOnce">ИСТИНА если нужно выполнить только раз, иначе цикл</param>
        /// <returns></returns>
        public static PromptStatus DrawWallArrows(Vector3d axisVector, Matrix3d ucs, bool onlyOnce = false, bool mirrorText = false)
        {
            double toleranceBottom = /*0.005;*/ _dataProvider.Read("tolerance", 0.005d);
            bool isToleranceOnly = /*false;*/ _dataProvider.Read("isToleranceOnly", false);
            object mirrorTextValue = null;
            if (mirrorText)
                mirrorTextValue = SetMirrorTextValue(1);
            try
            {
                Point3d? insertPoint = Point3d.Origin;
                while ((insertPoint = GetInsertPoint(axisVector, ucs, ref toleranceBottom, ref isToleranceOnly)).HasValue)
                {
                    PromptStatus res = PromptStatus.Cancel;

                    ValueArrow mainBlock = new ValueArrow(axisVector, ucs);

                    mainBlock._isToleranceOnly = isToleranceOnly;
                    mainBlock._toleranceBottom = toleranceBottom;

                    mainBlock._insertPointUcs = insertPoint.Value;
                    if ((res = mainBlock.JigDraw()) != PromptStatus.OK)
                        return res;
                    if (onlyOnce)
                        break;

                    _dataProvider.Write("tolerance", mainBlock._toleranceBottom);
                    _dataProvider.Write("isToleranceOnly", mainBlock._isToleranceOnly);
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
                if (mirrorText)
                    SetMirrorTextValue(mirrorTextValue);
            }
        }

        /// <summary>
        /// Основной метод расчета
        /// </summary>
        public override void Calculate()
        {
            _clearEntities();

            if (!_destLowerPointComplete)
            {
                DisplayArrow(_destLowerPointUcs);
            }
            else if (!_jigLowerPointComplete)
            {
                DisplayRedirectedArrow(_jigLowerPointUcs);
            }
        }

        public void DisplayArrow(Point3d destationPointUcs)
        {
            _arrow = new WallDeviationArrows.Arrow(_axisVector, 5, 0.5);

            double value = _arrow.Calculate(destationPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttribute(_arrow.ArrowLine.GetCenterPoint(), null, _arrow.LineTarnsform).ToList();

            _arrow.AppendArrowSymbols(symbs);

            symbs.Add((Entity)_arrow.ArrowLine.Clone());


            if (Math.Abs(value) > _toleranceBottom)
            {
                if (_isToleranceOnly)
                {
                    value = (double)Math.Sign(value) * _toleranceBottom;
                    _arrow.LastValue = value;
                }
                _arrow.Highlight = true;
            }

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_", Math.Abs((value * 1000)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);
        }

        public void DisplayRedirectedArrow(Point3d jigPointUcs)
        {
            _arrow.Redirect(jigPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttribute(_arrow.ArrowLine.GetCenterPoint(), null, _arrow.LineTarnsform).ToList();
            symbs.Add((Entity)_arrow.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_", Math.Abs((_arrow.LastValue.Value * 1000d)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);
        }

        #region JigDrawing
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите фактическое положение:");
            ppo.UseBasePoint = true;
            ppo.BasePoint = _insertPointUcs.TransformBy(_ucs);
            ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

            if (!_destLowerPointComplete)
            {
                PromptPointResult ppr = prompts.AcquirePoint(ppo);
                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                _destLowerPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                return SamplerStatus.OK;
            }

            else
            {
                if (!_jigLowerPointComplete)
                {
                    ppo.Message = "\nУкажите место отрисовки";
                    PromptPointResult ppr = prompts.AcquirePoint(ppo);
                    if (ppr.Status != PromptStatus.OK)
                        return SamplerStatus.Cancel;

                    _jigLowerPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                    return SamplerStatus.OK;
                }
                else
                {
                    return SamplerStatus.Cancel;

                }

            }

        }

        public override PromptStatus JigDraw()
        {
            if (!_destLowerPointComplete)
            {
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
                _destLowerPointComplete = true;
            }
            if (!_jigLowerPointComplete)
            {
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
                _jigLowerPointComplete = true;
            }
            if (base.JigDraw() != PromptStatus.OK)
                return PromptStatus.Cancel;

            Tools.AppendEntity(Entities);

            Tools.GetAcadDatabase().TransactionManager.QueueForGraphicsFlush();
            Tools.GetAcadEditor().UpdateScreen();
            return PromptStatus.OK;
        }
        #endregion

        /// <summary>
        /// Механизм определения базового вектора для отрисовки стрелок (XLine)
        /// </summary>
        /// <param name="ucs"></param>
        /// <returns></returns>
        public static Point3d[] GetAxisVectorCmd(CoordinateSystem3d ucs)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку на проектной оси/грани [ОсьХ/ОсьУ/Указать/Выход]<X>:");
            ppo.Keywords.Add("Xaxis", "ХОсь", "ХОсь", true, true);
            ppo.Keywords.Add("Yaxis", "УОсь", "УОсь", true, true);
            ppo.Keywords.Add("ENter", "УКазать", "УКазать", true, true);
            ppo.Keywords.Add("EXIt", "ВЫХод", "ВЫХод", true, true);
            ppo.AllowArbitraryInput = true;

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status == PromptStatus.Keyword)
            {
                switch (ppr.StringResult)
                {
                    case "Xaxis":
                        {
                            return _vectorToScreen(ucs.Xaxis);
                        }
                    case "Yaxis":
                        {
                            return _vectorToScreen(ucs.Yaxis);
                        }
                    case "Enter":
                        {
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            if (ppr.Status != PromptStatus.OK)
                                return null;
                            break;
                        }
                    case "Exit":
                        {
                            return null;
                        }
                }
            }

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

        /// <summary>
        /// Промпт для получения начальных данных рисования стрелок (основное меню)
        /// </summary>
        /// <param name="axisVector">Вектор, определяющий проектное положение конструкции</param>*/
        /// <param name="ucs">Текущая ПСК</param>
        /// <param name="bottomTolerance">Допустимое отклонение по низу конструкции (от оси/точки вставки)</param>
        /// <param name="topTolerance">Допустимое отклонение по верху конструкции (от вертикали)</param>
        /// <param name="isTopMaxTolerance">Определяет метод определения допуска отклонения по верху вертикального сооружения</param>
        /// <param name="isToleranceOnly">Определяет метод отрисовки стрелок, если значение ИСТИНА величина отклонений будет ограничена допустимым пределом</param>
        /// <returns>Точка вставки, если NULL - выход</returns>
        public static Point3d? GetInsertPoint(Vector3d axisVector, Matrix3d ucs, ref double bottomTolerance, ref bool isToleranceOnly)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку вставки/проектное положение");
            //ppo.Keywords.Add("Perpendicular", "Перпендикуляр", "Перпендикуляр", true, true);
            ppo.Keywords.Add("Tolerance", "ДОПуск", "ДОПуск", true, true);
            ppo.Keywords.Add("IsToleranceOnlyTrue", "ТОЛькоВДопуске", "ТОЛько В Допуске", true, true);
            ppo.Keywords.Add("IsToleranceOnlyFalse", "ФАКТически", "ФАКТические данные", true, true);
            ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            ppo.AllowArbitraryInput = true;

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            while (ppr.Status == PromptStatus.Keyword)
            {
                switch (ppr.StringResult)
                {
                    /*case "Perpendicular":
                        {
                            if (DrawWallArrows(_calculateVector(axisVector, ucs, true), ucs, true) == PromptStatus.OK)
                                ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }*/
                    case "Tolerance":
                        {
                            double? toleranse = _promptTolerance("\nУкажите допуск по низу от оси, м", bottomTolerance);
                            if (toleranse.HasValue)
                                bottomTolerance = toleranse.Value;
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "IsToleranceOnlyTrue":
                        {
                            isToleranceOnly = true;
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "IsToleranceOnlyFalse":
                        {
                            isToleranceOnly = false;
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "Exit":
                        {
                            return null;
                        }
                }
            }
            if (ppr.Status != PromptStatus.OK)
                return null;
            return ppr.Value;
        }

        /// <summary>
        /// Пересчитвыет вектор оси для только положительных направлений
        /// </summary>
        /// <param name="vector">Пересчитываемый вектор в системе ucs</param>
        /// <param name="ucs">Система координат определяющая положительные направления осей</param>
        /// <param name="getNormal">Получить ось до правой</param>
        /// <returns>Вектор по направлению сонаправленный с одной из осей системы ucs</returns>
        protected static Vector3d _calculateVector(Vector3d vector, Matrix3d ucs, bool getNormal)
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




        #region Prompts
        protected static double? _promptTolerance(string msg, double defaultValue)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions(msg);
            pdo.AllowNone = false;
            pdo.AllowNegative = false;
            pdo.DefaultValue = defaultValue;
            pdo.UseDefaultValue = true;
            pdo.UseDefaultValue = true;

            PromptDoubleResult pdr = Tools.GetAcadEditor().GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return null;
            return pdr.Value;
        }
        #endregion

        #region Helpers

        
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
        /// Очищает список приметивов
        /// </summary>
        protected void _clearEntities()
        {
            Tools.StartTransaction(() =>
            {
                if (Entities.FirstOrDefault(ent => ent is BlockReference) != null)
                    ((BlockReference)Entities.First()).BlockTableRecord.GetObjectForWrite<BlockTableRecord>().DeepErase(true);
            });
            Entities.ForEach(ent => ent.Dispose());
            Entities.Clear();
        }

        private void _setEntitiesToBlock(Point3d insertPointUcs, IEnumerable<Entity> entities, Dictionary<string, string> attrInfo, bool erase)
        {
            ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", Point3d.Origin, entities, Annotative);
            ObjectId brId = AcadBlocks.BlockTools.AppendBlockItem(insertPointUcs.TransformBy(Ucs), btrId, attrInfo);

            Tools.StartTransaction(() =>
            {
                BlockReference br = brId.GetObjectForRead<BlockReference>();
                br.UpgradeOpen();
                if (_arrow != null)
                    _arrow.SaveToEntity(br);
                br.RecordGraphicsModified(true);

                Entities.Add((Entity)br.Clone());
                if (erase)
                {
                    br.Erase(true);
                }
            });

        }

        [Obsolete("Требует доработки, изменить механизм используя XLine")]
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

        [Obsolete("Требует доработки, изменить механизм используя XLine")]
        private static Point3d[] _vectorToScreen(Vector3d vector)
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

                    Point2d topLeft = viewCenter + new Vector2d(-viewWidth / 2, viewHeight / 2);
                    Point2d topRight = viewCenter + new Vector2d(viewWidth / 2, viewHeight / 2);
                    Point2d bottomLeft = viewCenter + new Vector2d(-viewWidth / 2, -viewHeight / 2);
                    Point2d bottomRight = viewCenter + new Vector2d(viewWidth / 2, -viewHeight / 2);

                    Point3d point = bottomLeft.Convert3d().Add((topRight.Convert3d() - bottomLeft.Convert3d()).MultiplyBy(0.1));
                    //res = _vectorToScreen(point.TransformBy(matDcsToWcs), vector);

                    Polyline vpRectg = new Polyline(5);
                    vpRectg.AddVertexAt(0, bottomLeft, 0, 0, 0);
                    vpRectg.AddVertexAt(1, bottomRight, 0, 0, 0);
                    vpRectg.AddVertexAt(2, topRight, 0, 0, 0);
                    vpRectg.AddVertexAt(3, topLeft, 0, 0, 0);
                    vpRectg.AddVertexAt(4, bottomLeft, 0, 0, 0);

                    Polyline line = new Polyline(2);
                    line.AddVertexAt(0, point, 0, 0, 0);
                    line.AddVertexAt(1, point.Add(vector), 0, 0, 0);

                    var intersects = line.IntersectWith(vpRectg, Intersect.ExtendThis);
                    res = intersects.Select(p => p.TransformBy(matDcsToWcs)).ToArray();
                }
            });

            if (res == null || res.Length != 2)
                return null;
            return res;
        }

        
        private IEnumerable<Entity> _createAttribute(Point3d alignmentPoint, string prefix, Matrix3d transform)
        {
            alignmentPoint = alignmentPoint.TransformBy(Matrix3d.Displacement(transform.CoordinateSystem3d.Yaxis.MultiplyBy(2.0d * 0.2)));

            AttributeDefinition ad = new AttributeDefinition();
            ad.SetDatabaseDefaults(Tools.GetAcadDatabase());
            ad.Verifiable = true;
            ad.Tag = "отклонение_" + (string.IsNullOrWhiteSpace(prefix) ? "" : prefix);
            ad.TextString = "12";
            ad.Annotative = AnnotativeStates.False;
            ad.Height = 2.0d;
            ad.HorizontalMode = TextHorizontalMode.TextCenter; //TextHorizontalMode.TextLeft;
            ad.VerticalMode = TextVerticalMode.TextBottom;
            ad.Position = Point3d.Origin;
            ad.TransformBy(transform);
            ad.Position = alignmentPoint;
            ad.AlignmentPoint = alignmentPoint;
            ad.AdjustAlignment(Tools.GetAcadDatabase());


            DBText adPrefix = new DBText();
            Polyline bound = new Polyline(5);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                //DBText adPrefix = new DBText();
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
                //Polyline bound = new Polyline(5);
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

                adPrefix.Height = 1.7;
                adPrefix.AdjustAlignment(Tools.GetAcadDatabase());
                vector = rectg.Value.UpperLeft - rectg.Value.LowerRight;
                vector = vector.TransformBy(transform);
                rectg = adPrefix.GetTextBoxCorners();
                Vector3d vector2 = rectg.Value.UpperLeft - rectg.Value.LowerRight;
                adPrefix.TransformBy(Matrix3d.Displacement(vector2.Normalize().MultiplyBy((vector.Length - vector2.Length) / 2d)));
                adPrefix.AdjustAlignment(Tools.GetAcadDatabase());

            }
            yield return ad;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                yield return adPrefix;
                yield return bound;
            }
        }
        

        #endregion
    }


}
