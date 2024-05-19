using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Collections.Generic;

namespace IgorKL.ACAD3.Model.Drawing {
    public class TransientGraphicsTools {
        public class SelectableTransient : Transient {
            // Windows messages we care about
            const int WM_LBUTTONDOWN = 513;
            const int WM_LBUTTONUP = 514;

            // Internal state
            public List<Entity> EntitiyList { get; set; }
            bool _picked = false, _clicked = false;

            public SelectableTransient(List<Entity> enties) {
                this.EntitiyList = new List<Entity>(enties.Count);
                foreach (var _ent in enties)
                    this.EntitiyList.Add((Entity)_ent/*.Clone()*/);
            }

            protected override int SubSetAttributes(DrawableTraits traits) {
                traits.Color = (short)(_picked ? (_clicked ? 1 : 2) : 0);

                return (int)DrawableAttributes.None;
            }

            protected override void SubViewportDraw(ViewportDraw vd) {
                foreach (var _ent in EntitiyList)
                    _ent.ViewportDraw(vd);
            }

            protected override bool SubWorldDraw(WorldDraw wd) {
                foreach (var _ent in EntitiyList)
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

                foreach (var _ent in EntitiyList) {
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

                    if (_picked != wasPicked)
                        return;
                }
            }

            public void Display() {
                Transient.CapturedDrawable = this;

                TransientManager.CurrentTransientManager.AddTransient(
                  this, TransientDrawingMode.DirectShortTerm,
                  128, new IntegerCollection()
                );
            }


            public void StopDisplaying() {
                Transient.CapturedDrawable = null;

                TransientManager.CurrentTransientManager.EraseTransient(
                    this,
                    new IntegerCollection()
                );
            }

            public void DisplayUpdate() {
                Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager.UpdateTransient(
                    this, new IntegerCollection());
            }

            protected override void Dispose(bool value) {
                if (!this.IsDisposed) {
                    // Dispose of all entities
                    for (int i = 0; i < EntitiyList.Count; i++) {
                        if (EntitiyList[i] != null && !EntitiyList[i].IsDisposed)
                            EntitiyList[i].Dispose();
                        EntitiyList[i] = null;
                    }
                    this.EntitiyList = null;
                }

                base.Dispose(value);
            }
        }

        public class TransientTest : Transient {
            // Windows messages we care about
            const int WM_LBUTTONDOWN = 513;
            const int WM_LBUTTONUP = 514;

            // Internal state
            public List<Entity> EntitiyList { get; set; }
            bool _picked = false, _clicked = false;

            public TransientTest(List<Entity> enties) {
                this.EntitiyList = new List<Entity>(enties.Count);
                foreach (var _ent in enties)
                    this.EntitiyList.Add((Entity)_ent);
            }

            protected override int SubSetAttributes(DrawableTraits traits) {
                traits.Color = (short)(_picked ? (_clicked ? 1 : 2) : 0);

                return (int)DrawableAttributes.None;
            }

            protected override void SubViewportDraw(ViewportDraw vd) {
                foreach (var _ent in EntitiyList)
                    _ent.ViewportDraw(vd);
            }

            protected override bool SubWorldDraw(WorldDraw wd) {
                foreach (var _ent in EntitiyList)
                    _ent.WorldDraw(wd);

                return true;
            }

            protected override void OnDeviceInput(DeviceInputEventArgs e) {
                base.OnDeviceInput(e);
            }

            private void ForceMessage() {
                System.Drawing.Point pt =
                  System.Windows.Forms.Cursor.Position;
                System.Windows.Forms.Cursor.Position =
                  new System.Drawing.Point(pt.X, pt.Y);
            }

            protected override void OnPointInput(PointInputEventArgs e) {
                base.OnPointInput(e);
            }

            public void Display() {
                Transient.CapturedDrawable = this;

                TransientManager.CurrentTransientManager.AddTransient(
                  this, TransientDrawingMode.Contrast,
                  128, new IntegerCollection()
                );
            }

            public void StopDisplaying() {
                Transient.CapturedDrawable = null;

                TransientManager.CurrentTransientManager.EraseTransient(
                    this,
                    new IntegerCollection()
                );
            }

            public void DisplayUpdate() {
                Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager.UpdateTransient(
                    this, new IntegerCollection());
            }

            protected override void Dispose(bool value) {
                if (!this.IsDisposed) {
                    for (int i = 0; i < EntitiyList.Count; i++) {
                        if (EntitiyList[i] != null && !EntitiyList[i].IsDisposed)
                            EntitiyList[i].Dispose();
                        EntitiyList[i] = null;
                    }
                    this.EntitiyList = null;
                }

                base.Dispose(value);
            }
        }
    }
}
