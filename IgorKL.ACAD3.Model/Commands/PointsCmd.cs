using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Autodesk.Civil.DatabaseServices;

using IgorKL.ACAD3.Model.Helpers.SdrFormat;
using wnd = System.Windows.Forms;

namespace IgorKL.ACAD3.Model.Commands
{
    public partial class PointsCmd
    {
        [RibbonCommandButton("Импорт точек Sdr", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_ImportSdrData", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void ImportSdrData()
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            SdrReader rd = new SdrReader();
            wnd.OpenFileDialog opfWndDia = new wnd.OpenFileDialog();
            opfWndDia.AddExtension = true;
            opfWndDia.Filter = "Text files (*.txt)|*.txt|Sdr files (*.sdr)|*.sdr|All files (*.*)|*.*";
            opfWndDia.FilterIndex = 2;
            if (opfWndDia.ShowDialog() == wnd.DialogResult.OK)
            {
                PromptIntegerOptions scaleOpt = new PromptIntegerOptions("\nУкажите маштаб, 1:");
                scaleOpt.UseDefaultValue = true;
                scaleOpt.DefaultValue = 1000;
                scaleOpt.AllowNegative = false;
                scaleOpt.AllowZero = false;
                scaleOpt.AllowNone = false;
                PromptIntegerResult scaleRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(scaleOpt);
                if (scaleRes.Status != PromptStatus.OK)
                    return;
                double scale = scaleRes.Value / 1000d;

                PromptIntegerOptions digCountOpt = new PromptIntegerOptions("\nЗнаков после запятой");
                digCountOpt.UseDefaultValue = true;
                digCountOpt.DefaultValue = 2;
                digCountOpt.AllowNegative = false;
                PromptIntegerResult digCountRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(digCountOpt);
                if (digCountRes.Status != PromptStatus.OK)
                    return;

                string path = opfWndDia.FileName;
                var points = rd._SdrCoordParser(path);
                if (points == null)
                    return;
                string lname = "__SDRP_" + System.IO.Path.GetFileNameWithoutExtension(opfWndDia.SafeFileName);
                string lnameElev = lname + "__Elevations";
                string lnameName = lname + "__Names";
                Layers.LayerTools.CreateHiddenLayer(lname);
                Layers.LayerTools.CreateHiddenLayer(lnameElev);
                Layers.LayerTools.CreateHiddenLayer(lnameName);

                using (Transaction trans = Tools.StartTransaction())
                {
                    BlockTable acBlkTbl;
                    acBlkTbl = trans.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                                                 OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;

                    foreach (var p in points)
                    {

                        DBPoint acPoint = new DBPoint(new Point3d(p.y, p.x, p.h));
                        acPoint.Layer = lname;
                        acPoint.SetDatabaseDefaults();
                        Group gr = new Group();

                        string format = digCountRes.Value == 0 ? "#0" : ((Func<string>)(() => { format = "#0."; for (int i = 0; i < digCountRes.Value; i++) format += "0"; return format; })).Invoke();

                        var text = _CreateText(new Point3d(p.y + 2.0 * scale, p.x, 0), Math.Round(p.h, digCountRes.Value, MidpointRounding.AwayFromZero).ToString(format, culture), lnameElev, scale);

                        var nameText = _CreateText(text, p.name, lnameName, scale);

                        acBlkTblRec.AppendEntity(acPoint);
                        trans.AddNewlyCreatedDBObject(acPoint, true);

                        ObjectId elevId = acBlkTblRec.AppendEntity(text);
                        trans.AddNewlyCreatedDBObject(text, true);


                        ObjectId nameId = acBlkTblRec.AppendEntity(nameText);
                        trans.AddNewlyCreatedDBObject(nameText, true);

                        gr.Append(elevId);
                        gr.Append(nameId);
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.AddDBObject(gr);

                        trans.AddNewlyCreatedDBObject(gr, true);

                    }
                    Application.DocumentManager.MdiActiveDocument.Database.Pdmode = 32;
                    Application.DocumentManager.MdiActiveDocument.Database.Pdsize = 2 * scale;
                    trans.Commit();
                }

            }
        }
        [RibbonCommandButton("Импорт точек Sdr Cogo", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_ImportSdrDataCivil", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void ImportSdrDataCivil()
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            SdrReader rd = new SdrReader();
            wnd.OpenFileDialog opfWndDia = new wnd.OpenFileDialog();
            opfWndDia.AddExtension = true;
            opfWndDia.Filter = "Text files (*.txt)|*.txt|Sdr files (*.sdr)|*.sdr|All files (*.*)|*.*";
            opfWndDia.FilterIndex = 2;
            
            if (opfWndDia.ShowDialog() == wnd.DialogResult.OK)
            {
                string path = opfWndDia.FileName;
                var points = rd._SdrCoordParser(path);
                if (points == null)
                    return;
                foreach (var p in points)
                {
                    IgorKL.ACAD3.Model.CogoPoints.CogoPointFactory.CreateCogoPoints(new Point3d(p.y, p.x, p.h), p.name, p.code2);
                }
            }
        }

        [RibbonCommandButton("Экспорт точек в Sdr", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_ExportSdrData", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void ExportSdrData()
        {

            IList<DBPoint> points;
            if (!TrySelectObjects<DBPoint>(out points, OpenMode.ForRead, "\nВыберите точки"))
                return;
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new wnd.SaveFileDialog();
            string pattern = "#0.000";
            saveFileDialog1.DefaultExt = "sdr";
            saveFileDialog1.AddExtension = true;
            System.Windows.Forms.DialogResult dres = saveFileDialog1.ShowDialog();

            if (dres == System.Windows.Forms.DialogResult.OK || dres == System.Windows.Forms.DialogResult.Yes)
            {
                string path = saveFileDialog1.FileName;

                StringBuilder sb = new StringBuilder();
                int i = 0;
                foreach (DBPoint p in points)
                {
                    var line = SdrWriter.SdrFormatter.CreateSdrLine(false);
                    line.Fields[0].Value = "08KI";
                    line.Fields[1].Value = "num_" + (++i).ToString("#0", culture);
                    line.Fields[2].Value = p.Position.Y.ToString(pattern, culture);
                    line.Fields[3].Value = p.Position.X.ToString(pattern, culture);
                    line.Fields[4].Value = p.Position.Z.ToString(pattern, culture);
                    line.Fields[5].Value = "A";
                    if (line.Fields.Count >= 7)
                        line.Fields[6].Value = "STN";
                    sb.AppendLine(line.ToString());
                }
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                {
                    sw.Write(sb);
                }
            }
        }

        [RibbonCommandButton("Точки из буфера", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_CreateAcadPointsFromBuffer")]
        public void iCmd_CreateAcadPointsFromBuffer()
        {
            PromptKeywordOptions kwOpt = new PromptKeywordOptions("\nSelect");
            kwOpt.AllowNone = false;
            kwOpt.Keywords.Add("FromText");
            //kwOpt.Keywords.Add("FromExl");
            kwOpt.Keywords.Add("FromAcadText");
            //kwOpt.Keywords.Add("FromAcadMText");

            PromptResult kwRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetKeywords(kwOpt);

            string data = "";

            if (kwRes.Status != PromptStatus.OK)
                return;

            PromptIntegerOptions scaleOpt = new PromptIntegerOptions("\nSpecify the scale, 1: ");
            scaleOpt.UseDefaultValue = true;
            scaleOpt.DefaultValue = 1000;
            scaleOpt.AllowNegative = false;
            scaleOpt.AllowZero = false;
            scaleOpt.AllowNone = false;
            PromptIntegerResult scaleRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(scaleOpt);
            if (scaleRes.Status != PromptStatus.OK)
                return;
            double scale = scaleRes.Value / 1000d;

            PromptIntegerOptions digCountOpt = new PromptIntegerOptions("\nNumber of decimal places: ");
            digCountOpt.UseDefaultValue = true;
            digCountOpt.DefaultValue = 2;
            digCountOpt.AllowNegative = false;
            PromptIntegerResult digCountRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(digCountOpt);
            if (digCountRes.Status != PromptStatus.OK)
                return;

            PromptKeywordOptions groupingOpt = new PromptKeywordOptions("\nGroup data? : ");
            groupingOpt.AllowNone = false;
            groupingOpt.Keywords.Add("Yes");
            groupingOpt.Keywords.Add("No");
            groupingOpt.Keywords.Default = "Yes";
            PromptResult groupingRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetKeywords(groupingOpt);
            if (groupingRes.Status != PromptStatus.OK)
                return;

            string lname = "__CLIPP" + "POINTS";
            string lnameElev = lname + "__Elevations";
            string lnameName = lname + "__Names";
            Layers.LayerTools.CreateHiddenLayer(lname);
            Layers.LayerTools.CreateHiddenLayer(lnameElev);
            Layers.LayerTools.CreateHiddenLayer(lnameName);

            switch (kwRes.StringResult)
            {
                case "FromText":
                    {
                        data = System.Windows.Forms.Clipboard.GetText();
                        break;
                    }
                /*case "FromExl":
                    {
                        var buff = System.Windows.Forms.Clipboard.GetData(System.Windows.Forms.DataFormats.CommaSeparatedValue.ToString());
                        if (buff is String)
                            data = (String)buff;
                        break;
                    }*/
                case "FromAcadText":
                    {
                        data = "\t" + _getDbTextString("\nSelect text", "is't DBText");
                        data += "\t" + _getDbTextString("\nSelect text", "is't DBText");
                        break;
                    }
                default:
                    return;
            }

            data = data.Replace(',', '.');
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(data);
            string[] lines = data.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            using (Transaction trans = Tools.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = trans.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;
                int count = lines.Length;
                foreach (string l in lines)
                {
                    string[] coords = l.Split(new char[] { '\t' }, StringSplitOptions.None);
                    try
                    {
                        string name = coords[0];
                        double y = double.Parse(coords[1], culture);
                        double x = double.Parse(coords[2], culture);
                        double h = 0d;
                        ObjectId nameId = ObjectId.Null;
                        ObjectId pointId = ObjectId.Null;
                        ObjectId elevId = ObjectId.Null;
                        if (coords.Length > 3)
                        {
                            try
                            {
                                h = double.Parse(coords[3], culture);
                            }
                            catch { }
                            if (coords[3].Length > 0)
                            {
                                string f = "#0";
                                if (digCountRes.Value > 0)
                                    f = (f += ".").PadRight(f.Length + digCountRes.Value, '0');
                                var text = _CreateText(new Point3d(x + 2d * scale, y, 0d), h.ToString(f), lnameElev, scale);
                                elevId = acBlkTblRec.AppendEntity(text);
                                trans.AddNewlyCreatedDBObject(text, true);
                            }
                        }
                        DBPoint point = new DBPoint(new Point3d(x, y, h));
                        point.SetDatabaseDefaults();
                        point.Layer = lname;
                        pointId = acBlkTblRec.AppendEntity(point);
                        trans.AddNewlyCreatedDBObject(point, true);

                        if (name.Length > 0)
                        {
                            var text = _CreateText(new Point3d(x + 2d * scale, y + 3.0d * scale, 0d), name, lnameName, scale);
                            nameId = acBlkTblRec.AppendEntity(text);
                            trans.AddNewlyCreatedDBObject(text, true);
                        }

                        if (groupingRes.StringResult == "Yes")
                        {
                            Group gr = new Group();
                            if (!nameId.IsNull)
                                gr.Append(nameId);
                            if (!elevId.IsNull)
                                gr.Append(elevId);
                            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.AddDBObject(gr);
                            trans.AddNewlyCreatedDBObject(gr, true);
                        }
                    }
                    catch
                    { count -= 1; }
                }
                Application.DocumentManager.MdiActiveDocument.Database.Pdmode = 32;
                Application.DocumentManager.MdiActiveDocument.Database.Pdsize = 2 * scale;
                trans.Commit();

            }
        }
        [RibbonCommandButton("Текст в буфер", RibbonPanelCategories.Text_Annotations)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_SetTextDataToBuffer", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void SetTextDataToBuffer()
        {
            const string sep = "\t";
            List<DBText> data;
            if (!ObjectCollector.TrySelectObjects(out data, "\nВыберете текстовые объекты для импорта в буфер обмена: "))
                return;
            if (data.Count == 0)
                return;
            string res = "";
            using (Transaction trans = Tools.StartTransaction())
            {
                foreach (var text in data)
                {
                    res += text.TextString + sep;
                }
            }
            res = res.Remove(res.Length - sep.Length);
            System.Windows.Forms.Clipboard.SetText(res);
        }


        [RibbonCommandButton("Круги в точки", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_ConvertCircleToPoint", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void iCmd_ConvertCircleToPoint()
        {
            IList<Circle> circles;
            if (!TrySelectObjects<Circle>(out circles, OpenMode.ForRead, "\nВыберите круги: "))
                return;
            try
            {

                using (Transaction trans = Tools.StartTransaction())
                {
                    BlockTable acBlkTbl;
                    acBlkTbl = trans.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                                                    OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;
                    foreach (var circle in circles)
                    {
                        DBPoint point = new DBPoint(new Point3d(circle.Center.ToArray()));
                        point.SetDatabaseDefaults();
                        acBlkTblRec.AppendEntity(point);
                        trans.AddNewlyCreatedDBObject(point, true);

                    }
                    trans.Commit();
                }
            }
            catch (Exception ex) { Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.Message); }
        }

        [RibbonCommandButton("Cogo точки из буфера", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_CreateCogoPointsFromBuffer")]
        public void CreateCogoPointsFromBuffer()
        {
            IgorKL.ACAD3.Model.CogoPoints.Views.PointStylesSelectorControl control = null;
            IgorKL.ACAD3.Model.MainMenu.MainPaletteSet ps = null;

            if (IgorKL.ACAD3.Model.MainMenu.MainPaletteSet.CreatedInstance.FindVisual("Cogo точки из буфера") == null)
            {
                control = new CogoPoints.Views.PointStylesSelectorControl();
                ps = IgorKL.ACAD3.Model.MainMenu.MainPaletteSet.CreatedInstance;
                control = (IgorKL.ACAD3.Model.CogoPoints.Views.PointStylesSelectorControl)ps.AddControl("Cogo точки из буфера", control);
                ps.Show();
                control.CommandAction = CreateCogoPointsFromBuffer;
                //control.CommandAction = () => Application.DocumentManager.ExecuteInApplicationContext(x => CreateCogoPointsFromBuffer(), null);
            }
            else
            //control.CommandAction = () =>
            {
                ps = IgorKL.ACAD3.Model.MainMenu.MainPaletteSet.CreatedInstance;
                control = ps.FindVisual("Cogo точки из буфера") as IgorKL.ACAD3.Model.CogoPoints.Views.PointStylesSelectorControl;
                if (control == null)
                    return;
                if (!ps.Visible)
                {
                    ps.Show();
                    return;
                }
                
                string separator = "\t";
                string data = System.Windows.Forms.Clipboard.GetText();
                data = data.Replace(',', '.');
                string[] lines = data.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);

                using (Transaction trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] fields = line.Split(new[] { separator }, StringSplitOptions.None);
                        if (fields.Length < 3)
                            continue;

                        try
                        {
                            string name = fields[0];
                            double north = double.Parse(fields[1], System.Globalization.NumberStyles.Number, Tools.Culture);
                            double east = double.Parse(fields[2], System.Globalization.NumberStyles.Number, Tools.Culture);
                            double elevation = 0;
                            if (fields.Length > 3)
                                elevation = double.Parse(fields[3], System.Globalization.NumberStyles.Number, Tools.Culture);
                            string description = "_PointsFromBuffer";
                            if (fields.Length > 4)
                                description = fields[4];

                            ObjectId pointId = IgorKL.ACAD3.Model.CogoPoints.CogoPointFactory.CreateCogoPoints(new Point3d(east, north, elevation), name, description);
                            var point = trans.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;

                            point.SetDatabaseDefaults();

                            point.LabelStyleId = control.SelectedPointLabelStyle.Key;
                            point.StyleId = control.SelectedPointStyle.Key;

                            /*point.LabelStyleId = control.SelectedPointLabelStyle.Key;
                            point.StyleId = control.SelectedPointStyle.Key;*/

                        }
                        catch (System.Exception ex)
                        {
                            Tools.GetAcadEditor().WriteMessage(string.Format("\nCreate point error, message: {0}", ex.Message));
                        }
                    }
                    HostApplicationServices.WorkingDatabase.TransactionManager.QueueForGraphicsFlush();
                    trans.Commit();
                }
            };

            

            /*
            string separator = "\t";
            string data = System.Windows.Forms.Clipboard.GetText();
            data = data.Replace(',', '.');
            string[] lines = data.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);

            using (Transaction trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] fields = line.Split(new[] { separator }, StringSplitOptions.None);
                    if (fields.Length < 3)
                        continue;

                    try
                    {
                        string name = fields[0];
                        double north = double.Parse(fields[1], System.Globalization.NumberStyles.Number, Tools.Culture);
                        double east = double.Parse(fields[2], System.Globalization.NumberStyles.Number, Tools.Culture);
                        double elevation = 0;
                        if (fields.Length > 3)
                            elevation = double.Parse(fields[3], System.Globalization.NumberStyles.Number, Tools.Culture);
                        string description = "_PointsFromBuffer";
                        if (fields.Length > 4)
                            description = fields[4];

                        ObjectId pointId = IgorKL.ACAD3.Model.CogoPoints.CogoPointFactory.CreateCogoPoints(new Point3d(east, north, elevation), name, description);
                        var point = trans.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;

                        point.SetDatabaseDefaults();

                        point.LabelStyleId = control.SelectedPointLabelStyle.Key;
                        point.StyleId = control.SelectedPointStyle.Key;

                        /*point.LabelStyleId = control.SelectedPointLabelStyle.Key;
                        point.StyleId = control.SelectedPointStyle.Key;*/

                    /*}
                    catch (System.Exception ex)
                    {
                        Tools.GetAcadEditor().WriteMessage(string.Format("\nCreate point error, message: {0}", ex.Message));
                    }
                }
                trans.Commit();
            }*/

        }
        
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_EditPointElevation", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void EditPointElevation()
        {
            List<CogoPoint> points;
            if (!ObjectCollector.TrySelectObjects(out points, "\nSelect points"))
                return;

            PromptDoubleOptions valueOption = new PromptDoubleOptions("\nEnter value");
            valueOption.AllowNone = false;
            valueOption.DefaultValue = 0d;

            PromptDoubleResult valueResult = Tools.GetAcadEditor().GetDouble(valueOption);
            if (valueResult.Status != PromptStatus.OK)
                return;

            IgorKL.ACAD3.Model.CogoPoints.CogoPointEditor.EditElevation(points, valueResult.Value);
        }

        [RibbonCommandButton("Случайная отметка", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_EditPointElevationRandom", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void EditPointElevationRandom()
        {
            var keywords = new { PositiveOnly = "PositiveOnly", NegativeOnly = "NegativeOnly", Both = "Both" };

            List<CogoPoint> points;
            if (!ObjectCollector.TrySelectObjects(out points, "\nSelect points"))
                return;

            PromptDoubleOptions valueOption = new PromptDoubleOptions("\nEnter value");
            valueOption.AllowNone = false;
            valueOption.DefaultValue = 0d;
            valueOption.AllowNegative = false;

            PromptDoubleResult valueResult = Tools.GetAcadEditor().GetDouble(valueOption);
            if (valueResult.Status != PromptStatus.OK)
                return;

            PromptKeywordOptions options = new PromptKeywordOptions("\nEnter method");
            options.AppendKeywordsToMessage = true;
            options.AllowArbitraryInput = true;
            options.Keywords.Add(keywords.PositiveOnly);
            options.Keywords.Add(keywords.NegativeOnly);
            options.Keywords.Add(keywords.Both);
            options.AppendKeywordsToMessage = true;
            options.AllowNone = true;
            options.Keywords.Default = keywords.Both;

            PromptResult keywordResult = Tools.GetAcadEditor().GetKeywords(options);
            if (keywordResult.Status != PromptStatus.OK)
                return;

            double ratio = keywordResult.StringResult == keywords.Both ? -0.5 : 0d;
            double sign = keywordResult.StringResult == keywords.NegativeOnly ? -1d : 1d;
            if (keywordResult.StringResult == keywords.Both)
                sign = 2d;

            Random random = new Random(DateTime.Now.Second);

            foreach (CogoPoint point in points)
                point.Elevation += (random.NextDouble() + ratio) * valueResult.Value * sign;
        }

        [RibbonCommandButton("Описание в Имя Cogo", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_ReplacePointDescriptionToName", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void ReplacePointDescriptionToName()
        {
            List<CogoPoint> points;
            if (!ObjectCollector.TrySelectObjects(out points, "\nSelect points"))
                return;
            foreach (CogoPoint point in points)
                point.PointName = point.FullDescription;
        }
    }
}
