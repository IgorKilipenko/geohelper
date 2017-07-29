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

namespace IgorKL.ACAD3.Model.Drawing.Views
{
    /// <summary>
    /// Логика взаимодействия для EntitiesRandomEditorView.xaml
    /// </summary>
    public partial class EntitiesRandomEditorView : UserControl
    {
        MainMenu.HostProvider _dataHost;

        public event SelectionChangedEventHandler FormatChanged;
        public string SelectedFormat
        {
            get { return _comboBox_Format.SelectedValue.ToString(); }
        }
        public bool UseDefineFormat
        {
            get { return _checkBox_UseDefineFormat.IsChecked.Value; }
        }
        public bool UseMinTolerance
        {
            get
            {
                return _checkBox_UseMinTolerance.IsChecked.Value;
            }
        }

        public EntitiesRandomEditorView()
        {
            InitializeComponent();
        }

        public Action CommandAction { get; set; }


        public MainMenu.HostProvider DataHost { 
            get{
                return _dataHost;
            }

            set
            {
                if (_dataHost != null)
                    _dataHost.ValueSaved -= _dataHost_ValueSaved;
                _dataHost = value;
                if (_dataHost != null)
                    _dataHost.ValueSaved += _dataHost_ValueSaved;
                _textBox_TopTolerance.Text = _dataHost.Read("maxTolerance", 0.05d).ToString("#0.00");
                _textBox_BottomTolerance.Text = _dataHost.Read("minTolerance", -0.05d).ToString("#0.00");
            }
        }

        void _dataHost_ValueSaved(object sender, MainMenu.HostProvider.KeyValueEventArgs e)
        {
            if (e.Name == "maxTolerance" && e.ValueType == typeof(double))
            {
                _textBox_TopTolerance.Text = ((double)e.Value).ToString("#0.00");
            }
            else if (e.Name == "minTolerance" && e.ValueType == typeof(double))
            {
                _textBox_BottomTolerance.Text = ((double)e.Value).ToString("#0.00");
            }
        }

        private void ok_Button_Click(object sender, RoutedEventArgs e)
        {
            if (CommandAction != null)
            {
                CommandExecutor.Execute(CommandAction, false);
            }
        }

        private void _comboBox_Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            On_FormatChanged(e);
        }

        protected void On_FormatChanged(SelectionChangedEventArgs e)
        {
            if (FormatChanged != null)
                FormatChanged(this, e);
        }

        private void _checkBox_UseMinTolerance_Unchecked(object sender, RoutedEventArgs e)
        {
            _textBox_BottomTolerance.IsEnabled = false;
        }

        private void _checkBox_UseMinTolerance_Checked(object sender, RoutedEventArgs e)
        {
            _textBox_BottomTolerance.IsEnabled = true;
        }

        private void _checkBox_UseDefineFormat_Checked(object sender, RoutedEventArgs e)
        {
            _comboBox_Format.IsEnabled = true;
        }

        private void _checkBox_UseDefineFormat_Unchecked(object sender, RoutedEventArgs e)
        {
            _comboBox_Format.IsEnabled = false;
        }

    }
}
