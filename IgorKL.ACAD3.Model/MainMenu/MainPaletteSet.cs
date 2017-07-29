using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

namespace IgorKL.ACAD3.Model.MainMenu
{
    public class MainPaletteSet:PaletteSet
    {
        Dictionary<string, ControlStateInfo> _controls;

        public static MainPaletteSet CreatedInstance { get; private set; }

        public static string Name { get { return "ТочностиНЕТ (Настройки)"; } }

        public static event EventHandler PaletteSetClosed;

        private MainPaletteSet()
            : base(Name, new Guid("{F33CED68-5404-4267-90EB-C58A975D12FD}"))
        {
            _controls = new Dictionary<string, ControlStateInfo>();
            this.StateChanged +=
                  (s, e) =>
                  {
                      // On hide we fire a command to check the state properly

                      if (e.NewState == StateEventIndex.Hide)
                      {
                          CheckPaletteSetState();
                      }
                  };
            this.Save += MainPaletteSet_Save;
            this.Load += MainPaletteSet_Load;

            this.Size = new System.Drawing.Size(200, 600);
            this.Style = PaletteSetStyles.NameEditable | PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton
                | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu | PaletteSetStyles.ShowCloseButton;
            this.Dock = DockSides.None;
            this.Location = new System.Drawing.Point(100, 100);
            this.DockEnabled = DockSides.None | DockSides.Left | DockSides.Right;
            PaletteSetClosed += MainPaletteSet_PaletteSetClosed;

            Application.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
        }

        void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            _clearViews();
            _controls.Clear();
            /*CreatedInstance.Close();
            CreatedInstance = null;
            CreatedInstance = CreateNew();*/
            
            /*foreach (var item in _controls)
            {
                var view = System.Windows.Media.VisualTreeHelper.GetParent(item.Value.View);
                if (item.Value.HostDocument == e.Document)
                    this.AddControl(item.Key, item.Value.View);
            }*/
        }


        void MainPaletteSet_Load(object sender, PalettePersistEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MainPaletteSet_Save(object sender, PalettePersistEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MainPaletteSet_PaletteSetClosed(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }



        public void CheckPaletteSetState()
        {
            // If it's invisible, it has been closed

            if (!this.Visible)
            {
                if (PaletteSetClosed != null)
                {
                    this.RemoveAllControls();
                    PaletteSetClosed(this, new EventArgs());
                }
            }
        }

        public static MainPaletteSet CreateNew()
        {
            if (CreatedInstance == null)
            {
                var main = new MainPaletteSet();
                MainPaletteSet.CreatedInstance = main;
                return main;
            }
            else
                throw new NotSupportedException("Меню настоек уже созданно");
        }

        public void Show()
        {
            this.Visible = true;
        }

        public void Hide()
        {
            this.Visible = false;
        }

        public System.Windows.Media.Visual AddControl(string name, System.Windows.Media.Visual control)
        {
            if (_controls.ContainsKey(name))
                control = _controls[name].View;
            else
            {
                _controls.Add(name, new ControlStateInfo() { View = control, HostDocument = Tools.GetActiveAcadDocument() });
                base.AddVisual(name, control, true);
            }
            if (FindVisual(name) == null)
                base.AddVisual(name, control);
            return control;
        }
        
        public System.Windows.Media.Visual FindControl(string name)
        {
            return _controls.FirstOrDefault(x => x.Key == name).Value.View;
        }
        public bool RemoveControl(string name)
        {
            if (!_controls.ContainsKey(name))
                return false;
            var item = _controls[name];

            for (int i = 0; i < base.Count; i++ )
            {
                if (base[i].Name == name)
                {
                    
                    base.Remove(i);
                    _controls.Remove(name);
                    var view = System.Windows.Media.VisualTreeHelper.GetParent(item.View);
                    
                    return true;
                }
            }

            return false;
        }
        public void RemoveAllControls()
        {
            _clearViews();
            _controls.Clear();
        }

        private void _clearViews()
        {
            for (int i = 0; i < base.Count; i++)
            {
                base.Remove(i);
            }
        }

        private void _addControlsToView()
        {
            foreach (var control in _controls)
                this.AddVisual(control.Key, control.Value.View, true);
        }

        public System.Windows.Media.Visual FindVisual(string name)
        {
            for (int i = 0; i < base.Count; i++)
                if (base[i].Name == name)
                    return FindControl(name);
            return null;
        }

        private class ControlStateInfo
        {
            public System.Windows.Media.Visual View { get; set; }
            public int Index { get; set; }
            public Document HostDocument { get; set; }
        }
    }

    /*
    public class MainPaletteSet : PaletteSet
    {
        Dictionary<string, ControlStateInfo> _controls;

        public static MainPaletteSet CreatedInstance { get; private set; }

        public static string Name { get { return "ТочностиНЕТ (Настройки)"; } }

        public static event EventHandler PaletteSetClosed;

        private MainPaletteSet()
            : base(Name, new Guid("{F33CED68-5404-4267-90EB-C58A975D12FD}"))
        {
            _controls = new Dictionary<string, ControlStateInfo>();
            this.StateChanged +=
                  (s, e) =>
                  {
                      // On hide we fire a command to check the state properly

                      if (e.NewState == StateEventIndex.Hide)
                      {
                          CheckPaletteSetState();
                      }
                  };
            this.Save += MainPaletteSet_Save;
            this.Load += MainPaletteSet_Load;

            this.Size = new System.Drawing.Size(300, 600);
            this.Style = PaletteSetStyles.NameEditable | PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton
                | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu | PaletteSetStyles.ShowCloseButton;
            this.Dock = DockSides.None;
            this.Location = new System.Drawing.Point(200, 200);
            PaletteSetClosed += MainPaletteSet_PaletteSetClosed;

            Application.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
        }

        void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            _clearViews();

            foreach (var item in _controls)
            {
                var view = System.Windows.Media.VisualTreeHelper.GetParent(item.Value.View);
                if (item.Value.HostDocument == e.Document)
                    this.AddControl(item.Key, item.Value.View);
            }
        }


        void MainPaletteSet_Load(object sender, PalettePersistEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MainPaletteSet_Save(object sender, PalettePersistEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MainPaletteSet_PaletteSetClosed(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }



        public void CheckPaletteSetState()
        {
            // If it's invisible, it has been closed

            if (!this.Visible)
            {
                if (PaletteSetClosed != null)
                {
                    this.RemoveAllControls();
                    PaletteSetClosed(this, new EventArgs());
                }
            }
        }

        public static MainPaletteSet CreateNew()
        {
            if (CreatedInstance == null)
            {
                var main = new MainPaletteSet();
                MainPaletteSet.CreatedInstance = main;
                return main;
            }
            else
                throw new NotSupportedException("Меню настоек уже созданно");
        }

        public void Show()
        {
            this.Visible = true;
        }

        public void Hide()
        {
            this.Visible = false;
        }

        public System.Windows.Media.Visual AddControl(string name, System.Windows.Media.Visual control)
        {
            if (_controls.ContainsKey(name))
                control = _controls[name].View;
            else
            {
                _controls.Add(name, new ControlStateInfo() { View = control, HostDocument = Tools.GetActiveAcadDocument() });
                base.AddVisual(name, control, true);
            }
            if (FindVisual(name) == null)
                base.AddVisual(name, control);
            return control;
        }

        public System.Windows.Media.Visual FindControl(string name)
        {
            return _controls.FirstOrDefault(x => x.Key == name).Value.View;
        }
        public bool RemoveControl(string name)
        {
            if (!_controls.ContainsKey(name))
                return false;
            var item = _controls[name];

            for (int i = 0; i < base.Count; i++)
            {
                if (base[i].Name == name)
                {

                    base.Remove(i);
                    _controls.Remove(name);
                    var view = System.Windows.Media.VisualTreeHelper.GetParent(item.View);

                    return true;
                }
            }

            return false;
        }
        public void RemoveAllControls()
        {
            _clearViews();
            _controls.Clear();
        }

        private void _clearViews()
        {
            for (int i = 0; i < base.Count; i++)
            {
                var item = _controls.Values.ElementAt(i);
                if (item != null)
                {
                    var view = System.Windows.Media.VisualTreeHelper.GetParent(item.View);
                    var newControl = new System.Windows.Controls.UserControl();
                    var obj = ((System.Windows.Controls.UserControl)view).Content;/
                }
                base.Remove(i);
            }
        }

        private void _addControlsToView()
        {
            foreach (var control in _controls)
                this.AddVisual(control.Key, control.Value.View, true);
        }

        private System.Windows.Media.Visual FindVisual(string name)
        {
            for (int i = 0; i < base.Count; i++)
                if (base[i].Name == name)
                    return FindControl(name);
            return null;
        }

        private class ControlStateInfo
        {
            public System.Windows.Media.Visual View { get; set; }
            public int Index { get; set; }
            public Document HostDocument { get; set; }
        }
    }*/
}
