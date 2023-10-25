using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

using IgorKL.ACAD3.Model;

namespace IgorKL.ACAD3.Customization.Ribbons {
    public class ICmdRibbonPane : IExtensionApplication
    {
        public static string TabId { get => "ICmd_Drawing_Tab"; }
        public static string AssemblyName { get => "IgorKL.ACAD3.Model.dll"; }
        public static string AppName { get => "GeoHelper.NET"; }

        public ICmdRibbonPane()
        {
            _ribbonTab = null;
        }

        private RibbonTab _ribbonTab;

        public void Initialize()
        {
            try
            {
                if (ComponentManager.Ribbon != null)
                {
                    _ribbonTab = ComponentManager.Ribbon.Tabs.FirstOrDefault(tab => tab.Id == TabId);
                    if (_ribbonTab == null)
                    {
                        _createMainRibbon();
                        _createArrowsRibbonPanel();
                        _createAutoRibbonButtons();
                        _createMangeRibbonPanel();
                        /// _ribbonTab.IsActive = true;
                    }
                    _ribbonTab.IsVisible = true;
                    /// ComponentManager.Ribbon.ActiveTab = _ribbonTab;
                }
                if (ComponentManager.Ribbon.Tabs.FirstOrDefault(tab => tab == _ribbonTab) == null) {
                    ComponentManager.Ribbon.Tabs.Add(_ribbonTab);
                }
            } catch (System.Exception ex) {
                Tools.GetAcadEditor().WriteMessage(ex.Message);
                string asmName = AssemblyName;
                var asm = LoadAssembly(asmName);
            }
        }

        public void Terminate() {
            try {
                //_ribbonTab.IsActive = false;
                //ComponentManager.Ribbon.Tabs.Remove(_ribbonTab);
                Application.SystemVariableChanged -= new Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventHandler(wsChangedHandler);

            } catch { }

        }

        [Obsolete("Not impl")]
        public static void SaveCustomizationSection(bool removeTab = false) {
            string menuName = (string)Application.GetSystemVariable("MENUNAME");
            var cs = new Autodesk.AutoCAD.Customization.CustomizationSection(menuName + ".cuix");
            var ribbonRoot = cs.MenuGroup.RibbonRoot;
            var appTab = ribbonRoot.FindTab(TabId);

            bool updatetd = false;

            if (appTab != null && !removeTab) {
                ribbonRoot.RibbonTabSources.Add(appTab);
                updatetd = true;
            } else if (appTab != null) {
                ribbonRoot.RibbonTabSources.Remove(appTab);
                updatetd = true;
            }

            if (updatetd) { 
                cs.Save(); 
            }

            /*if (ribbonPanelSourceReference is null) {
                var panel = homeTab.AddNewPanel(elementId, "TogglePanel");
                var row = panel.AddNewRibbonRow();
                row.AddNewToggleButton("XyzToggleButton", "XYZSTATE\nToggle", null, RibbonButtonStyle.LargeWithText);
                cs.Save();
            }*/
        }

        /*void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (_ribbonTab == null)
                Application.DocumentManager.DocumentCreated -= DocumentManager_DocumentCreated;
            this.Initialize();
            if (!_ribbonTab.IsVisible)
                _ribbonTab.IsVisible = true;
            ComponentManager.Ribbon.UpdateLayout();
        }*/

#if DEBUG1
        void RibbonServices_RibbonPaletteSetCreated(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.Ribbon.RibbonServices.RibbonPaletteSet.PaletteActivated += RibbonPaletteSet_PaletteActivated;
            Autodesk.AutoCAD.Ribbon.RibbonServices.RibbonPaletteSet.Load += RibbonPaletteSet_Load;
        }

        void RibbonPaletteSet_Load(object sender, PalettePersistEventArgs e)
        {

        }

        void RibbonPaletteSet_PaletteActivated(object sender, PaletteActivatedEventArgs e)
        {
            try
            {
                /*string asmName = "IgorKL.ACAD3.Model.dll";
                var asm = LoadAssembly(asmName);*/

                var ribbTab = ComponentManager.Ribbon.Tabs.FirstOrDefault(tab => tab.Id == "ICmd_Drawing_Tab");
                if (ribbTab == null)
                {
                    _createRibbon();
                    _createAutoRibbonButtons();
                }
                else
                    _ribbonTab = ribbTab;

                foreach (var tab in ComponentManager.Ribbon.Tabs)
                    if (tab.Id == "CIVIL.ID_Civil3DHome")
                    {
                        //tab.Panels.Add(panel);
                        if (!tab.IsActive)
                            tab.IsActive = true;
                        foreach (var item in tab.Panels[0].Source.Items)
                            if (item is RibbonButton)
                            {
                                RibbonButton rb = item as RibbonButton;
                            }

                    }
                _ribbonTab.IsActive = true;
            }
            catch (System.Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage(ex.Message);
            }
            finally
            {

            }
        }
#endif

        private void _createMainRibbon()
        {
            _ribbonTab = new RibbonTab
            {
                Id = TabId,
                Name = AppName,
                Tag = this,
                Title = AppName,
                IsContextualTab = false,
                IsVisible = true,
            };

            Application.SystemVariableChanged += new Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventHandler(wsChangedHandler);

            /*ComponentManager.Ribbon.Tabs.Add(_ribbonTab);
            string menuName = (string)Application.GetSystemVariable("MENUNAME");
            var cs = new Autodesk.AutoCAD.Customization.CustomizationSection(menuName + ".cuix");
            var ribbonRoot = cs.MenuGroup.RibbonRoot;
            Autodesk.AutoCAD.Customization.RibbonTabSource ts = new Autodesk.AutoCAD.Customization.RibbonTabSource(ribbonRoot);

            string curWorkspaceName = (string)Application.GetSystemVariable("WSCURRENT");
            var ws = cs.getWorkspace(curWorkspaceName);*/

        }

        private void wsChangedHandler (object _, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e) {
            if (e.Name == "WSCURRENT" && _ribbonTab != null) {
                if (ComponentManager.Ribbon.Tabs.FirstOrDefault(tab => tab == _ribbonTab) == null) {
                    ComponentManager.Ribbon.Tabs.Add(_ribbonTab);
                }
            };
        }

        private void _createMangeRibbonPanel()
        {
            try
            {
                RibbonPanelSource sourceMangPanel = new RibbonPanelSource();
                sourceMangPanel.Title = "Автозагрузка";
                sourceMangPanel.Id = "ICmd_Drawing.DeamandLoader";

                RibbonButton buttonNetload = new RibbonButton();
                buttonNetload.Id = "ICmd_Drawing_Netload";
                buttonNetload.Orientation = System.Windows.Controls.Orientation.Vertical;
                buttonNetload.Size = RibbonItemSize.Standard;
                buttonNetload.ShowText = true;
                buttonNetload.Text = "Загрузить .dll";
                buttonNetload.CommandParameter = "NETLOAD";
                buttonNetload.CommandHandler = new RibbonButtonCommandHandler();
                buttonNetload.GroupName = "Управление";
                buttonNetload.Orientation = System.Windows.Controls.Orientation.Horizontal;

                RibbonButton buttonRegApp = new RibbonButton();
                buttonRegApp.Text = "Добавить";
                buttonRegApp.Id = "Register_ICmd_Drawing";
                buttonRegApp.ShowImage = true;
                buttonRegApp.ShowText = true;
                buttonRegApp.ResizeStyle = RibbonItemResizeStyles.ResizeWidth;
                buttonRegApp.AllowInToolBar = true;
                buttonRegApp.Orientation = System.Windows.Controls.Orientation.Vertical;
                buttonRegApp.Size = RibbonItemSize.Large;
                buttonRegApp.Image = loadImage(Properties.Resources.RegIcon);
                buttonRegApp.LargeImage = loadImage(Properties.Resources.RegIcon);
                buttonRegApp.CommandParameter = "Register_ICmd_Drawing";
                buttonRegApp.CommandHandler = new RibbonButtonCommandHandler();

                RibbonButton buttonUnRegApp = new RibbonButton();
                buttonUnRegApp.Text = "Убрать";
                buttonUnRegApp.Id = "Unregister_ICmd_Drawing";
                buttonUnRegApp.ShowImage = true;
                buttonUnRegApp.ShowText = true;
                buttonUnRegApp.ResizeStyle = RibbonItemResizeStyles.ResizeWidth;
                buttonUnRegApp.AllowInToolBar = true;
                buttonUnRegApp.Orientation = System.Windows.Controls.Orientation.Vertical;
                buttonUnRegApp.Size = RibbonItemSize.Large;
                buttonUnRegApp.Image = loadImage(Properties.Resources.UnRegIcon);
                buttonUnRegApp.LargeImage = loadImage(Properties.Resources.UnRegIcon);
                buttonUnRegApp.CommandParameter = "Unregister_ICmd_Drawing";
                buttonUnRegApp.CommandHandler = new RibbonButtonCommandHandler();


                RibbonPanel mangPanel = new RibbonPanel
                {
                    //mangPanel.CustomPanelTitleBarBackground = System.Windows.Media.Brushes.LightYellow;
                    CanToggleOrientation = true,
                    Source = sourceMangPanel,
                    ResizeStyle = RibbonResizeStyles.NeverResizeItemWidth | RibbonResizeStyles.NeverHideText | RibbonResizeStyles.NeverCollapseItem
                };

                mangPanel.Source.Items.Add(buttonRegApp);
                mangPanel.Source.Items.Add(buttonUnRegApp);
                //mangPanel.Source.Items.Add(buttonNetload);

                _ribbonTab.Panels.Add(mangPanel);
            }
            catch
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("MangeRibbonPanel creating exception");
            }
//#endif
        }

        private void _createArrowsRibbonPanel()
        {
            RibbonPanelSource source = new RibbonPanelSource();
            source.Title = "Стрелки";
            source.Id = "ICmd_Drawing.Arrows";



            RibbonButton buttonWallArrows = new RibbonButton();
            buttonWallArrows.Text = "Верх/Низ";
            buttonWallArrows.Id = "ICmd_Drawing_DrawWallArrows";
            buttonWallArrows.ShowImage = true;
            buttonWallArrows.ShowText = true;
            //buttonWallArrows.GroupName = "Стрелки";
            buttonWallArrows.ResizeStyle = RibbonItemResizeStyles.ResizeWidth;
            buttonWallArrows.AllowInToolBar = true;
            buttonWallArrows.Orientation = System.Windows.Controls.Orientation.Vertical;
            buttonWallArrows.Size = RibbonItemSize.Large;
            buttonWallArrows.Image = loadImage(Properties.Resources.DrawWallArrows);
            buttonWallArrows.LargeImage = loadImage(Properties.Resources.DrawWallArrows);
            buttonWallArrows.CommandParameter = "iCmd_DrawWallArrows";
            buttonWallArrows.CommandHandler = new RibbonButtonCommandHandler();

            RibbonButton buttonArrows = new RibbonButton();
            buttonArrows.Text = "Анкера";
            buttonArrows.Id = "ICmd_Drawing_DrawArrows";
            buttonArrows.ShowImage = true;
            buttonArrows.ShowText = true;
            //buttonArrows.GroupName = "Стрелки";
            buttonArrows.ResizeStyle = RibbonItemResizeStyles.ResizeWidth;
            buttonArrows.AllowInToolBar = true;
            buttonArrows.Orientation = System.Windows.Controls.Orientation.Vertical;
            buttonArrows.Size = RibbonItemSize.Large;
            buttonArrows.Image = loadImage(Properties.Resources.DrawArrows);
            buttonArrows.LargeImage = loadImage(Properties.Resources.DrawArrows);
            buttonArrows.CommandParameter = "iCmd_DrawArrows";
            buttonArrows.CommandHandler = new RibbonButtonCommandHandler();

            RibbonButton buttonDimensionValueRandom = new RibbonButton();
            buttonDimensionValueRandom.Text = "Размеры";
            buttonDimensionValueRandom.Id = "ICmd_Drawing_EditDimensionValueRandom";
            buttonDimensionValueRandom.ShowImage = true;
            buttonDimensionValueRandom.ShowText = true;
            //buttonDimensionValueRandom.GroupName = "Стрелки";
            buttonDimensionValueRandom.ResizeStyle = RibbonItemResizeStyles.ResizeWidth;
            buttonDimensionValueRandom.AllowInToolBar = true;
            buttonDimensionValueRandom.Orientation = System.Windows.Controls.Orientation.Vertical;
            buttonDimensionValueRandom.Size = RibbonItemSize.Large;
            buttonDimensionValueRandom.Image = loadImage(Properties.Resources.EditDimensionValueRandom);
            buttonDimensionValueRandom.LargeImage = loadImage(Properties.Resources.EditDimensionValueRandom);
            buttonDimensionValueRandom.CommandParameter = "iCmd_EditDimensionValueRandom";
            buttonDimensionValueRandom.CommandHandler = new RibbonButtonCommandHandler();

            RibbonPanel _arrowsPanel = new RibbonPanel();
            _arrowsPanel.Source = source;
            _arrowsPanel.CanToggleOrientation = true;
            _arrowsPanel.ResizeStyle = RibbonResizeStyles.NeverResizeItemWidth | RibbonResizeStyles.NeverHideText | RibbonResizeStyles.NeverCollapseItem;
            //_panel.CustomPanelTitleBarBackground = System.Windows.Media.Brushes.LightYellow;

            source.Items.Add(buttonWallArrows);
            source.Items.Add(buttonArrows);
            source.Items.Add(buttonDimensionValueRandom);
            //source.Items.Add(buttonNetload);

            _ribbonTab.Panels.Add(_arrowsPanel);
        }

        private static System.Windows.Media.Imaging.BitmapImage loadImage(System.Drawing.Bitmap img)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
        }

        private void _createAutoRibbonButtons()
        {
            string asmName = "IgorKL.ACAD3.Model.dll";
            var asm = LoadAssembly(asmName);
            if (asm == null)
                return;

            var types = asm.GetTypes();
            foreach (var t in types)
            {
                var methods = t.GetMethods(System.Reflection.BindingFlags.CreateInstance |
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);


                foreach (var mi in methods)
                {
                    object[] attrs = mi.GetCustomAttributes(typeof(CommandMethodAttribute), false);
                    foreach (CommandMethodAttribute cmdAttr in attrs)
                    {
                        var objs = mi.GetCustomAttributes(typeof(RibbonCommandButtonAttribute), false);
                        if (objs == null || objs.Length == 0)
                            continue;
                        RibbonCommandButtonAttribute ribbAttr = (RibbonCommandButtonAttribute)objs.First();

                        _pastToPanelAtSimpleRow(ribbAttr, cmdAttr, t, mi.Name);
                    }

                }
            }
        }

        private void _pastToPanelAtSimpleRow(RibbonCommandButtonAttribute ribbAttr, CommandMethodAttribute cmdAttr, Type type, string id)
        {
            if (ribbAttr.IsCivilCmd && !Tools.IsCivil3D()) {
                return;
            }

            RibbonButton button = new RibbonButton();
            button.IsToolTipEnabled = true;
            button.Id = type.FullName + id;
            button.Name = cmdAttr.GlobalName;
            button.Orientation = System.Windows.Controls.Orientation.Horizontal;
            button.ShowText = true;
            button.GroupName = ribbAttr.GroupName;
            button.CommandHandler = new RibbonButtonCommandHandler();
            button.CommandParameter = cmdAttr.GlobalName;
            button.Size = RibbonItemSize.Standard;
            button.Text = ribbAttr.Name;
            button.GroupLocation = Autodesk.Private.Windows.RibbonItemGroupLocation.Last;

            string name = ribbAttr.GroupName;
            RibbonPanel panel = _ribbonTab.Panels.FirstOrDefault(p => p.Source.Title == name);
            if (panel == null)
            {
                panel = new RibbonPanel();
                RibbonPanelSource source = new RibbonPanelSource();
                source.Id = ribbAttr.GroupName + "___source_owner";
                source.Title = button.GroupName;
                panel.Source = source;
                _ribbonTab.Panels.Add(panel);
                //panel.Source.IsSlideOutPanelVisible = true;
                panel.CanToggleOrientation = true;
                panel.ResizeStyle = RibbonResizeStyles.NeverHideText;
                //panel.CustomPanelTitleBarBackground = System.Windows.Media.Brushes.LightYellow;
            }

            RibbonItem row = panel.Source.Items.FirstOrDefault(x =>
            {
                if (x is RibbonRowPanel)
                    if (((RibbonRowPanel)x).Items.Count / 2 < 3)
                        return true;
                return false;
            });
            if (row == null)
            {
                row = new RibbonRowPanel();
                row.Id = panel.Source.Id + "." + type.FullName + "_row";
                row.Name = type.Name;
                panel.Source.Items.Add(row);
            }

            ((RibbonRowPanel)row).Items.Add(button);
            if (row is RibbonRowPanel) {
                ((RibbonRowPanel)row).Items.Add(new RibbonRowBreak());
            }

        }

        [LispFunction("ICmd_Netload")]
        private static ResultBuffer LoadAssembly(ResultBuffer rbArgs)
        {
            ResultBuffer result = null;
            try
            {
                TypedValue[] args = rbArgs.AsArray();
                var asm = System.Reflection.Assembly.LoadFrom(args[0].Value.ToString());
                result.Add(new TypedValue(0x138D, "OK"));
            }
            catch
            {
                result.Add(new TypedValue(0x138D, "Catch Error"));
            }
            return result;
        }

        private static System.Reflection.Assembly LoadAssembly(string name)
        {
            System.Reflection.Assembly asm = null;
            try
            {
                var exAsm = System.Reflection.Assembly.GetExecutingAssembly();
                var raNames = exAsm.GetReferencedAssemblies();
                asm = System.Reflection.Assembly.LoadFrom(System.IO.Path.GetDirectoryName(exAsm.Location) + "\\" +name);
                return asm;
            }
            catch (System.Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage("\nОшибка загрузки сборки {0}\nСообщение об ошибке:", name, ex.Message);
            }
            return null;
        }

        public class RibbonButtonCommandHandler : System.Windows.Input.ICommand
        {

            public bool CanExecute(object parameter)
            {
                //return parameter is RibbonButton;
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                if (parameter is RibbonButton button)
                {
                    /*string cmd =button.CommandParameter.ToString();
                    if (cmd.ToUpper().StartsWith("@@ICmd_Netload".ToUpper()))    
                    {
                        try
                        {
                            string[] args = cmd.Substring("@@ICmd_Netload".Length).Split(new[] { "\"\\ \\\"" }, StringSplitOptions.RemoveEmptyEntries);
                            var asm = LoadAssembly(args[0].Trim('\"'));
                            if (asm == null)
                                Tools.GetAcadEditor().WriteMessage("\nОшибка загрузки, файл {0} не найден", args[0]);
                        }
                        catch (System.Exception ex)
                        { Tools.GetAcadEditor().WriteMessage("\n" +ex.Message); }
                    }
                    else*/
                    string cmd = button.CommandParameter.ToString();
                    Tools.GetActiveAcadDocument().SendStringToExecute(cmd + " ", true, false, true);
                }
            }
        }
    }
}
