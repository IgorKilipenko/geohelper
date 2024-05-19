using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace IgorKL.ACAD3.Model.Drawing {
    public class DrawingTools {
        public class DynamicPreviewer {
            TransientGraphicsTools.SelectableTransient _st;

            [CommandMethod("TRS2")]
            public void DisplayLine() {
                List<Entity> lines = new List<Entity>(new[] {
                    new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                    new Line(new Point3d(5, 4, 0), new Point3d(7,1, 0)),
                    new Line(new Point3d(3, 8, 0), new Point3d(17,11, 0))
                });

                _st = new TransientGraphicsTools.SelectableTransient(lines);

                _st.Display();
            }

            [CommandMethod("TRU2")]
            public void Stop() {
                _st.StopDisplaying();
            }

            [CommandMethod("TRS3")]
            public void DisplayLine2() {
                _st.Display();
            }
        }

        public class SelectableTransient : Transient {
            const int WM_LBUTTONDOWN = 513;
            const int WM_LBUTTONUP = 514;

            // Internal state
            Entity _ent = null;
            bool _picked = false, _clicked = false;

            public SelectableTransient(Entity ent) {
                _ent = ent;
            }

            protected override int SubSetAttributes(DrawableTraits traits) {
                traits.Color = (short)(_picked ? (_clicked ? 1 : 2) : 0);

                return (int)DrawableAttributes.None;
            }

            protected override void SubViewportDraw(ViewportDraw vd) {
                _ent.ViewportDraw(vd);
            }

            protected override bool SubWorldDraw(WorldDraw wd) {
                _ent.WorldDraw(wd);

                return true;
            }

            protected override void OnDeviceInput(DeviceInputEventArgs e) {
                bool redraw = false;

                if (e.Message == WM_LBUTTONDOWN) {
                    _clicked = true;

                    if (_picked) {
                        e.Handled = true;
                    }
                    redraw = true;
                } else if (e.Message == WM_LBUTTONUP) {
                    _clicked = false;
                    redraw = true;
                }

                if (redraw) {
                    TransientManager.CurrentTransientManager.UpdateTransient(
                      this, new IntegerCollection()
                    );

                    ForceMessage();
                }

                base.OnDeviceInput(e);
            }

            private void ForceMessage() {
                System.Drawing.Point pt =
                  System.Windows.Forms.Cursor.Position;
                System.Windows.Forms.Cursor.Position =
                  new System.Drawing.Point(pt.X, pt.Y);
            }

            protected override void OnPointInput(PointInputEventArgs e) {
                bool wasPicked = _picked;

                _picked = false;

                Curve cv = _ent as Curve;
                if (cv != null) {
                    Point3d pt =
                      cv.GetClosestPointTo(e.Context.ComputedPoint, false);
                    if (
                      pt.DistanceTo(e.Context.ComputedPoint) <= 0.1
                    ) {
                        _picked = true;
                    }
                }

                if (_picked != wasPicked) {
                    TransientManager.CurrentTransientManager.UpdateTransient(
                      this, new IntegerCollection()
                    );
                }
                base.OnPointInput(e);
            }
        }


        Line _ln = null;
        SelectableTransient _st = null;

        [CommandMethod("TRS")]
        public void TransientSelection() {
            _ln = new Line(Point3d.Origin, new Point3d(10, 10, 0));
            _st = new SelectableTransient(_ln);

            Transient.CapturedDrawable = _st;

            TransientManager.CurrentTransientManager.AddTransient(
              _st, TransientDrawingMode.DirectShortTerm,
              128, new IntegerCollection()
            );
        }

        [CommandMethod("TRU")]
        public void RemoveTransientSelection() {
            Transient.CapturedDrawable = null;

            if (_st != null) {
                TransientManager.CurrentTransientManager.EraseTransient(
                  _st,
                  new IntegerCollection()
                );
                _st.Dispose();
                _st = null;
            }

            // And dispose of our line

            if (_ln != null) {
                _ln.Dispose();
                _ln = null;
            }
        }
    }
}
