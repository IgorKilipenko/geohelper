using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class BlockExtensions
    {
        [Obsolete]
        public static AttributeReference GetAttributeByTag(this BlockReference br, string tag, Transaction trans)
        {
            foreach (ObjectId id in br.AttributeCollection)
            {
                AttributeReference ar = (AttributeReference)id.GetObject(OpenMode.ForRead, true, true);
                if (ar.Tag == tag)
                    return ar;
            }
            return null;
        }

        public static AttributeReference GetAttributeByTag(this BlockReference br, string tag)
        {
            foreach (ObjectId id in br.AttributeCollection)
            {
                AttributeReference ar = (AttributeReference)id.GetObject(OpenMode.ForRead, true, true);
                if (ar.Tag == tag)
                    return ar;
            }
            return null;
        }

        public static List<AttributeDefinition> GetAttributes(this BlockTableRecord btr)
        {
            List<AttributeDefinition> res = new List<AttributeDefinition>();
            Tools.StartOpenCloseTransaction(() =>
            {
                
                if (btr.HasAttributeDefinitions)
                {
                    // Add attributes from the block table record
                    foreach (ObjectId objID in btr)
                    {
                        DBObject dbObj = btr.Database.TransactionManager.TopTransaction.GetObject(objID, OpenMode.ForRead) as DBObject;

                        if (dbObj is AttributeDefinition)
                        {
                            AttributeDefinition acAtt = dbObj as AttributeDefinition;
                            res.Add(acAtt);
                        }
                    }
                }
            });
            return res;
        }

        public static void EraseBolckTableRecord(this BlockTableRecord btr)
        {
            Tools.StartTransaction(() =>
            {
                if (btr.Id != ObjectId.Null)
                {
                    btr = btr.Id.GetObjectForRead<BlockTableRecord>(false);
                    if (!btr.IsErased)
                    {
                        btr.UpgradeOpen();
                        using (BlockTableRecordEnumerator enumerator = btr.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                enumerator.Current.GetObject((OpenMode)OpenMode.ForWrite).Erase();
                            }
                        }
                        btr.DowngradeOpen();
                    }
                }
            });
        }

        public static ObjectId GetAnonymClone(this BlockReference br, Point3d position)
        {
            ObjectId res = ObjectId.Null;
            Tools.StartTransaction(() =>
                {

                    br = br.Id.GetObjectForRead<BlockReference>(true);
                    DBObjectCollection entIds = new DBObjectCollection();
                    ((BlockReference)br.Clone()).Explode(entIds);

                    List<Entity> ents = new List<Entity>();
                    foreach (DBObject obj in entIds)
                    {
                        Entity ent = (Entity)obj;
                        ent.TransformBy(br.BlockTransform);
                        ents.Add(ent);
                    }
                    List<string> attrVals = new List<string>();
                    foreach (ObjectId id in br.AttributeCollection)
                    {
                        AttributeReference ar = (AttributeReference)id.GetObject(OpenMode.ForRead, true, true);
                        attrVals.Add(ar.TextString);
                    }
                    ObjectId btrId = IgorKL.ACAD3.Model.AcadBlocks.BlockTools.CreateBlockTableRecordEx(Point3d.Origin, "*U", ents, AnnotativeStates.True);
                    res = IgorKL.ACAD3.Model.AcadBlocks.BlockTools.AddBlockRefToModelSpace(btrId, attrVals, position, br.BlockTransform);
                });
            Tools.StartTransaction(() =>
                {
                    res.GetObjectForRead<BlockReference>().BlockTableRecord.GetObjectForRead<BlockTableRecord>().Update();
                });
            return res;
        }

        public static void Update(this BlockTableRecord btr)
        {
            Tools.StartTransaction(() =>
            {
                if (btr.Id != ObjectId.Null)
                {
                    btr = btr.Id.GetObjectForRead<BlockTableRecord>(false);
                    btr.UpgradeOpen();
                    foreach (ObjectId id in btr.GetBlockReferenceIds(true, false))
                    {
                        BlockReference br = id.GetObjectForWrite<BlockReference>(false);
                        br.RecordGraphicsModified(true);
                        btr.Database.TransactionManager.QueueForGraphicsFlush();
                    }
                    foreach (ObjectId id in btr.GetAnonymousBlockIds())
                    {
                        BlockReference br = id.GetObjectForWrite<BlockReference>(false);
                        br.RecordGraphicsModified(true);
                        btr.Database.TransactionManager.QueueForGraphicsFlush();
                    }
                }
            });
        }

        public static void InnerTransform(this BlockReference br, Matrix3d transform)
        {
            Tools.StartTransaction(() =>
                {
                    br = br.Id.GetObjectForRead<BlockReference>();
                    //br = br.GetAnonymClone(br.Position).GetObjectForRead<BlockReference>();
                    BlockTableRecord btr = br.BlockTableRecord.GetObjectForRead<BlockTableRecord>();
                    btr.UpgradeOpen();
                    foreach (ObjectId id in btr)
                    {
                        Entity ent = id.GetObjectForWrite<Entity>();
                        ent.TransformBy(transform);
                    }
                    br.UpgradeOpen();
                    br.RecordGraphicsModified(true);
                    br.Database.TransactionManager.QueueForGraphicsFlush();
                });
        }

        public static BlockReference InnerTransform2(this BlockReference br, Matrix3d transform)
        {
            BlockReference res = null;
            Tools.StartTransaction(() =>
            {
                br = br.Id.GetObjectForRead<BlockReference>();
                br = br.GetAnonymClone(br.Position).GetObjectForRead<BlockReference>();
               
                BlockTableRecord btr = br.BlockTableRecord.GetObjectForRead<BlockTableRecord>();
                btr.UpgradeOpen();
                foreach (ObjectId id in btr)
                {
                    Entity ent = id.GetObjectForWrite<Entity>();
                    ent.TransformBy(transform);
                }
                br.UpgradeOpen();
                br.RecordGraphicsModified(true);
                res = br;
                
            });
            res.Database.TransactionManager.QueueForGraphicsFlush();
            return res;
        }

        public static void DeepErase(this BlockTableRecord btr, bool erasing = true)
        {
            Tools.StartTransaction(() =>
            {
                foreach (var item in btr)
                {
                    item.GetObjectForWrite<DBObject>().Erase(erasing);
                }
            });
        }



        #region Insert AttributeReferance
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef)
        {
            Database dbCurrent = HostApplicationServices.WorkingDatabase;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = dbCurrent.TransactionManager;
            using (Transaction tr = tm.StartTransaction())
            {
                BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId idAtt in btAttRec)
                {
                    Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                    if (ent is AttributeDefinition)
                    {
                        AttributeDefinition attDef = (AttributeDefinition)ent;
                        AttributeReference attRef = new AttributeReference();
                        attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                        ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                        tr.AddNewlyCreatedDBObject(attRef, true);
                    }
                }
                tr.Commit();
            }
        }
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        /// <param name="tr">Transaction</param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef, Transaction tr)
        {
            BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
            foreach (ObjectId idAtt in btAttRec)
            {
                Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                if (ent is AttributeDefinition)
                {
                    AttributeDefinition attDef = (AttributeDefinition)ent;
                    AttributeReference attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                    ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                }
            }
        }
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        /// <param name="strAttributeText">The textstring for all attributes</param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef, string strAttributeText)
        {
            Database dbCurrent = HostApplicationServices.WorkingDatabase;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = dbCurrent.TransactionManager;
            using (Transaction tr = tm.StartTransaction())
            {
                BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId idAtt in btAttRec)
                {
                    Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                    if (ent is AttributeDefinition)
                    {
                        AttributeDefinition attDef = (AttributeDefinition)ent;
                        AttributeReference attRef = new AttributeReference();
                        attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                        attRef.TextString = strAttributeText;
                        ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                        tr.AddNewlyCreatedDBObject(attRef, true);
                    }
                }
                tr.Commit();
            }
        }
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        /// <param name="strAttributeText">The textstring for all attributes</param>
        /// <param name="tr">Transaction</param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef, string strAttributeText, Transaction tr)
        {
            BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
            foreach (ObjectId idAtt in btAttRec)
            {
                Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                if (ent is AttributeDefinition)
                {
                    AttributeDefinition attDef = (AttributeDefinition)ent;
                    AttributeReference attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                    attRef.TextString = strAttributeText;
                    ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                }
            }
        }
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        /// <param name="strAttributeTag">The tag to insert the <paramref name="strAttributeText"/></param>
        /// <param name="strAttributeText">The textstring for <paramref name="strAttributeTag"/></param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef, string strAttributeTag, string strAttributeText)
        {
            Database dbCurrent = HostApplicationServices.WorkingDatabase;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = dbCurrent.TransactionManager;
            using (Transaction tr = tm.StartTransaction())
            {
                BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId idAtt in btAttRec)
                {
                    Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                    if (ent is AttributeDefinition)
                    {
                        AttributeDefinition attDef = (AttributeDefinition)ent;
                        AttributeReference attRef = new AttributeReference();
                        attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                        if (attRef.Tag == strAttributeTag)
                            attRef.TextString = strAttributeText;
                        ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                        tr.AddNewlyCreatedDBObject(attRef, true);
                    }
                }
                tr.Commit();
            }
        }
        /// <summary>
        /// Inserts all attributreferences
        /// </summary>
        /// <param name="blkRef">Blockreference to append the attributes</param>
        /// <param name="strAttributeTag">The tag to insert the <paramref name="strAttributeText"/></param>
        /// <param name="strAttributeText">The textstring for <paramref name="strAttributeTag"/></param>
        /// <param name="tr">Transacton</param>
        public static void InsertBlockAttibuteRef(this BlockReference blkRef, string strAttributeTag, string strAttributeText, Transaction tr)
        {
            BlockTableRecord btAttRec = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
            foreach (ObjectId idAtt in btAttRec)
            {
                Entity ent = (Entity)tr.GetObject(idAtt, OpenMode.ForRead);
                if (ent is AttributeDefinition)
                {
                    AttributeDefinition attDef = (AttributeDefinition)ent;
                    AttributeReference attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                    if (attRef.Tag == strAttributeTag)
                        attRef.TextString = strAttributeText;
                    ObjectId idTemp = blkRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                }
            }
        }
        #endregion

        public static AttributeDefinition ConvertToAttribute(this DBText text, string tag, string prompt)
        {
            AttributeDefinition ad = new AttributeDefinition();
            ad.SetDatabaseDefaults(HostApplicationServices.WorkingDatabase);
            ad.TextString = text.TextString;
            ad.HorizontalMode = text.HorizontalMode;
            ad.VerticalMode = text.VerticalMode;
            ad.Height = text.Height;
            ad.Annotative = text.Annotative;
            ad.Constant = false;
            ad.Verifiable = true;
            ad.Rotation = text.Rotation;
            ad.Tag = tag;
            ad.Prompt = prompt;
            ad.Position = text.Position;
            ad.AlignmentPoint = text.AlignmentPoint;
            ad.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            return ad;
        }
    }
}
