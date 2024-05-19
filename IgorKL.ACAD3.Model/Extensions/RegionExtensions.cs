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
using Autodesk.AutoCAD.BoundaryRepresentation;

namespace IgorKL.ACAD3.Model.Extensions {
    public static class RegionExtensions {

        ///<summary>
        /// Returns whether a Region contains a Point3d.
        ///</summary>
        ///<param name="point">A points to test against the Region.</param>
        ///<returns>A Boolean indicating whether the Region contains
        /// the point.</returns>
        public static bool ContainsPoint(this Region reg, Point3d point) {

            using (var brep = new Brep(reg)) {

                var pc = new PointContainment();

                using (var brepEnt = brep.GetPointContainment(point, out pc)) {
                    return pc != PointContainment.Outside;
                }
            }
        }
    }
}
