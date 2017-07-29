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
using Autodesk.Civil.DatabaseServices;

using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class CivilSurfaceExtensions
    {
        public static ObjectIdCollection GetBounds(this /*CivilSurface*/ITerrainSurface surface)
        {
            ITerrainSurface terrSurface = surface as ITerrainSurface;
            if (terrSurface == null)
                return null;
            var ids = terrSurface.ExtractBorder(Autodesk.Civil.SurfaceExtractionSettingsType.Model);
            return ids;
        }

        public static IEnumerable<Polyline3d> GetBoundariesDefinitions(this CivilSurface surface)
        {
            List<Polyline3d> res = new List<Polyline3d>();
            if (surface.BoundariesDefinition.Count == 0)
                return res;
            
            for (int i = 0; i< surface.BoundariesDefinition.Count; i++)
            {
                SurfaceBoundary sboundary = surface.BoundariesDefinition[i][0];
                Point3dCollection points = sboundary.Vertices;
                Polyline3d pline = new Polyline3d(Poly3dType.SimplePoly, points, true);
                res.Add(pline);
            }
            return res;
        }

        public static List<Polyline3d> ExtractBorders(this ITerrainSurface surface)
        {
            List<Polyline3d> result = new List<Polyline3d>();
            ObjectIdCollection entityIds = surface.ExtractBorder(Autodesk.Civil.SurfaceExtractionSettingsType.Plan);
            for (int i = 0; i < entityIds.Count; i++)
            {

                ObjectId entityId = entityIds[i];

                if (entityId.ObjectClass == RXClass.GetClass(typeof(Polyline3d)))
                {
                    Polyline3d border = entityId.GetObject(OpenMode.ForWrite) as Polyline3d;
                    result.Add(border);
                }

            }
            return result;
        }

        /*
        public static Polyline GetIntersectBorder(this ITerrainSurface surface1, ITerrainSurface surface2)
        {
            var borders1 = surface1.ExtractBorders();

            var borders2 = surface2.ExtractBorders();
            if (borders1 == null || borders2 == null)
                return null;

            Polyline border1 = borders1.LastOrDefault().ConvertToPolyline();
            Polyline border2 = borders2.LastOrDefault().ConvertToPolyline();

            HashSet<Point3d> points = new HashSet<Point3d>();
            for (int i = 0; i < border1.NumberOfVertices; i++)
            {
                Point3d p = border1.GetPoint3dAt(i);
                if (border2.IsInsidePolygon(p))
                    points.Add(p);
            }
            for (int i = 0; i < border2.NumberOfVertices; i++)
            {
                Point3d p = border2.GetPoint3dAt(i);
                if (border1.IsInsidePolygon(p))
                    if (!points.Contains(p))
                        points.Add(p);
            }

            Polyline pline = new Polyline(points.Count);
            pline.AddVertexes(points);
            return pline;
        }*/

        [Obsolete("Надо найти другой способ извлечения границы")]
        public static Polyline3d ExtractBorders(this Autodesk.Civil.DatabaseServices.TinVolumeSurface surface)
        {
            var defBoundaries = surface.GetBoundariesDefinitions();
            if (defBoundaries.Count() > 0)
                return defBoundaries.Last();
            else
            {
                bool displayBoundariesChanged = false;
                Autodesk.Civil.DatabaseServices.Styles.SurfaceStyle style = surface.StyleId.GetObjectForRead<Autodesk.Civil.DatabaseServices.Styles.SurfaceStyle>();
                var displayStyle = style.GetDisplayStylePlan(Autodesk.Civil.DatabaseServices.Styles.SurfaceDisplayStyleType.Boundary);
                if (!displayStyle.Visible)
                {
                    style.UpgradeOpen();
                    displayStyle.Visible = true;
                    style.DowngradeOpen();
                    displayBoundariesChanged = true;
                }

                Polyline3d res = null;
                DBObjectCollection dbcoll = new DBObjectCollection();
                surface.Explode(dbcoll);
                foreach (var dbobj in dbcoll)
                {
                    if (dbobj is BlockReference)
                    {
                        dbcoll.Clear();
                        ((BlockReference)dbobj).Explode(dbcoll);
                        List<Polyline3d> lines = new List<Polyline3d>();
                        foreach (var ent in dbcoll)
                        {
                            if (ent is Polyline3d)
                            {
                                Polyline3d pline = ent as Polyline3d;
                                if (pline.Closed)
                                    lines.Add(pline);
                            }
                        }



                        //lines.Sort((l1, l2) => Comparer<double>.Default.Compare(l1.Area, l2.Area));
                        res = lines.LastOrDefault();
                        break;
                    }
                }

                if (displayBoundariesChanged)
                {
                    style.UpgradeOpen();
                    displayStyle.Visible = false;
                    style.DowngradeOpen();
                }

                return res;
            }
        }
    }


}
