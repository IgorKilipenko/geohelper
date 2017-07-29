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
    public class SimpleEntityOverrideEx: Autodesk.AutoCAD.EditorInput.DrawJig,IDisposable
    {
        private List<Entity> _entities;
        private ObjectId _blockRecordId;
        private Database _db;
        private Point3d _origin;
        private AnnotativeStates _annotativeState;
        private string _name = "*U";
        private ObjectId _blockId;
        private Point3d _insertPoint;
        protected Object _thisLock = new Object();
        protected Matrix3d _ucs;
        protected IgorKL.ACAD3.Model.Helpers.Display.DynamicTransient _transient;


        public List<Entity> Entities { get { return _entities; } }
        public Point3d Origin { get { return _origin; }}
        public AnnotativeStates Annotative { get { return _annotativeState; } }

        public SimpleEntityOverrideEx(Point3d origin, AnnotativeStates annotativeState, List<Entity> entities, Matrix3d ucs)
            :base()
        {
            _ucs = ucs;

            _origin = origin.TransformBy(_ucs);
            _annotativeState = annotativeState;

            _db = Tools.GetAcadDatabase();
            _entities = entities;

            _blockId = ObjectId.Null;
            _insertPoint = Point3d.Origin;
            
            _createBlockRecord(_name, _annotativeState, _origin);
        }

        public void AppendEntity(Entity ent)
        {
            Tools.StartTransaction(() =>
            {
                _entities.Add(ent);
                BlockTableRecord btr = _blockRecordId.GetObjectForWrite<BlockTableRecord>();
                if (ent.Id != ObjectId.Null)
                    ent = ent.Id.GetObjectForWrite<Entity>();
                btr.AppendEntity(ent);
                if (ent.Id == ObjectId.Null)
                    btr.Database.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(ent, true);
            });
            //_createBlockRecord(_name, _annotativeState, _origin);
            Update();
        }

        /*public void Explode(ObjectIdCollection idSet)
        {
            foreach (Entity ent in _entities)
                idSet.Add(ent.Id);
        }*/

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


        public void Update()
        {
            Tools.StartTransaction(() =>
            {
                if (_blockRecordId != ObjectId.Null)
                {
                    BlockTableRecord btr = _blockRecordId.GetObjectForRead<BlockTableRecord>(false);
                    //btr.UpgradeOpen();
                    foreach (ObjectId id in btr.GetBlockReferenceIds(true, true))
                    {
                        BlockReference br = id.GetObjectForWrite<BlockReference>(false);
                        br.RecordGraphicsModified(true);
                        btr.Database.TransactionManager.QueueForGraphicsFlush();
                    }
                }
            });
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
            Update();
        }

        public virtual SimpleEntityOverrideEx GetClone()
        {
            List<Entity> buffer = new List<Entity>(_entities.Count);
            SimpleEntityOverrideEx clone = null;
            Tools.StartOpenCloseTransaction(() =>
            {
                for (int i = 0; i < _entities.Count; i++ )
                {
                    Entity ent = _entities[i];
                    if (ent.Id != ObjectId.Null)
                        ent = ent.Id.GetObjectForRead<Entity>();
                    ent = (Entity)ent.Clone();
                    buffer.Add(ent);
                }
                clone = new SimpleEntityOverrideEx(_origin, _annotativeState, buffer, _ucs);
            });
            return clone;
        }


        private void _createBlockRecord(string name, AnnotativeStates annotativeState, Point3d origin, bool eraseSource = true)
        {
            BlockTableRecord btr = new BlockTableRecord();
            btr.Name = name;
            btr.Origin = origin;
            btr.Annotative = annotativeState;

            if (eraseSource)
            {
                Tools.StartTransaction(() =>
                {
                    if (_blockRecordId != ObjectId.Null)
                    {
                        BlockTableRecord btrOld = _blockRecordId.GetObjectForWrite<BlockTableRecord>();
                        btrOld.Erase();
                    }
                });
            }

            Tools.StartTransaction(() =>
            {
                var bt = _db.BlockTableId.GetObjectForWrite<BlockTable>(false);

                bt.UpgradeOpen();
                ObjectId btrId = bt.Add(btr);

                _db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(btr, true);
                this._blockRecordId = btrId;

                foreach (Entity ent in _entities)
                {
                    btr.AppendEntity(ent);
                    _db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(ent, true);
                }
            });
        }

        public BlockReference GetObject(Point3d position, List<string> attrTextValues)
        {
            if (this._blockId != ObjectId.Null)
                _createBlockRecord(_name, _annotativeState, _origin, false);

            BlockReference br = null;
            Tools.StartTransaction(() =>
                {
                    if (_blockRecordId != ObjectId.Null)
                    {
                        BlockTable bt = _db.BlockTableId.GetObjectForRead<BlockTable>();
                        BlockTableRecord ms = bt[BlockTableRecord.ModelSpace].GetObjectForWrite<BlockTableRecord>();

                        BlockTableRecord btr = _blockRecordId.GetObjectForRead<BlockTableRecord>();

                        br = new BlockReference(position, _blockRecordId);
                        
                        ObjectContextManager ocm = btr.Database.ObjectContextManager;
                        ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                        if (btr.Annotative == AnnotativeStates.True)
                        {
                            br.AddContext(occ.CurrentContext);
                        }

                        ObjectId brId = ms.AppendEntity(br);
                        _db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(br, true);

                        // Add attributes from the block table record
                        List<AttributeDefinition> attributes = btr.GetAttributes();
                        int i = 0;
                        foreach (AttributeDefinition acAtt in attributes)
                        {
                            if (!acAtt.Constant)
                            {
                                using (AttributeReference acAttRef = new AttributeReference())
                                {
                                    //acAttRef.RecordGraphicsModified(true);

                                    acAttRef.SetAttributeFromBlock(acAtt, br.BlockTransform);
                                    //acAttRef.Position = acAtt.Position.TransformBy(br.BlockTransform);

                                    acAttRef.TextString = attrTextValues[i++];

                                    //if (acAtt.Annotative == AnnotativeStates.True)
                                    //acAttRef.AddContext(occ.CurrentContext);

                                    br.AttributeCollection.AppendAttribute(acAttRef);
                                    _db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(acAttRef, true);
                                }
                            }

                        }
                        _insertPoint = position;
                        _blockId = br.Id;
                    }
                });
            return br;
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
