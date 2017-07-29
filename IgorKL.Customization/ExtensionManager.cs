using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(IgorKL.ACAD3.Customization.ExtensionManager))]

namespace IgorKL.ACAD3.Customization
{
    
    public class ExtensionManager:IExtensionApplication
    {
        private static System.Collections.Concurrent.ConcurrentBag<IExtensionApplication> _applications = 
            new System.Collections.Concurrent.ConcurrentBag<IExtensionApplication>();

        public void Initialize()
        {
            try
            {
                if (Autodesk.AutoCAD.Ribbon.RibbonServices.RibbonPaletteSet != null)
                {
                    Initialize(new Ribbons.ICmdRibbonPane());
                    ribbonServices_RibbonPaletteSetCreated(Autodesk.AutoCAD.Ribbon.RibbonServices.RibbonPaletteSet, new EventArgs());
                }
                else
                    Autodesk.AutoCAD.Ribbon.RibbonServices.RibbonPaletteSetCreated += ribbonServices_RibbonPaletteSetCreated;

                IgorKL.ACAD3.Model.MainMenu.MainPaletteSet.CreateNew();
#if DEBUG
                Initialize(new GripPoints.ArrowGripOverrule());
                Initialize(new GripPoints.AnchorArrowGripOverrule());
                //Initialize(new Snap.CustomOSnapApp());
#endif
            }
            catch (System.Exception ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Initialize exception\n" + ex.Message);
            }
        }

        void ribbonServices_RibbonPaletteSetCreated(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.Ribbon.RibbonPaletteSet pset = (Autodesk.AutoCAD.Ribbon.RibbonPaletteSet)sender;
            pset.RibbonControl.Loaded += ribbonControl_Loaded;

#if ACAD2015
            pset.WorkspaceUnloading += ribbonPaletteSet_WorkspaceUnloading;
#endif
        }

// Добавил 07.08.15 не работал без акад 2015        
#if ACAD2015 
        void ribbonPaletteSet_WorkspaceUnloading(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.Ribbon.RibbonPaletteSet pset = (Autodesk.AutoCAD.Ribbon.RibbonPaletteSet)sender;
            pset.WorkspaceLoaded += ribbonPaletteSet_WorkspaceLoaded;
        }

        void ribbonPaletteSet_WorkspaceLoaded(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.Ribbon.RibbonPaletteSet pset = (Autodesk.AutoCAD.Ribbon.RibbonPaletteSet)sender;
            Initialize(_applications.First(x => x is Ribbons.ICmdRibbonPane));
            pset.WorkspaceLoaded -= ribbonPaletteSet_WorkspaceLoaded;
        }

#endif
        void ribbonControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Initialize(new Ribbons.ICmdRibbonPane());
        }

        public void Terminate()
        {
            while (_applications.Count > 0)
            {
                IExtensionApplication app;
                if (_applications.TryTake(out app))
                    app.Terminate();
            }
        }

        public void Initialize(IExtensionApplication application)
        {
            application.Initialize();
            if (_applications.FirstOrDefault(x => x == application) == null)
                _applications.Add(application);
        }


    }
}
