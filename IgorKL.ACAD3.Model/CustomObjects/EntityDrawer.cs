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
    public abstract class EntityDrawer : Autodesk.AutoCAD.EditorInput.DrawJig, IDisposable
    {
        private List<Entity> _entities;
        private Database _db;
        private AnnotativeStates _annotative;

        protected Object _thisLock = new Object();
        protected Matrix3d _ucs;
        protected IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient _transient;


        public List<Entity> Entities { get { return _entities; } }
        public Matrix3d Ucs { get { return _ucs; } }
        public AnnotativeStates Annotative { get { return _annotative; } }


        public EntityDrawer(List<Entity> entities, AnnotativeStates annotative, Matrix3d transformFromUcsToWcs)
            : base()
        {
            _ucs = transformFromUcsToWcs;
            _annotative = annotative;

            _db = Tools.GetAcadDatabase();
            _entities = entities;
        }

        public void TrasientDisplay()
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();

            foreach (var ent in Entities)
            {
                _transient.AddMarker((Entity)ent.Clone());
            }

            _transient.Display();
        }

        public void TrasientDisplay(IEnumerable<Entity> entities)
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();

            foreach (var ent in entities)
            {
                _transient.AddMarker((Entity)ent.Clone());
            }

            _transient.Display();
        }

        public virtual void TrasientDisplayAtBlock(Point3d insertPoint)
        {
            TrasientDisplayAtBlock(insertPoint, _entities);
        }

        public virtual void TrasientDisplayAtBlock(Point3d insertPoint, IEnumerable<Entity> entities)
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();

            Calculate();
            ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", insertPoint, entities.Select(ent => (Entity)ent.Clone()), _annotative);
            ObjectId blockId = AcadBlocks.BlockTools.AppendBlockItem(insertPoint, btrId, null, _ucs);

            BlockReference block = null;

            Tools.StartTransaction(() =>
            {
                block = blockId.GetObjectForWrite<BlockReference>();
                var buffer = (BlockReference)block.Clone();
                block.Erase(true);
                block = buffer;
            });

            _transient.AddMarker((DBObject)block);

            _transient.Display();
        }

        public abstract void Calculate();

        public virtual void StopTrasientDisplay()
        {
            if (_transient != null)
            {
                _transient.ClearTransientGraphics();
            }
        }

        public void InnerUpgradeOpen()
        {
            Entities.ForEach(ent => ent.UpgradeOpen());
        }

        public void InnerSetDatabaseDefaults(Database db)
        {
            Entities.ForEach(ent => ent.SetDatabaseDefaults(db));
        }


        public virtual void Dispose()
        {
            if (this._transient != null)
                _transient.Dispose();
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            Calculate();
            List<Entity> inMemoryEntities = new List<Entity>(Entities.Count);
            lock (Entities)
            {
                foreach (Entity ent in Entities.ToArray())
                {
                    //Entity inMemoryEntity = (Entity)ent.GetTransformedCopy(ToWcsTransform);
                    Entity inMemoryEntity = (Entity)ent.Clone();
                    inMemoryEntities.Add(inMemoryEntity);
                    draw.Geometry.Draw(inMemoryEntity);
                }
            }

            List<Entity> buffer = new List<Entity>(inMemoryEntities);
            System.Threading.Thread thread = new System.Threading.Thread(obj =>
            {
                foreach (Entity ent in (List<Entity>)obj)
                {
                    ent.Dispose();
                }
                ((List<Entity>)obj).Clear();
            });
            thread.Start(buffer);
            return true;
        }

        protected abstract override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts);

        public virtual PromptStatus JigDraw()
        {
            PromptResult promptResult = Tools.GetAcadEditor().Drag(this);
            return promptResult.Status;
        }

        public List<Entity> Explode()
        {
            List<Entity> res = new List<Entity>(Entities.Count);
            foreach (Entity ent in Entities)
            {
                res.Add(ent.GetTransformedCopy(_ucs));
            }
            return res;
        }

        protected Matrix3d ToUcsTransform { get { return _ucs.Inverse(); } }
        protected Matrix3d ToWcsTransform { get { return _ucs; } }

    }
}
