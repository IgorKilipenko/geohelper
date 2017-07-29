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

using Autodesk.AutoCAD.Windows;

using IgorKL.ACAD3.Model.Extensions;


namespace IgorKL.ACAD3.Model.Drawing.Views
{
    /// <summary>
    /// Логика взаимодействия для AnchorArrowView.xaml
    /// </summary>
    public partial class AnchorArrowView : UserControl
    {
        private Helpers.HostProvider _hostProvider;

        public AnchorArrowView()
        {
            InitializeComponent();
            _hostProvider = new Helpers.HostProvider();
            
            //Deserialize();
        }

        public Action CommandAction { get; set; }
        public double SelectedTolerance { get; private set; }

#if DEBUG
        [RibbonCommandButton("Окно меню", "Тест/Окна")]
        [Autodesk.AutoCAD.Runtime.CommandMethod(
                 "ShowMyPalette",
                 Autodesk.AutoCAD.Runtime.CommandFlags.Modal | Autodesk.AutoCAD.Runtime.CommandFlags.Session
                )]

        public static void CreatePaletteSet()
        {
            PaletteSet pset = new PaletteSet(typeof(AnchorArrowView).Name, new Guid());
            pset.Size = new System.Drawing.Size(300, 400);

            AnchorArrowView view = new AnchorArrowView();
            pset.AddVisual("Анкера", view, true);
            pset.Style =
         PaletteSetStyles.NameEditable |
         PaletteSetStyles.ShowPropertiesMenu |
         PaletteSetStyles.ShowAutoHideButton |
         PaletteSetStyles.ShowCloseButton;

            pset.DockEnabled = DockSides.None;

            pset.EnableTransparency(true);
            pset.KeepFocus = true;
            pset.Visible = true;
            //return pset;
        }
#endif

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            //Serialize();
        }

        private void textBox_Tolerance_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public MainMenu.HostProvider DataHost { get; set; }

        public sealed class MethodToValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var methodName = parameter as string;
                if (value == null || methodName == null)
                    return value;
                var methodInfo = value.GetType().GetMethod(methodName, new Type[0]);
                if (methodInfo == null)
                    return value;
                return methodInfo.Invoke(value, new object[0]);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException("MethodToValueConverter can only be used for one way conversion.");
            }
        }
    }

}
