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

namespace IgorKL.ACAD3.Model.CoordinateGeometry
{
    public interface IAllowableTolerance
    {
        PromptResult PromptTolerance(string msg);
        PromptResult PromptMinValue(string msg);
        PromptResult PromptMaxValue(string msg);
        double MaxAllowableValue {get;}
        double MinAllowableValue { get; }
    }
}
