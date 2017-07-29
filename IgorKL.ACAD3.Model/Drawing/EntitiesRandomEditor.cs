using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class EntitiesRandomEditor
    {
        private static MainMenu.HostProvider _dataHost = new MainMenu.HostProvider(new EntitiesRandomEditor());

        private double _maxTolerance;
        private double _minTolerance;
        private string _format;
        private List<Entity> _entities;
        private System.Globalization.CultureInfo _culture;

        public EntitiesRandomEditor()
        {
            if (_dataHost == null)
                _dataHost = new MainMenu.HostProvider(this);

            _culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            _maxTolerance = /*0.50;*/ _dataHost.Read("maxTolerance", 0.05d);
            _minTolerance = /*-0.50;*/ _dataHost.Read("minTolerance", -0.05d);
            _format = _dataHost.Read("format", "");
            _entities = new List<Entity>();
        }

        private void _editEntity(Entity ent)
        {
            if (ent is DBText)
                _editDbText((DBText)ent);
            else if (ent is MText)
                _editMText((MText)ent);
        }
#if DEBUG

        [RibbonCommandButton("Изм текст случайно", "Тест/Текст")]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_TestEditText", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void TestEditText()
        {
            string controlName = "Рандом редактор (Настройки)";

            Views.EntitiesRandomEditorView view = null;
            MainMenu.MainPaletteSet pset = MainMenu.MainPaletteSet.CreatedInstance;
            if ((view = pset.FindVisual(controlName) as Views.EntitiesRandomEditorView) == null)
            {
                view = new Views.EntitiesRandomEditorView();
                view.CommandAction = TestEditText;
                view.DataHost = _dataHost;
                pset.AddControl(controlName, view);
                pset.Show();
                return;
            }
            pset.Show();

            EntitiesRandomEditor mainBlock = new EntitiesRandomEditor();

            List<DBObject> objects;
            if (!ObjectCollector.TrySelectObjects(out objects))
                return;
            if (objects.Count == 0)
                return;
            
            Tools.StartTransaction(() =>{
            foreach (DBObject obj in objects)
            {
                if (obj is Entity)
                    mainBlock._editEntity((Entity)obj);
            }});

            _dataHost.Write("minTolerance", mainBlock._minTolerance);
            _dataHost.Write("maxTolerance", mainBlock._maxTolerance);
        }
#endif
        private void _editDbText(DBText text)
        {
            text.SafeEdit(() =>
                {
                    string textValue = _editString(text.TextString);
                    text.TextString = textValue;
                });
        }

        public void _editMText(MText mtext)
        {
            mtext.SafeEdit(() =>
                {
                    string textValue = _editString(mtext.Contents);
                    mtext.Contents = textValue;
                });
        }

        private void _editDimension(Dimension dim)
        {
        }



        private string _editString(string TextString)
        {
            System.Text.RegularExpressions.Match match = null;
            string valueText = string.Empty;
            string sep = string.Empty;
            string res = TextString;
            //  "\\d+\\b(<dig>\\.\\d+)?")
            var matches = System.Text.RegularExpressions.Regex.Matches(TextString.Replace(',', '.'), "(?<!\\k<dig>)\\d+(?<dig>\\.\\d+)?");
            if (matches.Count > 0 && matches[matches.Count - 1].Success)
            {
                match = matches[matches.Count - 1];
                //List<System.Text.RegularExpressions.Group> groups = new List<System.Text.RegularExpressions.Group>(match.Groups);
                valueText = match.Value;
            }

            if (!string.IsNullOrWhiteSpace(valueText))
            {
                double value;
                if (double.TryParse(valueText, System.Globalization.NumberStyles.Number, _culture, out value))
                {
                    value += _getAllowedToleranceValue(_maxTolerance, _minTolerance);
                    int ind = match.Index; //text.TextString.Replace(',','.').LastIndexOf(valueText);
                    if (ind > -1)
                    {
                        string format = "#0";
                        if (match.Groups["dig"].Success)
                        {
                            format += ".";
                            for (int i = 0; i < match.Groups["dig"].Length - 1; i++)
                                format += '0';
                            sep = match.Groups["dig"].Value[0].ToString();
                        }

                        res = TextString.Remove(ind, valueText.Length);
                        res = res.Insert(ind, /*"[" +*/ value.ToString(format, _culture) /*+ "]"*/);
                        
                        if (!string.IsNullOrWhiteSpace(sep))
                        {
                            res = res.Replace(".", sep);
                        }
                    }
                }
            }

            return res;
        }

        private double _getAllowedToleranceValue(double maxValue, double minValue)
        {
            bool flag = IgorKL.ACAD3.Model.Helpers.Math.Randoms.RandomGen.Next(1) > 0 ? true : false;

            double randVal = IgorKL.ACAD3.Model.Helpers.Math.Randoms.RandomGen.NextDouble();

            return flag ? randVal * maxValue : randVal * minValue;
        }


    }
}
