using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace IgorKL.ACAD3.Model.Commands
{

    public class CivilSurfaceCmd
    {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_ExtractSurfaceBorder", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_ExtractSurfaceBorder()
        {
            CivilSurface surface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out surface, "\nSelect a Surface: "))
                return;
            CivilSurfaces.SurfaceTools.ExtractBorder((ITerrainSurface)surface);
        }

        [RibbonCommandButton("Ограниченный объем", "Тест (Поверхности)")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_GetVolume", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_GetVolume()
        {
            TinVolumeSurface tinVolumeSurface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out tinVolumeSurface, "\nВыберить поверхность для вычисления объема"))
                return;

            Polyline border;
            if (!ObjectCollector.TrySelectAllowedClassObject(out border, "\nВыбирите ограничивающею полилинию"))
                return;

            var volumeInfo = CivilSurfaces.SurfaceTools.GetVolumeInfo(tinVolumeSurface, border);
            if (!volumeInfo.HasValue)
            {
                Tools.GetAcadEditor().WriteMessage("\nОшибка определения объема");
                return;
            }

            Tools.GetAcadEditor().WriteMessage(string.Format("\n\nПоверхность: {0}", tinVolumeSurface.Name));

            Tools.GetAcadEditor().WriteMessage(string.Format("\nНасыпь составила: {0}\nВыемка составила: {1}\nЧистый объем: {2}",
                volumeInfo.Value.Fill.ToString("#0.00"), volumeInfo.Value.Cut.ToString("#0.00"), volumeInfo.Value.Net.ToString("#0.00")));

        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_GetCroppingSurface", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_GetCroppingSurface()
        {
            TinSurface surface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out surface))
                return;
            Polyline border;
            if (!ObjectCollector.TrySelectAllowedClassObject(out border))
                return;

            CivilSurfaces.SurfaceTools.CroppingSurface5(surface, border);
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_CloningSurface", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_CloningSurface()
        {
            TinSurface surface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out surface))
                return;
            TinSurface newSurface = surface.Clone() as TinSurface;
            newSurface.Name = "Clone_" + surface.Name;
            using (Transaction trans = Tools.StartTransaction())
            {
                Tools.AppendEntityEx(trans, newSurface);
            }
        }

        [RibbonCommandButton("Выбрать поверхность", "Тест (Поверхности)")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iGuid_GetSelectSurfaceForm")]
        public void GetSelectSurfaceForm()
        {
            IgorKL.ACAD3.Model.CivilSurfaces.Views.FormSelect form = new CivilSurfaces.Views.FormSelect();
            
            var names = IgorKL.ACAD3.Model.CivilSurfaces.SurfaceTools.GetAllSurfaceNames();
            foreach (var n in names)
            {
                form.AddSurfaceName(n);
            }

            if (Application.ShowModalDialog(form) == System.Windows.Forms.DialogResult.OK)
            {
                Tools.GetAcadEditor().WriteMessage("\n" + form.SelectedSurfaceName);
            }
        }
#endif
    }
}
