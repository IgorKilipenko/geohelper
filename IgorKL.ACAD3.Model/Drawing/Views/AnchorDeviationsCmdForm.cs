using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using IgorKL.ACAD3.Model.AcadBlocks;
using acadApp = Autodesk.AutoCAD.ApplicationServices;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing.Views
{
    public partial class AnchorDeviationsCmdForm : Form
    {
        public AnchorDeviationsCmdForm()
        {
            InitializeComponent();
        }

    }
}
