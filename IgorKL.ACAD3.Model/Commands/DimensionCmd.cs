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
    public partial class DimensionCmd
    {
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_EditDimensionValueRandom", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void EditDimensionValueRandom()
        {
            var keywords = new { PositiveOnly = "PositiveOnly", NegativeOnly = "NegativeOnly", Both = "Both" };

            List<Dimension> dimensions;
            if (!ObjectCollector.TrySelectObjects(out dimensions, "\nУкажите объект размер: "))
                return;

            PromptDoubleOptions valueOption = new PromptDoubleOptions("\nУкажите значение допусуа: ");
            valueOption.AllowNone = false;
            valueOption.DefaultValue = 0d;
            valueOption.AllowNegative = false;

            PromptDoubleResult valueResult = Tools.GetAcadEditor().GetDouble(valueOption);
            if (valueResult.Status != PromptStatus.OK)
                return;

            PromptKeywordOptions options = new PromptKeywordOptions("\nВыбирите метод: ");
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

            using (Transaction trans = Tools.StartTransaction())
            {
                foreach (Dimension d in dimensions)
                {
                    Dimension dbObj = trans.GetObject(d.Id, OpenMode.ForRead) as Dimension;
                    if (dbObj == null)
                        continue;

                    /*var pInf = dbObj.GetType().GetProperties();
                    
                    foreach (var p in pInf)
                    {
                        try
                        {
                            Tools.Write(p.Name + "\t" + p.GetValue(dbObj).ToString() + "\n");
                        }
                        catch (Exception) { }
                    }*/

                    string format = "#0";
                    if (dbObj.Dimdec > 0)
                    {
                        format += ".";
                        for (int i = 0; i < dbObj.Dimdec; i++)
                            format += "0";
                    }

                    string suffix = "\\X" + (Math.Round(dbObj.Measurement, dbObj.Dimdec) + ((random.NextDouble() + ratio) * valueResult.Value * sign) * dbObj.Dimlfac).ToString(format);

                    dbObj = trans.GetObject(dbObj.Id, OpenMode.ForWrite) as Dimension;
                    dbObj.Suffix = suffix;
                }
                trans.Commit();
            }
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_AddDimensionValueRandom", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void AddDimensionValueRandom()
        {
            var keywords = new { PositiveOnly = "PositiveOnly", NegativeOnly = "NegativeOnly", Both = "Both" };

            List<Dimension> dimensions;
            if (!ObjectCollector.TrySelectObjects(out dimensions, "\nSelect dimensions"))
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

            using (Transaction trans = Tools.StartTransaction())
            {
                foreach (Dimension d in dimensions)
                {
                    Dimension dbObj = trans.GetObject(d.Id, OpenMode.ForRead) as Dimension;
                    if (dbObj == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(dbObj.Suffix))
                        continue;
                    if (!dbObj.Suffix.StartsWith("\\X"))
                        continue;
                    double val;
                    if (!double.TryParse(dbObj.Suffix.Replace("\\X", ""), out val))
                        continue;

                    string format = "#0";
                    if (dbObj.Dimdec > 0)
                    {
                        format += ".";
                        for (int i = 0; i < dbObj.Dimdec; i++)
                            format += "0";
                    }

                    string suffix = "\\X" + (Math.Round(val, dbObj.Dimdec) + ((random.NextDouble() + ratio) * valueResult.Value * sign) * dbObj.Dimlfac).ToString(format);

                    dbObj = trans.GetObject(dbObj.Id, OpenMode.ForWrite) as Dimension;
                    dbObj.Suffix = suffix;
                }
                trans.Commit();
            }
        }
    }
}
