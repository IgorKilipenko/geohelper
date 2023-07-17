using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using IgorKL.ACAD3.Model;
using helpers = IgorKL.ACAD3.Model.Drawing.Helpers;

/*[assembly: ExtensionApplication(
  typeof(IgorKL.ACAD3.Customization.GripPoints.ArrowGripOverrule))
]*/

namespace IgorKL.ACAD3.Customization.GripPoints
{
    public class ArrowGripOverrule:GripOverrule, IExtensionApplication
    {
        public void Initialize()
        {
            Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), this, true);
        }

        public void Terminate()
        {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(BlockReference)), this);
        }

        public ArrowGripOverrule()
        {
            SetExtensionDictionaryEntryFilter("ARROW_JigPosition");
        }

        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            ArrowGripData gdata = new ArrowGripData();
            gdata.GripPoint = _getCustomGrip(entity);
            grips.Add(gdata);
            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
        }

        private Point3d _getCustomGrip(Entity entity)
        {
            Xrecord xrecord = null;
            Point3d p = Point3d.Origin;
            Tools.StartTransaction(() =>
            {
                DBDictionary dic = (DBDictionary)entity.ExtensionDictionary.GetObject(OpenMode.ForRead, false, true);
                xrecord = (Xrecord)dic.GetAt("ARROW_JigPosition").GetObject(OpenMode.ForRead);

                p = new Point3d(xrecord.Select(tv => (double)tv.Value).ToArray());
                ArrowGripData gdata = new ArrowGripData();
            });

            return p;
        }

        public class ArrowGripData:GripData
        {
            public ArrowGripData()
                :base()
            { }
        }
    }


    public class AnchorArrowGripOverrule : GripOverrule, IExtensionApplication
    {
        private IEnumerable<Entity> _entities;

        public void Initialize()
        {
            Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), this, true);
        }

        public void Terminate()
        {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(BlockReference)), this);
        }

        public AnchorArrowGripOverrule()
        {
            SetExtensionDictionaryEntryFilter("ICmdFlag_WALLARROW_FLAG");
        }

        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            ArrowGripData gdata = new ArrowGripData();
            gdata.GripPoint = _getCustomGrip(entity);
            grips.Add(gdata);
            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
#if DEBUG
            helpers.Arrow.SafeObject so = 
                (helpers.Arrow.SafeObject)helpers.Arrow.SafeObject.NewFromEntity(entity, helpers.Arrow.SafeObject.AppName);

            _entities = so.Object.Explode().Select(x => x.GetTransformedCopy(Tools.GetAcadEditor().CurrentUserCoordinateSystem));
#endif
        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
#if DEBUG
            if (_entities != null)
            {
                IgorKL.ACAD3.Model.Tools.AppendEntity(_entities);
                _entities = null;
            }
#endif
        }

        private Point3d _getCustomGrip(Entity entity)
        {
            Xrecord xrecord = null;
            Point3d p = Point3d.Origin;
            Tools.StartTransaction(() =>
            {
                DBDictionary dic = (DBDictionary)entity.ExtensionDictionary.GetObject(OpenMode.ForRead, false, true);
                xrecord = (Xrecord)dic.GetAt("ICmdFlag_WALLARROW_FLAG").GetObject(OpenMode.ForRead);

                p = new Point3d(xrecord.Select(tv => (double)tv.Value).ToArray());
                ArrowGripData gdata = new ArrowGripData();
            });

            return p;
        }

        public class ArrowGripData : GripData
        {
            public ArrowGripData()
                : base()
            { }
        }
    }
}
