using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.AcadBlocks
{
    public class BlockTools
    {
        public static List<BlockReference> PromptBlocks(string msg = "\nSelect block references: ")
        {
            List<BlockReference> blocks;
            if (!ObjectCollector.TrySelectObjects(out blocks, msg))
                return null;
            else return blocks;
        }

        [Obsolete]
        public static List<AttributeDefinition> GetAttributes(BlockReference br, Transaction trans)
        {
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(br.BlockTableRecord, OpenMode.ForRead);
            return GetAttributes(btr, trans);
        }

        [Obsolete]
        public static List<AttributeDefinition> GetAttributes(BlockTableRecord btr, Transaction trans)
        {
            List<AttributeDefinition> res = new List<AttributeDefinition>();
            if (btr.HasAttributeDefinitions)
            {
                // Add attributes from the block table record
                foreach (ObjectId objID in btr)
                {
                    DBObject dbObj = trans.GetObject(objID, OpenMode.ForRead) as DBObject;

                    if (dbObj is AttributeDefinition)
                    {
                        AttributeDefinition acAtt = dbObj as AttributeDefinition;
                        res.Add(acAtt);
                    }
                }
            }
            return res;
        }

        [Obsolete]
        public static AttributeDefinition GetAttributeByTag(string tag, BlockTableRecord btr, Transaction trans)
        {
            var attrs = GetAttributes(btr, trans);
            foreach (var a in attrs)
                if (a.Tag == tag)
                    return a;
            return null;
        }

        [Obsolete]
        public static ObjectId CreateBlockTableRecordEx(Point3d origin, string name, List<Entity> entities, AnnotativeStates aState, bool rewriteBlock = false)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                var res = CreateBlockTableRecordEx(origin, name, entities, aState, trans, true, rewriteBlock);
                return res;
            }
        }

        [Obsolete]
        public static ObjectId CreateBlockTableRecordEx(Point3d origin, string name, List<Entity> entities, AnnotativeStates aState, Transaction trans, bool commit ,bool rewriteBlock = false)
        {
            var bt = (BlockTable)trans.GetObject(Tools.GetAcadDatabase().BlockTableId, OpenMode.ForRead);
            // Validate the provided symbol table name

            if (name != "*U")
            {
                SymbolUtilityServices.ValidateSymbolName(
                  name,
                  false
                );

                // Only set the block name if it isn't in use

                if (bt.Has(name))
                {
                    //throw new ArgumentException(string.Format("A block with this name \"{0}\" already exists.", name));
                    if (!rewriteBlock)
                        return bt[name];
                    else
                        throw new ArgumentException(string.Format("A block with this name \"{0}\" already exists.", name));
                }
            }

            // Create our new block table record...

            BlockTableRecord btr = new BlockTableRecord();

            // ... and set its properties

            btr.Name = name;
            btr.Origin = origin;
            btr.Annotative = aState;


            // Add the new block to the block table

            bt.UpgradeOpen();
            ObjectId btrId = bt.Add(btr);
            trans.AddNewlyCreatedDBObject(btr, true);

            foreach (Entity ent in entities)
            {
                btr.AppendEntity(ent);
                trans.AddNewlyCreatedDBObject(ent, true);
            }


            // Commit the transaction

            if (commit)
                trans.Commit();

            return btr.Id;

        }

        public static ObjectId CreateBlockTableRecord(string name, Point3d origin, IEnumerable<Entity> entities, AnnotativeStates annotative, bool getIfExists = false)
        {
            ObjectId btrId = ObjectId.Null;

            Tools.StartTransaction(() =>
                {
                    Transaction trans = Tools.GetAcadDatabase().TransactionManager.TopTransaction;

                    var bt = Tools.GetAcadDatabase().BlockTableId.GetObjectForRead<BlockTable>();

                    if (name != "*U")
                    {
                        SymbolUtilityServices.ValidateSymbolName(
                          name,
                          false
                        );

                        if (bt.Has(name))
                        {
                            if (!getIfExists)
                                btrId = bt[name];
                            else
                                throw new ArgumentException(string.Format("A block with this name \"{0}\" already exists.", name));
                        }
                    }

                    BlockTableRecord btr = new BlockTableRecord
                    {
                        Name = name,
                        Origin = origin,
                        Annotative = annotative
                    };

                    bt.UpgradeOpen();
                    btrId = bt.Add(btr);
                    trans.AddNewlyCreatedDBObject(btr, true);

                    foreach (Entity ent in entities)
                    {
                        btr.AppendEntity(ent);
                        trans.AddNewlyCreatedDBObject(ent, true);
                    }

                });
            return btrId;
        }

        [Obsolete]
        public static ObjectId SaveBlockTableRecord(BlockTableRecord btr)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                var bt = (BlockTable)trans.GetObject(Tools.GetAcadDatabase().BlockTableId, OpenMode.ForRead);
                // Validate the provided symbol table name

                string name = btr.Name;


                // Add the new block to the block table

                bt.UpgradeOpen();
                ObjectId btrId = bt.Add(btr);
                trans.AddNewlyCreatedDBObject(btr, true);


                // Commit the transaction

                trans.Commit();

                return btr.Id;
            }
        }

        public static ObjectId AddBlockRefToModelSpace(ObjectId blockTableRecordId, List<string> attrTextValues, Point3d location, Matrix3d matrix)
        {
            // Add a block reference to the model space
            using (Transaction trans = Tools.StartTransaction())
            {
                var res = AddBlockRefToModelSpace(blockTableRecordId, attrTextValues, location, matrix, trans, true);
                return res;
            }
        }

        [Obsolete]
        public static ObjectId AddBlockRefToModelSpace(ObjectId blockTableRecordId, 
            List<string> attrTextValues, Point3d location, Matrix3d matrix, Transaction trans, bool commit)
        {
            // Add a block reference to the model space
            BlockTableRecord ms = Tools.GetAcadBlockTableRecordModelSpace(trans, OpenMode.ForWrite);

            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(blockTableRecordId, OpenMode.ForRead);

            BlockReference br = new BlockReference(location, blockTableRecordId);
            br.TransformBy(matrix);

            ObjectContextManager ocm = btr.Database.ObjectContextManager;
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

            if (btr.Annotative == AnnotativeStates.True)
            {
                br.AddContext(occ.CurrentContext);
            }

            //br.RecordGraphicsModified(true);

            ObjectId brId = ms.AppendEntity(br);
            trans.AddNewlyCreatedDBObject(br, true);

            

            // Add attributes from the block table record
            List<AttributeDefinition> attributes = GetAttributes(btr, trans);
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

                        if (attrTextValues != null)
                            acAttRef.TextString = attrTextValues[i++];
                        else
                            acAttRef.TextString = acAtt.TextString;

                        //if (acAtt.Annotative == AnnotativeStates.True)
                        //acAttRef.AddContext(occ.CurrentContext);

                        br.AttributeCollection.AppendAttribute(acAttRef);
                        trans.AddNewlyCreatedDBObject(acAttRef, true);
                    }
                }

                // Change the attribute definition to be displayed as backwards
                //acAtt.UpgradeOpen();
                //acAtt.IsMirroredInX = true;
                //acAtt.IsMirroredInY = false;
            }
            br.RecordGraphicsModified(true);
            if (commit)
                trans.Commit();
            return brId;
        }

        public static ObjectId AppendBlockItem(Point3d insertPoint, ObjectId blockTableRecordId,
            List<string> attrTextValues, Matrix3d toWcsTransform)
        {
            ObjectId resBlockId = ObjectId.Null;
            Tools.StartTransaction(() =>
            {
                Transaction trans = Tools.GetTopTransaction();

                // Add a block reference to the model space
                BlockTableRecord ms = Tools.GetAcadBlockTableRecordModelSpace(OpenMode.ForWrite);

                BlockTableRecord btr = blockTableRecordId.GetObjectForRead<BlockTableRecord>();

                BlockReference br = new BlockReference(insertPoint, blockTableRecordId);
                br.SetDatabaseDefaults();
                br.TransformBy(toWcsTransform);

                ObjectContextManager ocm = btr.Database.ObjectContextManager;
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                if (btr.Annotative == AnnotativeStates.True)
                {
                    br.AddContext(occ.CurrentContext);
                }

                resBlockId = ms.AppendEntity(br);
                trans.AddNewlyCreatedDBObject(br, true);

                // Add attributes from the block table record
                List<AttributeDefinition> attributes = GetAttributes(btr, trans);
                int i = 0;
                foreach (AttributeDefinition acAtt in attributes)
                {
                    if (!acAtt.Constant)
                    {
                        using (AttributeReference acAttRef = new AttributeReference())
                        {
                            acAttRef.SetAttributeFromBlock(acAtt, br.BlockTransform);

                            if (attrTextValues != null)
                                acAttRef.TextString = attrTextValues[i++];
                            else
                                acAttRef.TextString = acAtt.TextString;

                            br.AttributeCollection.AppendAttribute(acAttRef);
                            trans.AddNewlyCreatedDBObject(acAttRef, true);
                        }
                    }
                }
                br.RecordGraphicsModified(true);
            });
            return resBlockId;
        }

        public static ObjectId AppendBlockItem(Point3d insertPointWcs, ObjectId blockTableRecordId,
            Dictionary<string, string> attrTextValues)
        {
            ObjectId resBlockId = ObjectId.Null;
            Tools.StartTransaction(() =>
            {
                Transaction trans = Tools.GetTopTransaction();

                // Add a block reference to the model space
                BlockTableRecord ms = Tools.GetAcadBlockTableRecordModelSpace(OpenMode.ForWrite);

                BlockTableRecord btr = blockTableRecordId.GetObjectForRead<BlockTableRecord>();

                BlockReference br = new BlockReference(insertPointWcs, blockTableRecordId);
                br.SetDatabaseDefaults();

                ObjectContextManager ocm = btr.Database.ObjectContextManager;
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                if (btr.Annotative == AnnotativeStates.True)
                {
                    br.AddContext(occ.CurrentContext);
                }

                resBlockId = ms.AppendEntity(br);
                trans.AddNewlyCreatedDBObject(br, true);

                // Add attributes from the block table record
                List<AttributeDefinition> attributes = GetAttributes(btr, trans);

                foreach (AttributeDefinition acAtt in attributes)
                {
                    acAtt.UpgradeOpen();
                    acAtt.AdjustAlignment(br.Database); //
                    acAtt.RecordGraphicsModified(true); //

                    if (!acAtt.Constant)
                    {
                        using (AttributeReference acAttRef = new AttributeReference())
                        {
                            acAttRef.SetAttributeFromBlock(acAtt, br.BlockTransform);

                            if (attrTextValues != null)
                            {
                                if (attrTextValues.ContainsKey(acAtt.Tag))
                                    acAttRef.TextString = attrTextValues[acAtt.Tag];
                                else
                                    acAttRef.TextString = acAtt.TextString;
                            }
                            else
                                acAttRef.TextString = acAtt.TextString;
                            
                            acAttRef.AdjustAlignment(br.Database);  //
                            acAttRef.RecordGraphicsModified(true);  //

                            br.AttributeCollection.AppendAttribute(acAttRef);
                            trans.AddNewlyCreatedDBObject(acAttRef, true);
                        }
                    }
                }
                br.RecordGraphicsModified(true);
            });
            return resBlockId;
        }

        public static void MirroringBlockByYAxis(BlockReference br)
        {
            CoordinateSystem.CoordinateTools.MirrorAtYAxis(br, br.Position);
            Matrix3d mat = Matrix3d.Mirroring(new Plane(br.Position, br.BlockTransform.CoordinateSystem3d.Zaxis));
            br.TransformBy(mat);
        }

        public static void MirroringBlockByXAxis(BlockReference br)
        {
            CoordinateSystem.CoordinateTools.MirrorAtXAxis(br, br.Position);
            Matrix3d mat = Matrix3d.Mirroring(new Plane(br.Position, br.BlockTransform.CoordinateSystem3d.Zaxis));
            br.TransformBy(mat);
        }

        public static List<Entity> GetEntities(BlockTableRecord btr, Transaction trans)
        {
            List<Entity> res = new List<Entity>();
            foreach (ObjectId objID in btr)
            {
                DBObject dbObj = trans.GetObject(objID, OpenMode.ForRead) as DBObject;

                if (!(dbObj is AttributeDefinition))
                {
                    res.Add(dbObj as Entity);
                }
            }
            return res;
        }

        public static ObjectId GetAnonymCopy(ObjectId brId, Transaction trans, bool commit)
        {
            BlockReference baseBr = (BlockReference)brId.GetObject(OpenMode.ForRead, true, true);
            BlockTableRecord baseBtr = (BlockTableRecord)baseBr.BlockTableRecord.GetObject(OpenMode.ForRead, false, true);
            var bt = (BlockTable)trans.GetObject(Tools.GetAcadDatabase().BlockTableId, OpenMode.ForRead);

            Matrix3d mat = Matrix3d.Identity;
            mat = mat.PreMultiplyBy(baseBr.BlockTransform);
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(baseBtr.Origin - baseBr.Position));

            BlockTableRecord btr = new BlockTableRecord() { Name = "*U" };
            btr.Annotative = baseBtr.Annotative;
            btr.Origin = baseBtr.Origin;

            // Add the new block to the block table

            bt.UpgradeOpen();
            ObjectId btrId = bt.Add(btr);
            trans.AddNewlyCreatedDBObject(btr, true);

            foreach (ObjectId id in baseBtr)
            {
                Entity ent = id.GetObject(OpenMode.ForRead, false, true) as Entity;
                if (ent != null)
                {
                    ent = ent.GetTransformedCopy(mat);
                    btr.AppendEntity(ent);
                    trans.AddNewlyCreatedDBObject(ent, true);
                }
            }

            // Commit the transaction

            if (commit)
                trans.Commit();

            return btr.Id;
        }

        public static ObjectId GetAnonymCopy(ObjectId brId)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                var res = GetAnonymCopy(brId, trans, true);
                return res;
            }
        }


        public static ObjectId GetTransformedAnonymCopy(BlockTableRecord btr, Matrix3d matrix)
        {
            ObjectId res = ObjectId.Null;
            Database db = Tools.GetAcadDatabase();
            Tools.StartTransaction(() =>
            {
                Matrix3d mat = Matrix3d.Identity.PreMultiplyBy(matrix);
                btr = btr.Id.GetObjectForRead<BlockTableRecord>(true);

                BlockTableRecord btr2 = new BlockTableRecord();

                btr2.Name = "*U";
                btr2.Annotative = btr.Annotative;
                btr2.Origin = btr.Origin;

                var bt = db.BlockTableId.GetObjectForWrite<BlockTable>();

                var btrId = bt.Add(btr2);
                db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(btr2, true);

                foreach (ObjectId id in btr)
                {
                    Entity ent = (Entity)id.GetObjectForRead<Entity>(true).Clone();

                    ent.TransformBy(mat);

                    btr2.AppendEntity(ent);
                    db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(ent, true);
                }
                res = btr2.Id;
            });
            return res;
        }
    }    

    


}
