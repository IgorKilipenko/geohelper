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
    public class WallDeviationArrows : CustomObjects.EntityDrawer
    {
        private static MainMenu.HostProvider _dataProvider = new MainMenu.HostProvider(new WallDeviationArrows());

        private Vector3d _axisVector;
        private Point3d _insertPointUcs;
        private Point3d _destLowerPointUcs;
        private Point3d _jigLowerPointUcs;
        private Point3d _destUpperPointUcs;
        private Point3d _jigUpperPointUcs;
        private Arrow _arrowLower;
        private Arrow _arrowUpper;

        private bool _destLowerPointComplete;
        private bool _jigLowerPointComplete;
        private bool _destUpperPointComplete;
        private bool _jigUpperPointComplete;

        private double _toleranceTop;
        private double _toleranceBottom;
        private bool _isToleranceOnly;
        private bool _isTopMaxTolerance;

        #region Ctors
        public WallDeviationArrows()
            :this(Matrix3d.Identity.CoordinateSystem3d.Xaxis, Matrix3d.Identity)
        { 
        }

        public WallDeviationArrows(Vector3d axisVector, Matrix3d ucs)
            :base(new List<Entity>(), AnnotativeStates.True, ucs)
        {
            _axisVector = axisVector;

            if (_dataProvider == null)
                _dataProvider = new MainMenu.HostProvider(this);

            _toleranceTop=_dataProvider.Read("toleranceTop", 0.01d);
            _toleranceBottom = _dataProvider.Read("toleranceBottom", 0.005d);
            _isTopMaxTolerance = _dataProvider.Read("isTopMaxTolerance", false);
            _isToleranceOnly = _dataProvider.Read("isToleranceOnly", false);
        }
        #endregion

        public Matrix3d TransformToArrowBlock
        {
            get { return Ucs.PreMultiplyBy(Matrix3d.Displacement(Point3d.Origin - _insertPointUcs.TransformBy(Ucs))); }
        }
        /// <summary>
        /// Определяет метод определения допуска отклонения по верху вертикального сооружения
        /// </summary>
        public bool IsTopMaxTolerance
        {
            get { return _isTopMaxTolerance; }
        }
        /// <summary>
        /// Определяет метод отрисовки стрелок, если значение ИСТИНА величина отклонений будет ограничена допустимым пределом
        /// </summary>
        public bool IsToleranceOnly
        {
            get { return _isToleranceOnly; }
        }

        #region Commands
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawWallArrows", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
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

            using (CustomObjects.EntityDrawer grphic = new WallDeviationArrows())
            {
                //grphic.TrasientDisplay(new[] { new Line(axisVectorPoints[0], axisVectorPoints[1]) });
                if (transientPoints != null && transientPoints.Length >= 2)
                    grphic.TrasientDisplay(new[] { new Line(transientPoints[0], transientPoints[1]) });
                DrawWallArrows(axisVector, ucs);
            }
            //grphic.Dispose();
        }

        [RibbonCommandButton("Стрелки рандом", "Стрелки")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawWallArrowsRandom", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawWallArrowsRandom()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;

            /////////////////////DATA HOST ADD//////////////////////////////////////////////////////////////////////////////////////////
            /*double toleranceBottom = 0.005;
            double toleranceTop = 0.010;
            bool isTopMaxTolerance = false;
            bool isWall = false;*/
            double toleranceTop = _dataProvider.Read("toleranceTop", 0.01d);
            double toleranceBottom = _dataProvider.Read("toleranceBottom", 0.005d);
            bool isTopMaxTolerance = _dataProvider.Read("isTopMaxTolerance", false);
            bool isWall = /*_dataProvider.Read("isWall", false);*/ false;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            CustomObjects.EntityDrawer grphic;

            

            Vector3d horizontalVector = ucs.CoordinateSystem3d.Xaxis;
            Vector3d verticalVector = ucs.CoordinateSystem3d.Yaxis;
            Vector3d valueVector = new Vector3d(0, 0, 0);
            Random random = new Random(DateTime.Now.Millisecond);

            PromptKeywordOptions pko = new PromptKeywordOptions("\nВыбирите вид [Стены/Колонны/ВЫХод] <Стены>:");
            pko.Keywords.Add("Walls", "Стены","Стены",true,true);
            pko.Keywords.Add("Columns", "Колонны", "Колонны", true, true);
            pko.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);

            PromptResult pkr = Tools.GetAcadEditor().GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK)
                return;
            grphic = new WallDeviationArrows();
            using (grphic)
            {
                switch (pkr.StringResult)
                {
                    case "Walls":
                        {
                            Point3d[] axisVectorPoints = GetAxisVectorCmd(ucs.CoordinateSystem3d);
                            if (axisVectorPoints == null)
                                return;
                            axisVectorPoints[0] = axisVectorPoints[0].TransformBy(ucs);
                            axisVectorPoints[1] = axisVectorPoints[1].TransformBy(ucs);

                            Vector3d axisVector = axisVectorPoints[1] - axisVectorPoints[0];
                            axisVector = _calculateVector(axisVector, ucs.Inverse(), false);
                            Point3d[] transientPoints = _vectorToScreen(axisVectorPoints[0], axisVector);


                            //grphic.TrasientDisplay(new[] { new Line(axisVectorPoints[0], axisVectorPoints[1]) });
                            if (transientPoints != null && transientPoints.Length >= 2)
                                grphic.TrasientDisplay(new[] { new Line(transientPoints[0], transientPoints[1]) });

                            isWall = true;
                            verticalVector = axisVector;
                            break;
                        }
                    case "Columns":
                        {
                            isWall = false;
                            break;
                        }
                    case "Exit":
                        {
                            return;
                        }
                }

                PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку вставки");
                ppo.AllowArbitraryInput = true;
                ppo.AllowNone = false;
                ppo.Keywords.Add("ToleranceTop", "ВЕРтикальность", "ВЕРтикальность допуск", true, true);
                ppo.Keywords.Add("ToleranceTopMax", "ВЕРХотОси", "ВЕРХ от Оси допуск", true, true);
                ppo.Keywords.Add("ToleranceBottom", "БАЗА", "БАЗА допуск", true, true);
                ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);

                PromptPointResult ppr = null;
                while ((ppr = Tools.GetAcadEditor().GetPoint(ppo)).Status == PromptStatus.OK || ppr.Status == PromptStatus.Keyword)
                {
                    if (ppr.Status == PromptStatus.Keyword)
                    {
                        switch (ppr.StringResult)
                        {
                            case "ToleranceBottom":
                                {
                                    double? toleranse = _promptTolerance("\nУкажите допуск по низу от оси, м", toleranceBottom);
                                    if (toleranse.HasValue)
                                        toleranceBottom = toleranse.Value;
                                    break;
                                }
                            case "ToleranceTop":
                                {
                                    double? toleranse = _promptTolerance("\nУкажите допуск отклонения от вертикальности, м", toleranceTop);
                                    if (toleranse.HasValue)
                                    {
                                        toleranceTop = toleranse.Value;
                                        isTopMaxTolerance = false;
                                    }
                                    break;
                                }
                            case "ToleranceTopMax":
                                {
                                    double? toleranse = _promptTolerance("\nУкажите допуск по верху от оси, м", toleranceTop);
                                    if (toleranse.HasValue)
                                    {
                                        toleranceTop = toleranse.Value;
                                        isTopMaxTolerance = true;
                                    }
                                    break;
                                }
                            case "Exit":
                                {
                                    return;
                                }
                        }
                    }
                    else
                    {
                        Point3d insertPoint = ppr.Value;
                        DrawWallArrowsRandom(insertPoint, verticalVector, ucs, toleranceBottom, toleranceTop, isTopMaxTolerance, isWall);
                        if (!isWall)
                            DrawWallArrowsRandom(insertPoint, horizontalVector, ucs, toleranceBottom, toleranceTop, isTopMaxTolerance, isWall);


                        _dataProvider.Write("toleranceTop", toleranceTop);
                        _dataProvider.Write("toleranceBottom", toleranceBottom);
                        _dataProvider.Write("isTopMaxTolerance", isTopMaxTolerance);
                    }
                }
                grphic.StopTrasientDisplay();
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Основной метод выводв данных. Рисует стрелки в случайном порядке
        /// </summary>
        /// <param name="insertPoint">Точка вставки стрелок в ПСК</param>
        /// <param name="axisVector">Вектор направления оси/грани (перпендикулярно стрелкам)</param>
        /// <param name="ucs">Текущая ПСК</param>
        /// <param name="toleranceBottom">Допуск отклонеий от оси по базе</param>
        /// <param name="toleranceTop">Допуск отклонения от вертикали</param>
        /// <param name="isTopMaxTolerance">Определяет метод определения допуска отклонения по верху вертикального сооружения,
        /// если ИСТИНА то допуск по верху задан от разбивочной оси</param>
        /// <param name="isWall">Если ИСТИНА стрилки будут рисоваться только по одной оси</param>
        /// <param name="isWall">Если только стены (только две стрелки)</param>
        public static void DrawWallArrowsRandom(Point3d insertPoint, Vector3d axisVector, Matrix3d ucs, double toleranceBottom, double toleranceTop, bool isTopMaxTolerance ,bool isWall)
        {
            object mirrorTextValue = SetMirrorTextValue(1);
            try
            {
                WallDeviationArrows mainBlock = new WallDeviationArrows(axisVector, ucs);
                mainBlock._insertPointUcs = insertPoint;
                mainBlock._toleranceBottom = toleranceBottom;
                mainBlock._toleranceTop = toleranceTop;
                mainBlock._isTopMaxTolerance = isTopMaxTolerance;
                mainBlock._isToleranceOnly = true;

                Point3d lowerDestPoint = insertPoint.CreateRandomCirclePoints(1, toleranceBottom).First();

                mainBlock._clearEntities();
                mainBlock.DisplayLowerArrow(lowerDestPoint);
                mainBlock._destLowerPointComplete = true;               ////////////////////////////////////////////////////
                //mainBlock.DisplayRedirectedLowerArrow(insertPoint.CreateRandomCirclePoints(1, 10d).First());

                mainBlock._clearEntities();
                Point3d upperDestPoint = new Point3d();
                upperDestPoint = lowerDestPoint.CreateRandomCirclePoints(1, toleranceTop).First();
                mainBlock.DisplayRedirectedLowerArrow(upperDestPoint);
                mainBlock._jigLowerPointComplete = true;
                mainBlock._clearEntities();
                mainBlock.DisplayUpperArrow(upperDestPoint);
                mainBlock._destUpperPointComplete = true;

                mainBlock._clearEntities();
                //mainBlock.DisplayRedirectedUpperArrow(insertPoint.CreateRandomCirclePoints(1, 1d).First());
                //mainBlock.DisplayRedirectedUpperArrow(!isWall ? upperDestPoint : insertPoint);

                mainBlock.JigDraw();

                //Tools.AppendEntity(mainBlock.Entities.Select(ent => (Entity)ent.Clone()));
                mainBlock.Entities.Clear();
            }
            catch (Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage(ex.Message);
                return;
            }
            finally
            {
                SetMirrorTextValue(mirrorTextValue);
            }

        }

        /// <summary>
        /// Основной метод выводв данных. Рисует стрелки по полученным с экрана точкам
        /// </summary>
        /// <param name="axisVector">Вектор направления оси/грани (перпендикулярно стрелкам)</param>
        /// <param name="ucs">Текущая ПСК</param>
        /// <param name="onlyOnce">ИСТИНА если нужно выполнить только раз, иначе цикл</param>
        /// <returns></returns>
        public static PromptStatus DrawWallArrows(Vector3d axisVector ,Matrix3d ucs, bool onlyOnce = false)
        {
            double toleranceTop = /*0.01;*/ _dataProvider.Read("toleranceTop", 0.01d);
            double toleranceBottom = /*0.005;*/ _dataProvider.Read("toleranceBottom", 0.005d);
            bool isTopMaxTolerance = /*false;*/ _dataProvider.Read("isTopMaxTolerance", false);
            bool isToleranceOnly = /*false;*/ _dataProvider.Read("isToleranceOnly", false);

            object mirrorTextValue = SetMirrorTextValue(1);
            try
            {
                Point3d? insertPoint = Point3d.Origin;
                while ((insertPoint = GetInsertPoint(axisVector, ucs, ref toleranceBottom, ref toleranceTop, ref isTopMaxTolerance ,ref isToleranceOnly)).HasValue)
                {
                    PromptStatus res = PromptStatus.Cancel;

                    WallDeviationArrows mainBlock = new WallDeviationArrows(axisVector, ucs);

                    mainBlock._isToleranceOnly = isToleranceOnly;
                    mainBlock._toleranceTop = toleranceTop;
                    mainBlock._isTopMaxTolerance = isTopMaxTolerance;
                    mainBlock._toleranceBottom = toleranceBottom;

                    mainBlock._insertPointUcs = insertPoint.Value;
                    if ((res = mainBlock.JigDraw()) != PromptStatus.OK)
                        return res;
                    if (onlyOnce)
                        break;

                    _dataProvider.Write("toleranceTop", mainBlock._toleranceTop);
                    _dataProvider.Write("toleranceBottom", mainBlock._toleranceBottom);
                    _dataProvider.Write("isTopMaxTolerance", mainBlock._isTopMaxTolerance);
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
                SetMirrorTextValue(mirrorTextValue);
            }
        }

        #endregion

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
            _clearEntities();

            if (!_destLowerPointComplete)
            {
                DisplayLowerArrow(_destLowerPointUcs);
            }
            else
            {
                if (!_jigLowerPointComplete)                                ///////////////////////!!!!!!!!!!!!!!!!!!!!//////////////
                    DisplayRedirectedLowerArrow(_jigLowerPointUcs);          ///////////////////////!!!!!!!!!!!!!!!!!!!!//////////////
                else
                {
                    if (!_destUpperPointComplete)
                        DisplayUpperArrow(_destUpperPointUcs);
                    else
                    {
                        if (!_jigUpperPointComplete)
                            DisplayRedirectedUpperArrow(_jigUpperPointUcs);
                    }
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

            if (!_destLowerPointComplete)
            {
                PromptPointResult ppr = prompts.AcquirePoint(ppo);
                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                /*if ((_destLowerPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.00001)
                    return SamplerStatus.NoChange;*/
                _destLowerPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                return SamplerStatus.OK;
            }

            else
            {
                if (!_jigLowerPointComplete)     ////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!////////////////////////
                {
                    ppo.Message = "\nУкажите место отрисовки";
                    PromptPointResult ppr = prompts.AcquirePoint(ppo);
                    if (ppr.Status != PromptStatus.OK)
                        return SamplerStatus.Cancel;

                    /*if ((_jigLowerPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.00001)
                        return SamplerStatus.NoChange;*/
                    _jigLowerPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                    return SamplerStatus.OK;    ////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!////////////////////////
                }
                else
                {
                    if (!_destUpperPointComplete)
                    {
                        ppo.Message = "\nУкажите фактическое положение верха";
                        PromptPointResult ppr = prompts.AcquirePoint(ppo);
                        if (ppr.Status != PromptStatus.OK)
                            return SamplerStatus.Cancel;

                        /*if ((_destUpperPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.00001)
                            return SamplerStatus.NoChange;*/
                        _destUpperPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        if (!_jigUpperPointComplete)
                        {
                            ppo.Message = "\nУкажите место отрисовки";
                            PromptPointResult ppr = prompts.AcquirePoint(ppo);
                            if (ppr.Status != PromptStatus.OK)
                                return SamplerStatus.Cancel;

                            /*if ((_jigUpperPointUcs - ppr.Value.TransformBy(_ucs.Inverse())).Length < 0.00001)
                                return SamplerStatus.NoChange;*/
                            _jigUpperPointUcs = ppr.Value.TransformBy(_ucs.Inverse());
                            return SamplerStatus.OK;
                        }
                        return SamplerStatus.Cancel;
                    }
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
                ///////////////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
                _jigLowerPointComplete = true;
                ///////////////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!/////////////////////////////
            }
            if (!_destUpperPointComplete)
            {
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
                _destUpperPointComplete = true;
            }

            //if (_arrowUpper.IsCodirectional)
            {
                if (base.JigDraw() != PromptStatus.OK)
                    return PromptStatus.Cancel;
            }
            _jigUpperPointComplete = true;

            Tools.AppendEntity(Entities);

            Tools.GetAcadDatabase().TransactionManager.QueueForGraphicsFlush();
            Tools.GetAcadEditor().UpdateScreen();
            return PromptStatus.OK;
        }
        #endregion



        #region Acad I/O Methods
        [Obsolete]
        public Vector3d? DisplayAxisLine()
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите первую точку положения оси/грани");
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
            Vector3d resVector = ppr.Value - ppo.BasePoint;
            resVector = new Vector3d(resVector.X, resVector.Y, 0d);

            Line line = new Line(ppr.Value, ppo.BasePoint);
            base.TrasientDisplay(new[] { line });
            return resVector;
        }

        public static Point3d[] GetAxisVectorCmdEx(CoordinateSystem3d ucs)
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
        public void DisplayLowerArrow(Point3d destationPointUcs)
        {
#if DEBUG1
            StopTrasientDisplay();
#endif
            _arrowLower = new Arrow(_axisVector);

            double value = _arrowLower.Calculate(destationPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttrute(_arrowLower.ArrowLine.GetCenterPoint(), "Н", _arrowLower.LineTarnsform).ToList();

            _arrowLower.AppendArrowSymbols(symbs);

            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());


            if (Math.Abs(value) > _toleranceBottom)
            {
                if (_isToleranceOnly)
                {
                    value = (double)Math.Sign(value) * _toleranceBottom;
                    _arrowLower.LastValue = value;
                }
                _arrowLower.Highlight = true;
            }

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((value * 1000)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);

#if DEBUG1
            if (_arrowLower.Highlight)
            {
                TrasientDisplay(new[] { (Entity)Entities.Last().Clone() });
            }
#endif
        }

        public void DisplayUpperArrow(Point3d destationPointUcs)
        {
#if DEBUG1
            StopTrasientDisplay();
#endif

            _arrowUpper = new Arrow(_arrowLower);
            double value = _arrowUpper.Calculate(destationPointUcs.TransformBy(TransformToArrowBlock));

            var symbs = _createAttrute(_arrowUpper.ArrowLine.GetCenterPoint(), "В", _arrowUpper.LineTarnsform).ToList();
            _arrowUpper.AppendArrowSymbols(symbs);
            symbs.Add((Entity)_arrowUpper.ArrowLine.Clone());

            symbs.AddRange(_arrowLower.ArrowSymbols.Select(ent => (Entity)ent.Clone()));
            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());


            double bothDeviation = Math.Round(value, 3) - Math.Round(_arrowLower.LastValue.Value,3);
            if (Math.Abs(bothDeviation) > _toleranceTop ||
                Math.Abs(value) > _toleranceTop)
            {
                if (_isToleranceOnly)
                {
                    value = (double)Math.Sign(value) * (_toleranceTop) + _arrowLower.LastValue.Value;
                    _arrowUpper.LastValue = value;
                }
                if (!_isTopMaxTolerance)
                    _arrowUpper.Highlight = true;
                else
                {
                    if (Math.Abs(value) > _toleranceTop)
                    {
                        if (_isToleranceOnly)
                        {
                            value = (double)Math.Sign(value) * _toleranceTop;
                            _arrowUpper.LastValue = value;
                        }
                        _arrowLower.Highlight = true;
                    }
                }
            }


            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((_arrowLower.LastValue.Value * 1000d)).ToString("#0"));    //
            attrInfo.Add("отклонение_В", Math.Abs((_arrowUpper.LastValue.Value * 1000d)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);

#if DEBUG1
            if (_arrowUpper.Highlight)
            {
                TrasientDisplay(new[] { (Entity)Entities.Last().Clone() });
            }
#endif
        }

        public void DisplayRedirectedUpperArrow(Point3d jigPointUcs)
        {
#if DEBUG1
            StopTrasientDisplay();
#endif
            _arrowUpper.Redirect(jigPointUcs.TransformBy(TransformToArrowBlock));
            //var symbs = _createAttrute(_arrowUpper.ArrowLine.GetCenterPoint(), "В", _arrowUpper.LineTarnsform).ToList();
            var symbs = _arrowUpper.ArrowSymbols.Select(ent => (Entity)ent.Clone()).ToList();
            symbs.Add((Entity)_arrowUpper.ArrowLine.Clone());

            symbs.AddRange(_arrowUpper.BaseArrow.ArrowSymbols.Select(ent => (Entity)ent.Clone()));
            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((_arrowLower.LastValue.Value * 1000d)).ToString("#0"));    //
            attrInfo.Add("отклонение_В", Math.Abs((_arrowUpper.LastValue.Value * 1000d)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);

#if DEBUG1
            if (_arrowUpper.Highlight)
            {
                TrasientDisplay(new[] { (Entity)Entities.Last().Clone() });
            }
#endif
        }

        public void DisplayRedirectedLowerArrow(Point3d jigPointUcs)
        {
#if DEBUG1
            StopTrasientDisplay();
#endif
            _arrowLower.Redirect(jigPointUcs.TransformBy(TransformToArrowBlock));
            var symbs = _createAttrute(_arrowLower.ArrowLine.GetCenterPoint(), "Н", _arrowLower.LineTarnsform).ToList();
            symbs.Add((Entity)_arrowLower.ArrowLine.Clone());

            Dictionary<string, string> attrInfo = new Dictionary<string, string>();
            attrInfo.Add("отклонение_Н", Math.Abs((_arrowLower.LastValue.Value * 1000d)).ToString("#0"));
            _setEntitiesToBlock(_insertPointUcs, symbs, attrInfo, true);

#if DEBUG1
            if (_arrowLower.Highlight)
            {
                TrasientDisplay(new[] { (Entity)Entities.Last().Clone() });
            }
#endif
        }

        /// <summary>
        /// Промпт для получения начальных данных рисования стрелок (основное меню)
        /// </summary>
        /// <param name="axisVector">Вектор, определяющий проектное положение конструкции</param>
        /// <param name="ucs">Текущая ПСК</param>
        /// <param name="bottomTolerance">Допустимое отклонение по низу конструкции (от оси/точки вставки)</param>
        /// <param name="topTolerance">Допустимое отклонение по верху конструкции (от вертикали)</param>
        /// <param name="isTopMaxTolerance">Определяет метод определения допуска отклонения по верху вертикального сооружения</param>
        /// <param name="isToleranceOnly">Определяет метод отрисовки стрелок, если значение ИСТИНА величина отклонений будет ограничена допустимым пределом</param>
        /// <returns>Точка вставки, если NULL - выход</returns>
        public static Point3d? GetInsertPoint(Vector3d axisVector, Matrix3d ucs, ref double bottomTolerance, ref double topTolerance, ref bool isTopMaxTolerance ,ref bool isToleranceOnly)
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите точку вставки/проектное положение");
            ppo.Keywords.Add("Perpendicular", "Перпендикуляр", "Перпендикуляр", true, true);
            ppo.Keywords.Add("ToleranceTop", "ВЕРтикальность", "ВЕРтикальность допуск", true, true);
            ppo.Keywords.Add("ToleranceTopMax", "ВЕРХотОси", "ВЕРХ от Оси допуск", true, true);
            ppo.Keywords.Add("ToleranceBottom", "БАЗА", "БАЗА допуск", true, true);
            ppo.Keywords.Add("IsToleranceOnlyTrue", "ТОЛькоВДопуске", "ТОЛько В Допуске", true, true);
            ppo.Keywords.Add("IsToleranceOnlyFalse", "ФАКТически", "ФАКТические данные", true, true);
            ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            ppo.AllowArbitraryInput = true;

            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            while (ppr.Status == PromptStatus.Keyword)
            {
                switch (ppr.StringResult)
                {
                    case "Perpendicular":
                        {
                            if (DrawWallArrows(_calculateVector(axisVector, ucs, true), ucs, true) == PromptStatus.OK)
                                ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "ToleranceBottom":
                        {
                            double? toleranse = _promptTolerance("\nУкажите допуск по низу от оси, м", bottomTolerance);
                            if (toleranse.HasValue)
                                bottomTolerance = toleranse.Value;
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "ToleranceTop":
                        {
                            double? toleranse = _promptTolerance("\nУкажите допуск отклонения от вертикали, м", topTolerance);
                            if (toleranse.HasValue)
                            {
                                topTolerance = toleranse.Value;
                                isTopMaxTolerance = false;
                            }
                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            break;
                        }
                    case "ToleranceTopMax":
                        {
                            double? toleranse = _promptTolerance("\nУкажите допуск отклонения по верху от оси, м", topTolerance);
                            if (toleranse.HasValue)
                            {
                                topTolerance = toleranse.Value;
                                isTopMaxTolerance = true;
                            }
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

        public Point3d? GetDestPoint()
        {
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите фактическое положение");
            PromptPointResult ppr = Tools.GetAcadEditor().GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return null;
            return ppr.Value;
        }
        #endregion



        #region Prompts
        private static double? _promptTolerance(string msg, double defaultValue)
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
            bound.AddVertexAt(1, rectg.Value.UpperLeft.Add((rectg.Value.UpperLeft - rectg.Value.LowerLeft).Normalize().MultiplyBy(adPrefix.Height*0.1)));
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
            adPrefix.TransformBy(Matrix3d.Displacement(vector2.Normalize().MultiplyBy((vector.Length - vector2.Length)/2d)));
            adPrefix.AdjustAlignment(Tools.GetAcadDatabase());

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
#if DEBUG
            Tools.GetAcadEditor().WriteMessage("\nAngle = {0}", 180d * angle / Math.PI);
#endif
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
                    vpRectg.AddVertexAt(0, bottomLeft, 0,0,0);
                    vpRectg.AddVertexAt(1, bottomRight,0,0,0);
                    vpRectg.AddVertexAt(2, topRight, 0, 0, 0);
                    vpRectg.AddVertexAt(3, topLeft, 0, 0, 0);
                    vpRectg.AddVertexAt(4, bottomLeft, 0, 0, 0);

                    vpRectg.TransformBy(matDcsToWcs);

                    Polyline line = new Polyline(2);
                    Matrix3d mat = Matrix3d.Displacement(viewCenter.Convert3d().TransformBy(matDcsToWcs) - point);
                    point = point.TransformBy(mat);
                    line.AddVertexAt(0, point, 0,0,0);
                    line.AddVertexAt(1, point.Add(vector), 0,0,0);

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

        private void _clearEntities()
        {
            Tools.StartTransaction(() =>
            {
                if (Entities.FirstOrDefault(ent => ent is BlockReference) != null)
                    ((BlockReference)Entities.First()).BlockTableRecord.GetObjectForWrite<BlockTableRecord>().DeepErase(true);
            });
            Entities.ForEach(ent => ent.Dispose());
            Entities.Clear();
        }
        #endregion





        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        internal class Arrow:CustomObjects.Helpers.CustomObjectSerializer
        {
            private double _length;
            private double _arrowBlug;
            private double _arrowLength;
            private double _spaceLength;
            private double _arrowsSpace;

            private Matrix3d _lineTarnsform;

            public Arrow(Vector3d axisVector, 
                double length = 4.0,
                double arrowBlug = 0.7,
                double arrowLengh = 1.5,
                double spaceLengh = 3.0,
                double arrowSpace = 0.5)
            {
                _length = length;
                _arrowBlug = arrowBlug;
                _arrowLength = arrowLengh;
                _spaceLength = spaceLengh;
                _arrowsSpace = arrowSpace;

                this.AxisVector = axisVector;

                this.ArrowSymbols = new List<Entity>();

                this.ArrowLine = _createLine();
                _lineTarnsform = Matrix3d.Identity;

                Matrix3d rotation = _getMainRotation();
                //this.LineTarnsform = rotation;
                _arrowPositionPreprocessor(ArrowActions.Rotation, rotation);

                this.BaseArrow = null;
            }
            public Arrow(Arrow baseArrow)
                :this(baseArrow.AxisVector)
            {
                this.BaseArrow = baseArrow;
            }

            public Polyline ArrowLine { get; private set; }
            public List<Entity> ArrowSymbols { get; private set; }
            public Vector3d AxisVector { get; private set; }
            public bool IsRedirected { get; private set; }
            public bool IsMirrored { get; private set; }
            public bool IsSymbolsMirrored { get; private set; }
            public bool IsBottomDisplacemented { get; private set; }
            public bool IsTopDisplacemented { get; private set; }
            public double? LastValue { get; set; }
            public bool Highlight { get; set; }
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
                _arrowPositionPreprocessor(ArrowActions.MirrowedLine, mirror);///////////////////////////////////////

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
                Redirect();
            }

            public void Redirect()
            {
                Point3d destPoint = Point3d.Origin.Add(_lineTarnsform.CoordinateSystem3d.Xaxis.Negate()
                    .MultiplyBy((IsRedirected ? -1 : 1) * (_spaceLength * 2d + _length + _arrowLength)));
                Matrix3d mat = Matrix3d.Displacement(destPoint.GetAsVector());
                _arrowPositionPreprocessor(ArrowActions.Redirected, mat);
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


            public double Calculate(Point3d destPointLocal)
            {
                this.LastValue = _calculateValue(destPointLocal);
                if (this.LastValue.Value < 0d)
                    this.Mirror();
                _arrowPositionPreprocessor(ArrowActions.NaN, Matrix3d.Identity);

                return LastValue.Value;
            }

            [Obsolete("Неправильно определяет значение, не по нормали. Используй _calculateValue(Point3d destPoint")]
            private double _calculateValueEx(Point3d destPoint)
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

            private double _calculateValue(Point3d destPoint)
            {
                try
                {
                    Point3d point2d = new Point3d(destPoint.X, destPoint.Y, 0d);
                    Line line = new Line(Point3d.Origin, Point3d.Origin.Add(this.AxisVector));
                    Line normal = line.GetPerpendicularFromPoint(point2d);
                    var vector = normal.EndPoint - normal.StartPoint;
                    double res = normal.Length * (vector.IsCodirectionalTo(AxisVector.GetPerpendicularVector(), Tolerance.Global) ? 1d : -1d) ;
                    return res;
                }
                catch
                {
                    return 0;
                }
            }

            [Obsolete("Надо доработать, перенисти трансформации препроцессор")]
            public void MirrorSymbols()
            {
                Plane plane = new Plane(_lineTarnsform.CoordinateSystem3d.Origin, _lineTarnsform.CoordinateSystem3d.Xaxis, _lineTarnsform.CoordinateSystem3d.Zaxis);
                Matrix3d mat = Matrix3d.Mirroring(plane);

                Extents3d? bounds = _getSymbolsBounds(this.ArrowSymbols);
                if (bounds.HasValue)
                {
                    Point3d max = bounds.Value.MaxPoint.TransformBy(_lineTarnsform.Inverse());
                    max = new Point3d(0, max.Y, 0).TransformBy(_lineTarnsform);
                    Point3d min = bounds.Value.MinPoint.TransformBy(_lineTarnsform.Inverse());
                    min = new Point3d(0, min.Y, 0).TransformBy(_lineTarnsform);

                    Vector3d deltaVector = (min - _lineTarnsform.CoordinateSystem3d.Origin);

                    Vector3d vector = (max - min).DivideBy(2d).Add(deltaVector);

                    Point3d point = _lineTarnsform.CoordinateSystem3d.Origin.Add(vector.Negate());
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

            public bool MoveToBottom()
            {
                if (this.IsBottomDisplacemented)
                    return false;

                Matrix3d? displacement = _movetAtYaxis(-1);
                if (!displacement.HasValue)
                    return false;

                _arrowPositionPreprocessor(ArrowActions.MovetBottom, displacement.Value);
                return true;
            }

            public bool MoveToTop()
            {
                if (this.IsTopDisplacemented)
                    return false;

                Matrix3d? displacement = _movetAtYaxis(1);
                if (!displacement.HasValue)
                    return false;

                _arrowPositionPreprocessor(ArrowActions.MovedTop, displacement.Value);
                return true;
            }

            private Matrix3d? _movetAtYaxis(int sign)
            {
                /*Extents3d? bounds = _getSymbolsBounds(this.ArrowSymbols);
                if (!bounds.HasValue)
                    return null;*/

                Vector3d direction = new Vector3d();
                if (sign < 0)
                    direction = _lineTarnsform.CoordinateSystem3d.Yaxis.Negate();
                else
                    direction = _lineTarnsform.CoordinateSystem3d.Yaxis;

                Matrix3d mat = Matrix3d.Displacement(direction.MultiplyBy(_arrowsSpace)); 

                return mat;
            }

            private void _arrowPositionPreprocessor(ArrowActions actionType, Matrix3d transform)
            {
                this.ArrowLine.TransformBy(transform);
                this.ArrowSymbols.ForEach(ent =>
                {
                    ent.TransformBy(transform);
                    if (ent is DBText)
                        ((DBText)ent).AdjustAlignment(Tools.GetAcadDatabase());
                });
                _lineTarnsform = _lineTarnsform.PreMultiplyBy(transform);


                switch (actionType)
                {
                    case ArrowActions.Rotation:
                        {
                            break;
                        }
                    case ArrowActions.MirrowedLine:
                        {
                            IsMirrored = !IsMirrored;
                            break;
                        }
                    case ArrowActions.Redirected:
                        {
                            IsRedirected = !IsRedirected;

                            if (this.BaseArrow != null)
                            {
                                if ((IsCodirectional && (IsRedirected != BaseArrow.IsRedirected))
                                    || (!IsCodirectional && (IsRedirected == BaseArrow.IsRedirected)))
                                    this.BaseArrow.Redirect();
                            }
                            break;
                        }
                    case ArrowActions.MovetBottom:
                        {
                            if (this.IsTopDisplacemented)
                            {
                                this.IsTopDisplacemented = false;
                                this.IsBottomDisplacemented = false;
                            }
                            else
                                this.IsBottomDisplacemented = !this.IsBottomDisplacemented;
                            break;
                        }
                    case ArrowActions.MovedTop:
                        {
                            if (this.IsBottomDisplacemented)
                            {
                                this.IsBottomDisplacemented = false;
                                this.IsTopDisplacemented = false;
                            }
                            else
                                this.IsTopDisplacemented = !this.IsTopDisplacemented;
                            break;
                        }
                }
                if (this.BaseArrow != null)
                {
                    if ((IsCodirectional && (IsRedirected == this.BaseArrow.IsRedirected)) ||
                        (!IsCodirectional && (IsRedirected != this.BaseArrow.IsRedirected)))
                    {
                        if (!this.IsTopDisplacemented)
                            this.MoveToTop();
                        if (!this.BaseArrow.IsBottomDisplacemented)
                            this.BaseArrow.MoveToBottom();
                        if (!this.BaseArrow.IsSymbolsMirrored)
                            this.BaseArrow.MirrorSymbols();
                    }
                    else
                    {
                        /*if (this.IsTopDisplacemented)
                            this.MoveToBottom();
                        if (this.BaseArrow.IsBottomDisplacemented)
                            this.BaseArrow.MoveToTop();
                        if (this.BaseArrow.IsSymbolsMirrored)
                            this.BaseArrow.MirrorSymbols();*/
                        
                        ///Поворачиваем символы на место
                        if (this.IsSymbolsMirrored)
                            this.MirrorSymbols();
                        if (this.BaseArrow.IsSymbolsMirrored)
                            this.BaseArrow.MirrorSymbols();
                        ///////////////////////////////////////////////

                        ///Приводим в центр по горизонтали
                        if (this.IsTopDisplacemented)
                            this.MoveToBottom();
                        if (this.IsBottomDisplacemented)
                            this.MoveToTop();
                        if (this.BaseArrow.IsTopDisplacemented)
                            this.BaseArrow.MoveToBottom();
                        if (this.BaseArrow.IsBottomDisplacemented)
                            this.BaseArrow.MoveToTop();
                        //////////////////////////////////////////////
                    }
                }


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
                                catch {  }
                            }
                        }
                    });
                if (res == null)
                    return null;
                res.TransformBy(_lineTarnsform);
                return res;
            }

            public event EventHandler<ArrowEventArgs> ArrowChanging;
            protected virtual void On_ArrowChanging(object sender, ArrowEventArgs e)
            {
                if (ArrowChanging != null)
                    ArrowChanging(sender, e);
            }

            public class ArrowEventArgs:EventArgs
            {
                public ArrowEventArgs()
                    :base()
                {

                }

                public ArrowActions Action { get; set; }
                public object Tag { get; set; }
            }

            public enum ArrowActions
            {
                NaN = 0,
                MovedTop,
                MovetBottom,
                MirrowedLine,
                SymbolMirrowed,
                Redirected,
                Rotation
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
