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

namespace IgorKL.ACAD3.Model
{
    public static class ObjectCollector
    {
        public static bool TrySelectObjects<T>(out List<T> selectedSet, string message = "\nВыберите объект: ")
            where T : DBObject
        {
            selectedSet = new List<T>();
            Editor ed = Tools.GetAcadEditor();

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = message;
            pso.AllowDuplicates = false;
            pso.AllowSubSelections = true;
            pso.RejectObjectsFromNonCurrentSpace = true;
            pso.RejectObjectsOnLockedLayers = false;

            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK)
                return false;

            using (Transaction trans = Tools.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    T obj = trans.GetObject(so.ObjectId, OpenMode.ForRead) as T;
                    if (obj != null)
                        selectedSet.Add(obj);
                }
            }
            return true;
        }

        public static bool TrySelectAllowedClassObject<TAllowedType>(out TAllowedType result, string message = "\nВыберите объект: ")
            where TAllowedType : DBObject
        {
            result = null;

            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(string.Format("\nДолжен быть {0}.", typeof(TAllowedType).Name));
            peo.AddAllowedClass(typeof(TAllowedType), false);
            peo.AllowNone = false;
            var per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return false;

            using (Transaction trans = Tools.StartTransaction())
            {
                result = (TAllowedType)trans.GetObject(per.ObjectId, OpenMode.ForRead);
                return true;
            }
        }

        public static bool TrySelectAllowedClassObject<TAllowedType>(out TAllowedType result, KeywordCollection keys, Func<PromptEntityResult, PromptStatus> keywordCollBack ,string message = "\nВыберите объект: ")
            where TAllowedType : DBObject
        {
            result = null;

            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(string.Format("\nДолжен быть {0}.", typeof(TAllowedType).Name));
            peo.AddAllowedClass(typeof(TAllowedType), false);
            peo.AllowNone = false;
            if (keys != null && keys.Count > 0)
                foreach (Keyword key in keys)
                    peo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);

            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK && per.Status != PromptStatus.Keyword)
                return false;

            if (per.Status == PromptStatus.Keyword)
            {
                if (keywordCollBack(per) == PromptStatus.OK)
                    while ((per = ed.GetEntity(peo)).Status == PromptStatus.Keyword)
                    {
                        if (keywordCollBack(per) != PromptStatus.OK)
                            return false;
                    }
                else
                    return false;

                if (per.Status != PromptStatus.OK)
                    return false;
            }

            using (Transaction trans = Tools.StartTransaction())
            {
                result = (TAllowedType)trans.GetObject(per.ObjectId, OpenMode.ForRead);
                return true;
            }
        }

        public static ObjectId SelectAllowedClassObject<TAllowedType>(string message, string rejectMessage)
            where TAllowedType : DBObject
        {
            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(rejectMessage);
            peo.AddAllowedClass(typeof(TAllowedType), false);
            peo.AllowNone = false;
            var per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return ObjectId.Null;
            else
                return per.ObjectId;
        }
        public static ObjectId SelectAllowedClassObject<TAllowedType>()
            where TAllowedType : DBObject
        {
            string msg = string.Format("\nSelect a {0}: ", typeof(TAllowedType).Name);
            string rejMsg = string.Format("\nThe selected object is not a {0}.", typeof(TAllowedType).Name);

            return SelectAllowedClassObject<TAllowedType>(msg, rejMsg);
        }

        public delegate PromptStatus PromptAction(PromptResult promptResult);
    }
}
