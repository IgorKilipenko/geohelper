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

namespace IgorKL.ACAD3.Model.CustomObjects
{
    public class SimpleEntityOverride : Autodesk.AutoCAD.EditorInput.DrawJig, IDisposable
    {
        private List<Entity> _entities;
        private Database _db;
        private Point3d _origin;
        protected Object _thisLock = new Object();
        protected Matrix3d _ucs;
        protected IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient _transient;


        public List<Entity> Entities { get { return _entities; } }
        public Point3d Origin { get { return _origin; } }

        public SimpleEntityOverride(Point3d origin, List<Entity> entities, Matrix3d ucs)
            : base()
        {
            _ucs = ucs;

            _origin = origin;

            _db = Tools.GetAcadDatabase();
            _entities = entities;
        }

        public virtual void Display()
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();

            foreach (var ent in Entities)
            {
                _transient.AddMarker((Entity)ent.Clone());
            }

            _transient.Display();
        }


        public virtual void StopDisplay()
        {
            if (_transient == null)
            {
                _transient.ClearTransientGraphics();
            }
        }

        public void InnerTransformBy(Matrix3d matrix)
        {
            Tools.StartTransaction(() =>
            {
                Matrix3d mat = Matrix3d.Identity.PreMultiplyBy(matrix);
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity ent = _entities[i];
                    if (ent.ObjectId != ObjectId.Null)
                        ent = ent.Id.GetObjectForWrite<Entity>(false);
                    ent.TransformBy(mat);
                }
            });
        }

        public virtual SimpleEntityOverride GetClone()
        {
            List<Entity> buffer = new List<Entity>(_entities.Count);
            SimpleEntityOverride clone = null;
            Tools.StartOpenCloseTransaction(() =>
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity ent = _entities[i];
                    if (ent.Id != ObjectId.Null)
                        ent = ent.Id.GetObjectForRead<Entity>();
                    ent = (Entity)ent.Clone();
                    buffer.Add(ent);
                }
                clone = new SimpleEntityOverride(_origin, buffer, _ucs);
            });
            return clone;
        }

        public virtual void Dispose()
        {
            if (this._transient != null)
                _transient.Dispose();
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            lock (Entities)
            {
                List<Entity> inMemoryEntities = new List<Entity>(Entities.Count);
                foreach (Entity ent in Entities.ToArray())
                {
                    Entity inMemoryEntity = (Entity)ent.Clone();
                    inMemoryEntities.Add(inMemoryEntity);
                    draw.Geometry.Draw(inMemoryEntity);
                }

                foreach (Entity ent in inMemoryEntities)
                {
                    ent.Dispose();
                }
                inMemoryEntities.Clear();
            }

            return true;
        }

        protected override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts)
        {
            return SamplerStatus.Cancel;
        }

        public virtual PromptStatus JigDraw()
        {
            PromptResult promptResult = Tools.GetAcadEditor().Drag(this);
            return promptResult.Status;
        }

    }
}
