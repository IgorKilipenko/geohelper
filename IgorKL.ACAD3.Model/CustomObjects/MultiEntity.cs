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
    public abstract class MultiEntity : Autodesk.AutoCAD.EditorInput.DrawJig, IDisposable
    {
        private List<Entity> _entities;
        private Database _db;
        private Point3d _originWcs;
        private Point3d _insertPointUcs;
        private Matrix3d _innerBlockTransform;
        private AnnotativeStates _annotative;
        //private ObjectId _blockId;

        protected Object _thisLock = new Object();
        protected Matrix3d _ucs;
        protected IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient _transient;


        public List<Entity> Entities { get { return _entities; } }
        public Point3d Origin { get { return _originWcs; } }
        public Point3d InsertPointUcs { get { return _insertPointUcs; } }
        public Matrix3d InnerBlockTransform { get { return _innerBlockTransform; } }
        public Matrix3d Ucs { get { return _ucs; } }

        public MultiEntity(Point3d insertPoint, Point3d originWcs, List<Entity> entities, AnnotativeStates annotative ,Matrix3d transformFromUcsToWcs)
            : base()
        {
            _innerBlockTransform = Matrix3d.Identity;
            _ucs = transformFromUcsToWcs;
            _insertPointUcs = insertPoint;
            _annotative = annotative;
            _originWcs = originWcs;

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

        public virtual void TrasientDisplayAtBlock()
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();
            
            Calculate();
            ObjectId btrId = _createTableRecord(_entities.Select(ent => (Entity)ent.Clone()));
            ObjectId blockId = _createBlockItem(btrId, _insertPointUcs);

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

        public virtual void TrasientDisplayAtBlock(IEnumerable<Entity> entities)
        {
            if (_transient == null)
                _transient = new IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient();

            Calculate();
            ObjectId btrId = _createTableRecord(entities.Select(ent => (Entity)ent.Clone()));
            ObjectId blockId = _createBlockItem(btrId, _insertPointUcs);

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
            if (_transient == null)
            {
                _transient.ClearTransientGraphics();
            }
        }

        public void InnerTransformBy(Matrix3d matrix)
        {
            Tools.StartTransaction(() =>
            {
                Matrix3d mat = _innerBlockTransform.PreMultiplyBy(matrix);
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity ent = _entities[i];
                    if (ent.ObjectId != ObjectId.Null)
                        ent = ent.Id.GetObjectForWrite<Entity>(false);
                    ent.TransformBy(mat);
                }
            });
        }


        [Obsolete]
        public void AddInnerEntitiesToDatabase(Database db)
        {
            Entities.ForEach(ent => ent.TransformBy(ToWcsTransform));
            Tools.AppendEntity(Entities);
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

        private ObjectId _createTableRecord(IEnumerable<Entity> entities)
        {
            return AcadBlocks.BlockTools.CreateBlockTableRecord("*U", _originWcs ,entities, _annotative);
        }

        protected virtual ObjectId _createBlockItem(ObjectId tableRecordId, Point3d insertPoint)
        {
            return AcadBlocks.BlockTools.AddBlockRefToModelSpace(tableRecordId, null, insertPoint, _ucs);
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

        private void _fromUcsToWcs()
        {
            InnerTransformBy(_ucs);
        }
        private void _fromWcsToUcs()
        {
            InnerTransformBy(_ucs.Inverse());
        }
    }
}
