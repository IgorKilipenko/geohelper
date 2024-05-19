using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcGi = Autodesk.AutoCAD.GraphicsInterface;

namespace IgorKL.ACAD3.Customization.Snap
{
    /// <summary>
    /// Register and unregister custom osnap
    /// </summary>
    public class CustomOSnapApp : IExtensionApplication
    {
        private QuarterOsnapInfo _info =
      new QuarterOsnapInfo();
        private QuarterGlyph _glyph =
          new QuarterGlyph();
        private CustomObjectSnapMode _mode;

        public void Initialize()
        {
            // Register custom osnap on initialize

            _mode =
              new CustomObjectSnapMode(
                "Quarter",
                "Quarter",
                "Quarter of length",
                _glyph
              );

            // Which kind of entity will use the osnap

            _mode.ApplyToEntityType(
              RXObject.GetClass(typeof(Polyline)),
              new AddObjectSnapInfo(_info.SnapInfoPolyline)
            );
            _mode.ApplyToEntityType(
              RXObject.GetClass(typeof(Curve)),
              new AddObjectSnapInfo(_info.SnapInfoCurve)
            );
            _mode.ApplyToEntityType(
              RXObject.GetClass(typeof(Entity)),
              new AddObjectSnapInfo(_info.SnapInfoEntity)
            );

            // Activate the osnap

            CustomObjectSnapMode.Activate("_Quarter");
        }

        // Unregister custom osnap on terminate

        public void Terminate()
        {
            CustomObjectSnapMode.Deactivate("_Quarter");
        }
    }

    /// <summary>
    /// Create new quarter object snap
    /// </summary>
    public class QuarterGlyph : AcGi.Glyph
    {
        private Point3d _pt;

        public override void SetLocation(Point3d point)
        {
            _pt = point;
        }

        protected override void SubViewportDraw(AcGi.ViewportDraw vd)
        {
            int glyphPixels = CustomObjectSnapMode.GlyphSize;
            var glyphSize = vd.Viewport.GetNumPixelsInUnitSquare(_pt);

            double glyphHeight =
              (glyphPixels / glyphSize.Y) * 1.1;

            string text = "¼";

            var dist = -glyphHeight / 2.0;
            var offset = new Vector3d(dist, dist, 0);
            var e2w = vd.Viewport.EyeToWorldTransform;
            var dir = Vector3d.XAxis.TransformBy(e2w);
            var pt = (_pt + offset).TransformBy(e2w);

            var style = new AcGi.TextStyle();
            var fd =
              new AcGi.FontDescriptor("txt.shx", false, false, 0, 0);
            style.Font = fd;
            style.TextSize = glyphHeight;

            vd.Geometry.Text(
              pt,
              vd.Viewport.ViewDirection,
              dir,
              text,
              false,
              style
            );
        }
    }

    /// <summary>
    /// OSnap info
    /// </summary>
    public class QuarterOsnapInfo
    {
        public void SnapInfoEntity(
          ObjectSnapContext context,
          ObjectSnapInfo result)
        {
        }

        public void SnapInfoCurve(
          ObjectSnapContext context,
          ObjectSnapInfo result
        )
        {
            var cv = context.PickedObject as Curve;
            if (cv == null)
                return;

            double startParam = cv.StartParam;
            double endParam = cv.EndParam;

            double param =
              startParam + ((endParam - startParam) * 0.25);
            var pt = cv.GetPointAtParameter(param);

            result.SnapPoints.Add(pt);

            param =
              startParam + ((endParam - startParam) * 0.75);
            pt = cv.GetPointAtParameter(param);

            result.SnapPoints.Add(pt);
            if (cv.Closed)
            {
                pt = cv.StartPoint;
                result.SnapPoints.Add(pt);
            }
        }

        public void SnapInfoPolyline(
          ObjectSnapContext context,
          ObjectSnapInfo result)
        {
            var pl = context.PickedObject as Polyline;
            if (pl == null)
                return;

            double plStartParam = pl.StartParam;
            double plEndParam = pl.EndParam;

            double startParam = plStartParam;
            double endParam = startParam + 1.0;

            while (endParam <= plEndParam)
            {
                double param =
                  startParam + ((endParam - startParam) * 0.25);
                var pt = pl.GetPointAtParameter(param);

                result.SnapPoints.Add(pt);

                param =
                  startParam + ((endParam - startParam) * 0.75);
                pt = pl.GetPointAtParameter(param);

                result.SnapPoints.Add(pt);

                startParam = endParam;
                endParam += 1.0;
            }
        }
    }
}
