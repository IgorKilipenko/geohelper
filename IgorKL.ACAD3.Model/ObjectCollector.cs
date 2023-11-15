using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgorKL.ACAD3.Model {
    public static class ObjectCollector {
        public static bool TrySelectObjects<AllowedType>(out List<AllowedType> selectedSet, string message = "\nВыберите объект: ")
            where AllowedType : DBObject {
            selectedSet = new List<AllowedType>();
            Editor ed = Tools.GetAcadEditor();

            PromptSelectionOptions pso = new PromptSelectionOptions {
                MessageForAdding = message,
                AllowDuplicates = false,
                AllowSubSelections = true,
                RejectObjectsFromNonCurrentSpace = true,
                RejectObjectsOnLockedLayers = false
            };

            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK)
                return false;

            using (Transaction trans = Tools.StartTransaction()) {
                foreach (SelectedObject so in psr.Value) {
                    AllowedType obj = trans.GetObject(so.ObjectId, OpenMode.ForRead) as AllowedType;
                    if (obj != null)
                        selectedSet.Add(obj);
                }
            }
            return true;
        }

        public static bool TrySelectAllowedClassObject<TAllowedType>(out TAllowedType result, string message = "\nВыберите объект: ")
            where TAllowedType : DBObject {
            result = null;

            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(string.Format("\nДолжен быть {0}.", typeof(TAllowedType).Name));
            peo.AddAllowedClass(typeof(TAllowedType), false);
            peo.AllowNone = false;
            var per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return false;

            using (Transaction trans = Tools.StartTransaction()) {
                result = (TAllowedType)trans.GetObject(per.ObjectId, OpenMode.ForRead);
                return true;
            }
        }

        public static bool TrySelectAllowedClassObject<AllowedType>(out AllowedType result, KeywordCollection keys, Func<PromptEntityResult, PromptStatus> keywordCollBack, string message = "\nВыберите объект: ")
            where AllowedType : DBObject {
            result = null;

            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(string.Format("\nДолжен быть {0}.", typeof(AllowedType).Name));
            peo.AddAllowedClass(typeof(AllowedType), false);
            peo.AllowNone = false;
            if (keys != null && keys.Count > 0)
                foreach (Keyword key in keys)
                    peo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);

            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK && per.Status != PromptStatus.Keyword)
                return false;

            if (per.Status == PromptStatus.Keyword) {
                if (keywordCollBack(per) == PromptStatus.OK)
                    while ((per = ed.GetEntity(peo)).Status == PromptStatus.Keyword) {
                        if (keywordCollBack(per) != PromptStatus.OK)
                            return false;
                    }
                else
                    return false;

                if (per.Status != PromptStatus.OK)
                    return false;
            }

            using (Transaction trans = Tools.StartTransaction()) {
                result = (AllowedType)trans.GetObject(per.ObjectId, OpenMode.ForRead);
                return true;
            }
        }

        public static ObjectId SelectAllowedClassObject<AllowedType>(string message, string rejectMessage)
            where AllowedType : DBObject {
            Editor ed = Tools.GetAcadEditor();
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage(rejectMessage);
            peo.AddAllowedClass(typeof(AllowedType), false);
            peo.AllowNone = false;
            var per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return ObjectId.Null;
            else
                return per.ObjectId;
        }
        public static ObjectId SelectAllowedClassObject<AllowedType>()
            where AllowedType : DBObject {
            string msg = string.Format("\nSelect a {0}: ", typeof(AllowedType).Name);
            string rejMsg = string.Format("\nThe selected object is not a {0}.", typeof(AllowedType).Name);

            return SelectAllowedClassObject<AllowedType>(msg, rejMsg);
        }


        public static int ForEachSelectedObject(Func<SelectedObject, Transaction, bool> action, string message = "\nВыберите объект: ") {
            Editor ed = Tools.GetAcadEditor();

            PromptSelectionOptions pso = new PromptSelectionOptions {
                MessageForAdding = message,
                AllowDuplicates = false,
                AllowSubSelections = true,
                RejectObjectsFromNonCurrentSpace = true,
                RejectObjectsOnLockedLayers = false
            };

            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK)
                return -1;

            int affectCount = 0;
            Tools.UseTransaction((trans, _, __) => {
                foreach (SelectedObject obj in psr.Value) {
                    bool abort = action(obj, trans);
                    if (abort) {
                        return;
                    }
                    ++affectCount;
                }
            });

            return affectCount;
        }

        public static List<DBObject> SelectObjects(Func<DBObject, bool> filter, string message = "\nВыберите объекты: ", OpenMode openMode = OpenMode.ForRead) {
            Editor ed = Tools.GetAcadEditor();
            var res = new List<DBObject>();

            PromptSelectionOptions pso = new PromptSelectionOptions {
                MessageForAdding = message,
                AllowDuplicates = false,
                AllowSubSelections = true,
                RejectObjectsFromNonCurrentSpace = true,
                RejectObjectsOnLockedLayers = false
            };

            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK)
                return res;

            using (Transaction trans = Tools.StartTransaction()) {
                foreach (SelectedObject so in psr.Value) {
                    var obj = trans.GetObject(so.ObjectId, openMode);
                    if (filter(obj))
                        res.Add(obj);
                }
            }
            return res;
        }

        public static AllowedType SelectSingleObject<AllowedType>(string message = "\nВыберите объект: ", bool allowObjectOnLockedLayer = true)
            where AllowedType : DBObject {
            Editor editor = Tools.GetAcadEditor();

            PromptEntityOptions options = new PromptEntityOptions(message) {
                AllowObjectOnLockedLayer = allowObjectOnLockedLayer,
                AllowNone = false
            };
            options.AddAllowedClass(typeof(AllowedType), false);

            PromptEntityResult promptResult = editor.GetEntity(options);
            if (promptResult.Status != PromptStatus.OK)
                return null;

            using (Transaction trans = Tools.StartTransaction()) {
                return trans.GetObject(promptResult.ObjectId, OpenMode.ForRead) as AllowedType;
            }
        }

        public delegate PromptStatus PromptAction(PromptResult promptResult);
    }
}
