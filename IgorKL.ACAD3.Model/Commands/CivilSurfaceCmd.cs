using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace IgorKL.ACAD3.Model.Commands {

    public class CivilSurfaceCmd {
#if DEBUG
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_ExtractSurfaceBorder", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_ExtractSurfaceBorder() {
            if (!ObjectCollector.TrySelectAllowedClassObject(out CivilSurface surface, "\nSelect a Surface: "))
                return;
            CivilSurfaces.SurfaceTools.ExtractBorder((ITerrainSurface)surface);
        }

        [RibbonCommandButton("Ограниченный объем", "Тест (Поверхности)", true)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_GetVolume", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_GetVolume() {
            if (!ObjectCollector.TrySelectAllowedClassObject(out TinVolumeSurface tinVolumeSurface, "\nВыбрать поверхность для вычисления объема"))
                return;

            if (!ObjectCollector.TrySelectAllowedClassObject(out Polyline border, "\nВыберите ограничивающею полилинию"))
                return;

            var volumeInfo = CivilSurfaces.SurfaceTools.GetVolumeInfo(tinVolumeSurface, border);
            if (!volumeInfo.HasValue) {
                Tools.GetAcadEditor().WriteMessage("\nОшибка определения объема");
                return;
            }

            Tools.GetAcadEditor().WriteMessage(string.Format("\n\nПоверхность: {0}", tinVolumeSurface.Name));

            Tools.GetAcadEditor().WriteMessage(string.Format("\nНасыпь составила: {0}\nВыемка составила: {1}\nЧистый объем: {2}",
                volumeInfo.Value.Fill.ToString("#0.00"), volumeInfo.Value.Cut.ToString("#0.00"), volumeInfo.Value.Net.ToString("#0.00")));

        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_GetCroppingSurface", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_GetCroppingSurface() {
            if (!ObjectCollector.TrySelectAllowedClassObject(out TinSurface surface))
                return;
            if (!ObjectCollector.TrySelectAllowedClassObject(out Polyline border))
                return;

            CivilSurfaces.SurfaceTools.CroppingSurface5(surface, border);
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_TEST_CloningSurface", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void TEST_CloningSurface() {
            if (!ObjectCollector.TrySelectAllowedClassObject(out TinSurface surface))
                return;

            TinSurface newSurface = surface.Clone() as TinSurface;
            newSurface.Name = "Clone_" + surface.Name;
            using (Transaction trans = Tools.StartTransaction()) {
                Tools.AppendEntityEx(trans, newSurface);
            }
        }

        [RibbonCommandButton("Выбрать поверхность", "Тест (Поверхности)", true)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iGuid_GetSelectSurfaceForm")]
        public void GetSelectSurfaceForm() {
            CivilSurfaces.Views.FormSelect form = new CivilSurfaces.Views.FormSelect();

            var names = Model.CivilSurfaces.SurfaceTools.GetAllSurfaceNames();
            foreach (var n in names) {
                form.AddSurfaceName(n);
            }

            if (Application.ShowModalDialog(form) == System.Windows.Forms.DialogResult.OK) {
                Tools.GetAcadEditor().WriteMessage("\n" + form.SelectedSurfaceName);
            }
        }
#endif
    }
}
