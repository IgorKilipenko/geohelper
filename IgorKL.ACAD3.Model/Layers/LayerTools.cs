using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

namespace IgorKL.ACAD3.Model.Layers
{
    public class LayerTools
    {
        /// <summary>
        /// http://through-the-interface.typepad.com/through_the_interface/2010/01/creating-an-autocad-layer-using-net.html
        /// </summary>
        public static void CreateHiddenLayer(string layerName, bool isHidden = false ,bool isFrozen = false)
        {
            short _colorIndex = 0;

            Document doc =
              Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr =
              db.TransactionManager.StartTransaction();
            using (tr)
            {
                LayerTable lt =
                  (LayerTable)tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead
                  );

                try
                {
                    SymbolUtilityServices.ValidateSymbolName(
                      layerName,
                      false
                    );

                    if (lt.Has(layerName))
                    {
                        ed.WriteMessage(
                          "\nA layer with this name already exists."
                        );
                        return;
                    }
                }
                catch
                {
                    ed.WriteMessage(
                      "\nInvalid layer name."
                    );
                    return;
                }

                LayerTableRecord ltr = new LayerTableRecord();

                ltr.Name = layerName;
                ltr.Color =
                  Color.FromColorIndex(ColorMethod.ByAci, _colorIndex);
                ltr.IsFrozen = isFrozen;
                ltr.IsHidden = isHidden;


                lt.UpgradeOpen();
                ObjectId ltId = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);

                tr.Commit();

                ed.WriteMessage(
                  "\nCreated layer named \"{0}\" with " +
                  "a color index of {1}.",
                  layerName, _colorIndex++
                );

            }

        }
    }
}
