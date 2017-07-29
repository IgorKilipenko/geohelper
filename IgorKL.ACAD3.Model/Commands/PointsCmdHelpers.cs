using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using IgorKL.ACAD3.Model.Helpers.SdrFormat;
using wnd = System.Windows.Forms;

namespace IgorKL.ACAD3.Model.Commands
{
    public partial class PointsCmd
    {
        private string _getDbTextString(string msg, string rmsg)
        {
            PromptEntityOptions entOpt = new PromptEntityOptions(msg);
            entOpt.SetRejectMessage(rmsg);
            entOpt.AllowNone = false;
            entOpt.AddAllowedClass(typeof(DBText), true);

            PromptEntityResult entRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetEntity(entOpt);
            if (entRes.Status != PromptStatus.OK)
                return null;
            var id = entRes.ObjectId;
            if (id.IsNull)
                return null;
            using (Transaction trans = Tools.StartTransaction())
            {
                string data = ((DBText)trans.GetObject(id, OpenMode.ForRead)).TextString;
                return data;
            }
        }


        [Obsolete("Не работает")]
        private string _getTextFromEntity<T>(string msg, string rmsg)
            where T : DBObject
        {
            PromptEntityOptions entOpt = new PromptEntityOptions(msg);
            entOpt.SetRejectMessage(rmsg);
            entOpt.AllowNone = false;
            entOpt.AddAllowedClass(typeof(T), true);

            PromptEntityResult entRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetEntity(entOpt);
            if (entRes.Status != PromptStatus.OK)
                return null;
            var id = entRes.ObjectId;
            if (id.IsNull)
                return null;
            using (Transaction trans = Tools.StartTransaction())
            {
                System.Reflection.MethodInfo mi = typeof(T).GetMethod("TextString");
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(mi.ReturnParameter.Name);
                object obj = trans.GetObject(id, OpenMode.ForRead);
                string data = mi.Invoke((DBObject)obj, null) as string;
                return data;
            }
        }


        #region Text factory Helpers
        public DBText _CreateText(Point3d position, string data, string lname, double scale)
        {
            // Create a single-line text object
            DBText acText = new DBText();
            acText.SetDatabaseDefaults();
            //acText.Position = position;
            acText.Height = 2.5 * scale;
            acText.TextString = data;
            acText.Annotative = AnnotativeStates.False;
            acText.VerticalMode = TextVerticalMode.TextVerticalMid;
            acText.HorizontalMode = TextHorizontalMode.TextLeft;
            acText.AlignmentPoint = position;
            acText.Layer = lname;

            return acText;
        }

        public DBText _CreateText(Point3d position, string data, double scale)
        {
            // Create a single-line text object
            DBText acText = new DBText();
            acText.SetDatabaseDefaults();
            //acText.Position = position;
            acText.Height = 2.5 * scale;
            acText.TextString = data;
            acText.Annotative = AnnotativeStates.False;
            acText.VerticalMode = TextVerticalMode.TextVerticalMid;
            acText.HorizontalMode = TextHorizontalMode.TextLeft;
            acText.AlignmentPoint = position;

            return acText;
        }

        public DBText _CreateText(DBText elevation, string data, string lname, double scale)
        {
            // Create a single-line text object
            DBText acText = new DBText();
            acText.SetDatabaseDefaults();
            //acText.Position = position;
            acText.Height = elevation.Height;
            acText.TextString = data;
            acText.Color = Color.FromColor(System.Drawing.Color.Yellow);
            acText.Annotative = AnnotativeStates.False;
            acText.VerticalMode = TextVerticalMode.TextBottom;
            acText.HorizontalMode = TextHorizontalMode.TextLeft;
            acText.AlignmentPoint = new Point3d(elevation.AlignmentPoint.X, elevation.AlignmentPoint.Y + elevation.Height / 2d + elevation.Height * 0.1, 0);
            acText.Layer = lname;

            return acText;
        }

        #endregion

        #region Prompt select Helpers

        public static bool TrySelectObjects<T>(out IList<T> objCollection, OpenMode openMode, string msg, Transaction transaction)
where T : DBObject
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            SelectionSet set = null;
            objCollection = null;
            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.AllowDuplicates = false;

            PromptSelectionResult res = ed.GetSelection(opt);
            if (res.Status == PromptStatus.OK)
            {
                if ((set = res.Value) != null)
                {
                    ObjectId[] ids = set.GetObjectIds();
                    objCollection = new List<T>(set.Count);
                    foreach (ObjectId id in ids)
                    {
                        DBObject dbobj = transaction.GetObject(id, openMode, true);
                        T val = dbobj as T;
                        if (val != null)
                            objCollection.Add(val);
                    }

                    ed.WriteMessage(string.Format("\n{0} selected points", objCollection.Count));
                    return true;
                }
            }
            return false;
        }

        public static bool TrySelectObjects(out SelectionSet set, OpenMode openMode, string msg)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            set = null;

            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.AllowDuplicates = false;

            PromptSelectionResult res = ed.GetSelection(opt);
            if (res.Status == PromptStatus.OK)
            {
                if ((set = res.Value) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TrySelectObjects<T>(out IList<T> objCollection, OpenMode openMode, string msg)
            where T : DBObject
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            Transaction transaction = null;
            try
            {
                return TrySelectObjects<T>(out objCollection, openMode, msg, out transaction);
            }
            finally
            {
                if (transaction != null && !transaction.IsDisposed)
                    transaction.Dispose();
            }
        }
        public static bool TrySelectObjects<T>(out IList<T> objCollection, OpenMode openMode, string msg, out Transaction transaction)
            where T : DBObject
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            transaction = null;
            SelectionSet set = null;
            objCollection = null;
            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.AllowDuplicates = false;

            PromptSelectionResult res = ed.GetSelection(opt);
            if (res.Status == PromptStatus.OK)
            {
                if ((set = res.Value) != null)
                {
                    ObjectId[] ids = set.GetObjectIds();
                    objCollection = new List<T>(set.Count);
                    try
                    {
                        transaction = Tools.StartTransaction();
                        foreach (ObjectId id in ids)
                        {
                            DBObject dbobj = transaction.GetObject(id, openMode, true);
                            T val = dbobj as T;
                            if (val != null)
                                objCollection.Add(val);
                        }

                        ed.WriteMessage(string.Format("\n{0} selected points", objCollection.Count));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        ed.WriteMessage("\nError selected object.\nMsg : {0}\nTrace : {1}", ex.Message, ex.StackTrace);
                        return false;
                    }
                }
            }
            return false;
        }

        #endregion
    }


}
