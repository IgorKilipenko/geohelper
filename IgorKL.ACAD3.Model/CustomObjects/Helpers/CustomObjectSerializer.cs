using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Permissions;

using Autodesk.AutoCAD.Runtime;
using acApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace IgorKL.ACAD3.Model.CustomObjects.Helpers
{
    [Serializable]
    public abstract class CustomObjectSerializer : ISerializable
    {
        //public const string appName = "MyApp";
        public abstract string ApplicationName { get; }

        public CustomObjectSerializer()
        {
        }

        public static object NewFromResBuf(ResultBuffer resBuf)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new AcadBinder();

            MemoryStream ms = SerializeUtil.ResBufToStream(resBuf);

            CustomObjectSerializer mbc = (CustomObjectSerializer)bf.Deserialize(ms);

            return mbc;
        }

        public object NewFromEntity(Entity ent)
        {
            using (
              ResultBuffer resBuf = ent.GetXDataForApplication(ApplicationName))
            {
                return NewFromResBuf(resBuf);
            }
        }
        public static object NewFromEntity(Entity ent, string appName)
        {
            using (
              ResultBuffer resBuf = ent.GetXDataForApplication(appName))
            {
                return NewFromResBuf(resBuf);
            }
        }

        public ResultBuffer SaveToResBuf()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            ms.Position = 0;

            ResultBuffer resBuf = SerializeUtil.StreamToResBuf(ms, ApplicationName);

            return resBuf;
        }

        public void SaveToEntity(Entity ent)
        {
            // Make sure application name is registered
            // If we were to save the ResultBuffer to an Xrecord.Data,
            // then we would not need to have a registered application name

            Transaction tr =
              ent.Database.TransactionManager.TopTransaction;

            RegAppTable regTable =
              (RegAppTable)tr.GetObject(
                ent.Database.RegAppTableId, OpenMode.ForWrite);
            if (!regTable.Has(ApplicationName))
            {
                RegAppTableRecord app = new RegAppTableRecord();
                app.Name = ApplicationName;
                regTable.Add(app);
                tr.AddNewlyCreatedDBObject(app, true);
            }

            using (ResultBuffer resBuf = SaveToResBuf())
            {
                ent.XData = resBuf;
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand,
           Flags = SecurityPermissionFlag.SerializationFormatter)]
        public abstract void GetObjectData(
          SerializationInfo info, StreamingContext context);

        /*
        [Serializable]
        public class AcadSerializer : CustomObjectSerializer
        {
            public string myString;
            public double myDouble;

            public AcadSerializer()
            {
            }

            protected AcadSerializer(
              SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new System.ArgumentNullException("info");

                myString = (string)info.GetValue("MyString", typeof(string));
                myDouble = (double)info.GetValue("MyDouble", typeof(double));
            }

            [SecurityPermission(SecurityAction.LinkDemand,
               Flags = SecurityPermissionFlag.SerializationFormatter)]
            public override void GetObjectData(
              SerializationInfo info, StreamingContext context)
            {
                info.AddValue("MyString", myString);
                info.AddValue("MyDouble", myDouble);
            }

            // Just for testing purposes

            public override string ToString()
            {
                return base.ToString() + "," +
                  myString + "," + myDouble.ToString();
            }
        }*/

    }

    public sealed class AcadBinder : SerializationBinder
    {
        public override System.Type BindToType(
          string assemblyName,
          string typeName)
        {
            return Type.GetType(string.Format("{0}, {1}",
              typeName, assemblyName));
        }
    }

    public class SerializeUtil
    {
        const int kMaxChunkSize = 127;

        public static ResultBuffer StreamToResBuf(
          MemoryStream ms, string appName)
        {
            ResultBuffer resBuf =
              new ResultBuffer(
                new TypedValue(
                  (int)DxfCode.ExtendedDataRegAppName, appName));

            for (int i = 0; i < ms.Length; i += kMaxChunkSize)
            {
                int length = (int)Math.Min(ms.Length - i, kMaxChunkSize);
                byte[] datachunk = new byte[length];
                ms.Read(datachunk, 0, length);
                resBuf.Add(
                  new TypedValue(
                    (int)DxfCode.ExtendedDataBinaryChunk, datachunk));
            }

            return resBuf;
        }

        public static MemoryStream ResBufToStream(ResultBuffer resBuf)
        {
            MemoryStream ms = new MemoryStream();
            TypedValue[] values = resBuf.AsArray();

            // Start from 1 to skip application name

            for (int i = 1; i < values.Length; i++)
            {
                byte[] datachunk = (byte[])values[i].Value;
                ms.Write(datachunk, 0, datachunk.Length);
            }
            ms.Position = 0;

            return ms;
        }
    }
}
