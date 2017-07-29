using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;


namespace IgorKL.ACAD3.Model.CustomObjects
{
    public abstract class EntityOverride
    {
        /*public bool annotative;
        private BlockTableRecord blockRecord;
        private Matrix3d matrix;
        private Lazy<ObjectIdCollection> objectCollection;

        public EntityOverride()
        {
            BlockTableRecord record = new BlockTableRecord { Name = "*U" };
            this.blockRecord = record;
            this.objectCollection = new Lazy<ObjectIdCollection>(() => new ObjectIdCollection());
            this.matrix = Matrix3d.get_Identity();
        }

        public bool Annotative { get; set; }
        public ObjectId BlockId { get; set; }
        public BlockTableRecord BlockRecord
        {
            get
            {
                if (!this.BlockId.IsNull)
                {
                    Document document = Application.get_DocumentManager().get_MdiActiveDocument();
                    using (document.LockDocument((DocumentLockMode)20, null, null, true))
                    {
                        using (Transaction transaction = document.get_TransactionManager().StartTransaction())
                        {
                            BlockReference reference = (BlockReference)this.BlockId.GetObject((OpenMode)OpenMode.ForWrite);
                            this.blockRecord = (BlockTableRecord)reference.BlockTableRecord.GetObject((OpenMode)OpenMode.ForWrite);
                            if (this.blockRecord.GetBlockReferenceIds(true, true).Count > 1)
                            {
                                BlockTableRecord record = new BlockTableRecord()
                                {
                                    Name = "*U"
                                };
                                this.blockRecord = record;
                                using (BlockTable table = (BlockTable)Tools.GetAcadDatabase().BlockTableId.GetObject(OpenMode.ForWrite, false, true))
                                {
                                    if (this.Annotative)
                                    {
                                        this.blockRecord.set_Annotative((AnnotativeStates)AnnotativeStates.True);
                                    }
                                    table.Add((SymbolTableRecord)this.blockRecord);
                                    transaction.AddNewlyCreatedDBObject((DBObject)this.blockRecord, true);
                                }
                                reference.set_BlockTableRecord(this.blockRecord.get_Id());
                            }
                            else
                            {
                                using (BlockTableRecordEnumerator enumerator = this.blockRecord.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        enumerator.get_Current().GetObject((OpenMode)OpenMode.ForWrite).Erase();
                                    }
                                }
                            }
                            transaction.Commit();
                        }
                        using (Transaction transaction2 = document.get_TransactionManager().StartTransaction())
                        {
                            BlockReference reference2 = (BlockReference)this.BlockId.GetObject((OpenMode)OpenMode.ForWrite);
                            this.blockRecord = (BlockTableRecord)reference2.get_BlockTableRecord().GetObject((OpenMode)OpenMode.ForWrite);
                            Matrix3d matrixd = Matrix3d.Displacement(-((Entity)this).InsertionPoint.GetAsVector());
                            foreach (Entity entity in this.Entities)
                            {
                                Entity transformedCopy = entity.GetTransformedCopy(matrixd);
                                this.objectCollection.Value.Add(this.blockRecord.AppendEntity(transformedCopy));
                                document.get_TransactionManager().AddNewlyCreatedDBObject((DBObject)transformedCopy, true);
                            }
                            transaction2.Commit();
                        }
                        //Helper.EntityUpdate(this.BlockId);
                        document.get_TransactionManager().FlushGraphics();
                        return this.blockRecord;
                    }
                }
            }
            set
            {
                this.blockRecord = value;
            }

        }
        public abstract IEnumerable<Entity> Entities { get; }
        public ObjectId Id { get; set; }
        public bool IsValueCreated { get; set; }
        public bool NeedTransform { get; set; }
        public ObjectIdCollection ObjectCollection
        {
            get
            {
                return this.objectCollection.Value;
            }
        }


        public void Draw(WorldDraw draw)
        {
            WorldGeometry geometry = draw.get_Geometry();
            foreach (Entity entity in this.Entities)
            {
                geometry.Draw((Drawable)entity);
            }
        }
        */



    }
}
