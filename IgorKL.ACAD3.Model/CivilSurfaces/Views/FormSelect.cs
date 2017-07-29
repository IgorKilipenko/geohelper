using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IgorKL.ACAD3.Model.CivilSurfaces.Views
{
    public partial class FormSelect : Form
    {
        public FormSelect()
        {
            InitializeComponent();
        }

        public void AddSurfaceName(string name)
        {
            this.comboBox1.Items.Add(name);
        }

        public string SelectedSurfaceName
        {
            get { return this.comboBox1.SelectedItem as string; }
        }

        private void button_Select_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        public ComboBox SurfaceComboBox
        {
            get
            {
                return this.comboBox1;
            }
        }
    }
}
