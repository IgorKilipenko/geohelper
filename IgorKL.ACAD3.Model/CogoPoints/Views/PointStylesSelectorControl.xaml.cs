using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.CogoPoints.Views
{
    /// <summary>
    /// Логика взаимодействия для PointStylesSelectorControl.xaml
    /// </summary>
    public partial class PointStylesSelectorControl : UserControl
    {
        ObjectIdNamedCollection _pointsStyleCollection;
        ObjectIdNamedCollection _pointsLabelStylesCollection;

        public ObjectIdNamedCollection CogoPointStyles { get { return _pointsStyleCollection; } }
        public ObjectIdNamedCollection CogoPointLabelStyles { get { return _pointsLabelStylesCollection; } }
        public Action CommandAction { get; set; }

        public KeyValuePair<ObjectId, string> SelectedPointStyle { get { return (KeyValuePair<ObjectId, string>)_comboBox_PointStyles.SelectedItem; } }
        public KeyValuePair<ObjectId, string> SelectedPointLabelStyle { get { return (KeyValuePair<ObjectId, string>)_comboBox_PointLabelStyles.SelectedItem; } }
        public PointStylesSelectorControl()
        {
            _pointsStyleCollection = new ObjectIdNamedCollection();
            _pointsLabelStylesCollection = new ObjectIdNamedCollection();
            InitAcadValues();
            InitializeComponent();
            
        }

        public void InitAcadValues()
        {
            //using (var docLock = Tools.GetActiveAcadDocument().LockDocument())
            {
                _pointsStyleCollection.Clear();
                _pointsLabelStylesCollection.Clear();

                /*_pointsStyleCollection.Add(CogoPointEditor.GeDefailtPointStyleId(), CogoPointEditor.GeDefailtPointStyleName());
                _pointsLabelStylesCollection.Add(CogoPointEditor.GeDefailtPointLableStyleId(), CogoPointEditor.GeDefailtPointLableStyleName());
                */
                _pointsStyleCollection.Add(ObjectId.Null, "<По умолчанию>");
                _pointsLabelStylesCollection.Add(ObjectId.Null, "<По умолчанию>");

                var styles = CogoPointEditor.GetAllPointsStyles();
                var lableStyles = CogoPointEditor.GetAllPointsLabelStyles();

                using (Transaction trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId styleId in styles)
                    {
                        PointStyle ps = null;
                        try
                        {
                            ps = styleId.GetObject(OpenMode.ForRead, true, true) as PointStyle;
                            if (ps == null || ps.Id.IsNull)
                                continue;
                        }
                        catch
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Только для Autocad Civil 3D");
                            return;
                        }

                        if (!_pointsStyleCollection.ContainsKey(styleId))
                            _pointsStyleCollection.Add(styleId, ps.Name);
                        else
                            _pointsStyleCollection[styleId] = ps.Name;

                    }

                    foreach (ObjectId labelStyleId in lableStyles)
                    {
                        LabelStyle ls = null;
                        try
                        {
                            ls = labelStyleId.GetObject(OpenMode.ForRead, true, true) as LabelStyle;
                            if (ls == null || ls.Id.IsNull)
                                continue;
                        }
                        catch
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Только для Autocad Civil 3D");
                            return;
                        }

                        if (!_pointsLabelStylesCollection.ContainsKey(labelStyleId))
                            _pointsLabelStylesCollection.Add(labelStyleId, ls.Name);
                        else
                            _pointsLabelStylesCollection[labelStyleId] = ls.Name;
                    }

                }

                
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            if (CommandAction != null)
            {
                CommandExecutor.Execute(CommandAction, false);
                //CommandAction();
            }
        }

    }
}
