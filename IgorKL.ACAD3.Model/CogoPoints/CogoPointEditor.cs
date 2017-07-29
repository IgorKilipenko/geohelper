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


namespace IgorKL.ACAD3.Model.CogoPoints
{
    public static class CogoPointEditor
    {

        public static void EditElevation(IEnumerable<CogoPoint> points, double value)
        {
            foreach (CogoPoint point in points)
            {
                point.Elevation += value;
            }
        }

        public static Autodesk.Civil.DatabaseServices.Styles.PointStyleCollection  GetAllPointsStyles()
        {
            return Tools.GetActiveCivilDocument().Styles.PointStyles;
        }

        public static Autodesk.Civil.DatabaseServices.Styles.LabelStyleCollection GetAllPointsLabelStyles()
        {
            return Tools.GetActiveCivilDocument().Styles.LabelStyles.PointLabelStyles.LabelStyles;
        }

        public static Autodesk.Civil.Settings.SettingsPoint GetPointSettings()
        {
            Autodesk.Civil.Settings.SettingsPoint pointSettings =
                Tools.GetActiveCivilDocument().Settings.GetSettings<Autodesk.Civil.Settings.SettingsPoint>()
                as Autodesk.Civil.Settings.SettingsPoint;
            return pointSettings;
        }

        public static ObjectId GeDefailtPointStyleId()
        {
            var settings = GetPointSettings();
            return settings.Styles.PointStyleId.Value;
        }
        public static string GeDefailtPointStyleName()
        {
            var settings = GetPointSettings();
            return settings.Styles.Point.Value;
        }

        public static ObjectId GeDefailtPointLableStyleId()
        {
            var settings = GetPointSettings();
            return settings.Styles.PointLabelStyleId.Value;
        }
        public static string GeDefailtPointLableStyleName()
        {
            var settings = GetPointSettings();
            return settings.Styles.PointLabel.Value;
        }
    }
}
