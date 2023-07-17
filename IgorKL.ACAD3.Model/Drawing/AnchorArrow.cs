using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using display = IgorKL.ACAD3.Model.Helpers.Display;
using IgorKL.ACAD3.Model.Extensions;
using helpers = IgorKL.ACAD3.Model.Drawing.Helpers;

namespace IgorKL.ACAD3.Model.Drawing {
    public class AnchorArrow : CustomObjects.SimpleEntityOverride {
        private Point3d _insertPoint;
        private Point3d _destPoint;
        private helpers.Arrow _arrow;
        private bool _jigSelected;
        private Point3d _jigDestPoint;

        private double _tolerance;
        private static MainMenu.HostProvider _dataHost = new MainMenu.HostProvider(new AnchorArrow());

        public AnchorArrow()
            : this(Point3d.Origin) { }
        public AnchorArrow(Point3d origin)
            : base(origin, new List<Entity>(), Matrix3d.Identity) {
            if (_dataHost == null)
                _dataHost = new MainMenu.HostProvider(this);
            _tolerance = _dataHost.Read("tolerance", 0.005d);
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawArrows", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawArrows() {

            AnchorArrow mainBlock = new AnchorArrow();

            mainBlock._ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите проектное положение");
            ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            ppo.Keywords.Add("Blug", "ТолщинаСтрелки", "ТолщинаСтрелки", true, true);
            ppo.AllowArbitraryInput = true;
            ppo.AllowNone = false;

            double? arrowBlug = null;
            PromptPointResult ppr = null;
            promptPoint();

            void promptPoint() {
                while ((ppr = Tools.GetAcadEditor().GetPoint(ppo)).Status == PromptStatus.OK || ppr.Status == PromptStatus.Keyword) {

                    if (ppr.Status == PromptStatus.Keyword) {
                        switch (ppr.StringResult) {
                            case "Exit": {
                                return;
                            }
                            case "Blug": {
                                var res = Tools.GetAcadEditor().GetDouble(new PromptDoubleOptions("\nУкажите толщину стрелки") { AllowNone = false, DefaultValue = 0.7d, AllowNegative = false });
                                if (res.Status == PromptStatus.OK) {
                                    arrowBlug = res.Value;
                                }
                                promptPoint();
                                return;
                            }
                        }
                    }

                    mainBlock._insertPoint = ppr.Value;
                    mainBlock._arrow = new helpers.Arrow(ppr.Value, mainBlock._ucs, arrowBlug);
                    if (mainBlock.JigDraw() == PromptStatus.OK)
                        mainBlock.SaveToDatabase();
                }
            }
        }

        [RibbonCommandButton("Анкера рандом", "Стрелки")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawArrowsRandom", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawArrowsRandom() {
            AnchorArrow mainBlock = new AnchorArrow();
            mainBlock._ucs = CoordinateSystem.CoordinateTools.GetCurrentUcs();
            mainBlock._tolerance = _dataHost.Read("tolerance", mainBlock._tolerance);
            PromptPointOptions ppo = new PromptPointOptions("\nУкажите проектное положение");
            ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            ppo.Keywords.Add("Tolerance", "ДОПуск", "ДОПуск", true, true);
            ppo.AllowArbitraryInput = true;
            ppo.AllowNone = false;

            List<Point3d> points = new List<Point3d>();

            PromptPointResult ppr = null;
            while ((ppr = Tools.GetAcadEditor().GetPoint(ppo)).Status == PromptStatus.OK
                || ppr.Status == PromptStatus.Keyword) {
                if (ppr.Status == PromptStatus.Keyword) {
                    switch (ppr.StringResult) {
                        case "Exit": {
                            return;
                        }
                        case "Tolerance": {
                            PromptDoubleOptions pdo = new PromptDoubleOptions("\nУкажите допуск в плане, м: ");
                            pdo.AllowArbitraryInput = false;
                            pdo.AllowNegative = false;
                            pdo.AllowNone = false;
                            pdo.UseDefaultValue = true;
                            pdo.DefaultValue = mainBlock._tolerance;

                            PromptDoubleResult pdr = Tools.GetAcadEditor().GetDouble(pdo);
                            if (pdr.Status != PromptStatus.OK)
                                return;

                            mainBlock._tolerance = pdr.Value;
                            _dataHost.Write("tolerance", mainBlock._tolerance);

                            ppr = Tools.GetAcadEditor().GetPoint(ppo);
                            if (ppr.Status != PromptStatus.OK)
                                return;
                            break;
                        }
                    }
                }

                mainBlock._insertPoint = ppr.Value;
                Point3d destPoint = mainBlock._insertPoint.CreateRandomCirclePoints(1, mainBlock._tolerance).First();
                mainBlock._arrow = new helpers.Arrow(ppr.Value, mainBlock._ucs);

                mainBlock._arrow.Calculate(destPoint);
                mainBlock._jigSelected = true;
                mainBlock._destPoint = destPoint;
                if (mainBlock.JigDraw(mainBlock._jigSelected) == PromptStatus.OK)
                    mainBlock.SaveToDatabase();
            }
        }

        public virtual void DisplayAtBlock(Point3d position, Dictionary<string, string> attributeInfoSet) {
            if (_transient == null)
                _transient = new display.DynamicTransient();

            ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecordEx(Origin, "*U", Entities, AnnotativeStates.True);
            BlockReference br = new BlockReference(position, btrId);

            foreach (var ent in Entities) {
                _transient.AddMarker((Entity)ent.Clone());
            }

            _transient.Display();
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw) {
            lock (Entities) {

                Entities.Clear();
                Entities.AddRange(_arrow.Explode().Select(x => {
                    if (x is DBText)
                        return _convertToAttribute((DBText)x);
                    return x;
                }));

                ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecordEx(_insertPoint, "*U", Entities.Select(x => (Entity)x.Clone()).ToList(), AnnotativeStates.True);
                ObjectId brId = AcadBlocks.BlockTools.AddBlockRefToModelSpace(btrId, null, _insertPoint, _ucs);

                Tools.StartTransaction(() => {
                    BlockReference br = brId.GetObjectForWrite<BlockReference>();
                    br.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
                    br.RecordGraphicsModified(true);

                    Entity inMemoryEntity = (Entity)br.Clone();
                    draw.Geometry.Draw(inMemoryEntity);

                    var btr = br.BlockTableRecord.GetObjectForWrite<BlockTableRecord>();
                    br.Erase();
                    btr.EraseBolckTableRecord();
                    inMemoryEntity.Dispose();
                });
            }

            return true;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts) {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\nУкажите фактическое положение");
            if (!_jigSelected) {
                ppo.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
                ppo.BasePoint = _insertPoint.TransformBy(_ucs);

                ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;
                if (ppr.Status == PromptStatus.Keyword) {
                    switch (ppr.StringResult) {
                        case "Exit": {
                            return SamplerStatus.Cancel;
                        }
                    }
                }

                _destPoint = ppr.Value.TransformBy(_ucs.Inverse());
                _jigDestPoint = _destPoint;



                _arrow.Calculate(_destPoint);

                return SamplerStatus.OK;
            } else {
                ppo = new JigPromptPointOptions("\nУкажите сторону отрисовки");
                ppo.UseBasePoint = true;
                ppo.BasePoint = _insertPoint.TransformBy(_ucs);

                ppo.UserInputControls = UserInputControls.NoZeroResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;
                {
                    switch (ppr.StringResult) {
                        case "Exit": {
                            return SamplerStatus.Cancel;
                        }
                    }
                }

                if (_jigDestPoint == ppr.Value.TransformBy(_ucs.Inverse()))
                    return SamplerStatus.NoChange;

                _jigDestPoint = ppr.Value.TransformBy(_ucs.Inverse());

                _arrow.Redirect(_jigDestPoint);

                return SamplerStatus.OK;
            }
        }

        public override PromptStatus JigDraw() {
            _jigSelected = false;
            var promptStatus = base.JigDraw();
            if (promptStatus == PromptStatus.OK) {
                _jigSelected = true;
                base.JigDraw();
            }
            return promptStatus;
        }

        public PromptStatus JigDraw(bool isDestPointSelected) {
            PromptStatus promptStatus = PromptStatus.OK;
            if (!isDestPointSelected) {
                _jigSelected = false;
                promptStatus = base.JigDraw();
            }
            if (promptStatus == PromptStatus.OK) {
                _jigSelected = true;
                base.JigDraw();
            }
            return promptStatus;
        }

        public void SaveToDatabase() {
            ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecordEx(_insertPoint, "*U", Entities, AnnotativeStates.True);
            ObjectId brId = AcadBlocks.BlockTools.AddBlockRefToModelSpace(btrId, null, _insertPoint, _ucs);
            Tools.StartTransaction(() => {
                BlockReference br = brId.GetObjectForWrite<BlockReference>();
                br.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
                br.RecordGraphicsModified(true);

                var xrecord = CustomObjects.Helpers.XRecordTools.GetSetExtensionDictionaryEntry(br.Id, "ICmdFlag_WALLARROW_FLAG").GetObjectForRead<Xrecord>();
                xrecord.UpgradeOpen();

                Point3d p = _insertPoint;

                ResultBuffer rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.Real, p.X),
                        new TypedValue((int)DxfCode.Real, p.Y),
                        new TypedValue((int)DxfCode.Real, p.Z)
                    );
                xrecord.Append(rb);

                _arrow.SerializeTo(br);
            });
        }

        private AttributeDefinition _convertToAttribute(DBText text) {
            AttributeDefinition ad = new AttributeDefinition();
            ad.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
            ad.TextString = text.TextString;
            ad.HorizontalMode = text.HorizontalMode;
            ad.VerticalMode = text.VerticalMode;
            ad.Height = text.Height;
            ad.Annotative = text.Annotative;
            ad.Constant = false;
            ad.Verifiable = true;
            ad.Rotation = text.Rotation;
            ad.Tag = "анкера_отклонение";
            ad.Prompt = "Отклонение " + (ad.Rotation == 0d ? "по горизонтали" : "по вертикали");
            ad.Position = text.Position;
            ad.AlignmentPoint = text.AlignmentPoint;
            ad.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            return ad;
        }

        private void _showSettingMenu() {
            var menu = MainMenu.MainPaletteSet.CreatedInstance;
            var arrowControl = new Views.AnchorArrowView();
            menu.AddControl("Анкера (настройки)", arrowControl);
            menu.Show();
        }
    }
}
