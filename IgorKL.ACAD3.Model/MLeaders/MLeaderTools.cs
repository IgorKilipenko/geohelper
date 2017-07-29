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

namespace IgorKL.ACAD3.Model.MLeaders
{
    public class MLeaderTools
    {
        public static string CopyTextContents(MLeader sourceLeader, MLeader destLeader)
        {
            if (destLeader.ContentType == ContentType.MTextContent)
                if (destLeader.ContentType == sourceLeader.ContentType)
                {
                    using (Transaction trans = Tools.StartTransaction())
                    {
                        destLeader = trans.GetObject(destLeader.Id, OpenMode.ForWrite) as MLeader;
                        MText mText = destLeader.MText;
                        mText.Contents = sourceLeader.MText.Contents;
                        destLeader.MText = mText;

                        trans.Commit();
                        return destLeader.MText.Contents;
                    }
                }
            return null;
        }

        public static ObjectId CreateMLeader(MText mText, Point3d location)
        {
            MLeader leader = new MLeader();
            leader.SetDatabaseDefaults();

            leader.ContentType = ContentType.MTextContent;

            leader.MText = mText;

            int idx = leader.AddLeaderLine(new Point3d(1, 1, 0));
            leader.AddFirstVertex(idx, new Point3d(0, 0, 0));

            using (Transaction trans = Tools.StartTransaction())
            {
                return Tools.AppendEntityEx(trans, leader, true);
            }
        }


        /// <summary>
        /// http://adndevblog.typepad.com/autocad/2012/05/how-to-create-mleader-objects-in-net.html
        /// </summary>
        /// <param name="blkLeader"></param>
        /// <param name="location"></param>
        /// <param name="firstVertex"></param>
        /// <param name="secondVertex"></param>
        /// <returns></returns>
        public static ObjectId CreateMLeader(BlockTableRecord blkLeader, Point3d location ,Point3d firstVertex, Point3d secondVertex)
        {
            MLeader leader = new MLeader();
            leader.SetDatabaseDefaults();

            leader.ContentType = ContentType.BlockContent;

            leader.BlockContentId = blkLeader.Id;
            leader.BlockPosition = location;

            int idx = leader.AddLeaderLine(secondVertex);
            leader.AddFirstVertex(idx, firstVertex);

            //Handle Block Attributes
            //int AttNumber = 0;

            //Doesn't take in consideration oLeader.BlockRotation
            /*Matrix3d Transfo = Matrix3d.Displacement(
                leader.BlockPosition.GetAsVector());*/
            using (Transaction trans = Tools.StartTransaction())
            {
                /*foreach (ObjectId blkEntId in blkLeader)
                {
                    AttributeDefinition AttributeDef = trans.GetObject(
                        blkEntId,
                        OpenMode.ForRead)
                            as AttributeDefinition;

                    if (AttributeDef != null)
                    {
                        AttributeReference AttributeRef =
                            new AttributeReference();

                        AttributeRef.SetAttributeFromBlock(
                            AttributeDef,
                            Transfo);

                        AttributeRef.Position =
                            AttributeDef.Position.TransformBy(Transfo);

                        AttributeRef.TextString = "Attrib #" + (++AttNumber);

                        leader.SetBlockAttribute(blkEntId, AttributeRef);
                    }
                }*/

                return Tools.AppendEntityEx(trans, leader, true);
            }
        }
    }
}
