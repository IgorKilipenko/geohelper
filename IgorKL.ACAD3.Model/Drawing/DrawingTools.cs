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
using Autodesk.AutoCAD.GraphicsInterface;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class DrawingTools
    {
        public class DynamicPreviewer
        {
            TransientGraphicsTools.SelectableTransient _st;

            [CommandMethod("TRS2")]
            public void DisplayLine()
            {
                
                // Create a line and pass it to the SelectableTransient
                // This makes cleaning up much more straightforward

                List<Entity> lines = new List<Entity>( new[] {
                    new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                    new Line(new Point3d(5, 4, 0), new Point3d(7,1, 0)),
                    new Line(new Point3d(3, 8, 0), new Point3d(17,11, 0))
                });

                _st = new TransientGraphicsTools.SelectableTransient(lines);

                _st.Display();
            }

            [CommandMethod("TRU2")]
            public void Stop()
            {
                _st.StopDisplaying();
            }

            [CommandMethod("TRS3")]
            public void DisplayLine2()
            {
                _st.Display();
            }
        }

        public class SelectableTransient : Transient
        {
            // Windows messages we care about

            const int WM_LBUTTONDOWN = 513;
            const int WM_LBUTTONUP = 514;

            // Internal state

            Entity _ent = null;
            bool _picked = false, _clicked = false;

            public SelectableTransient(Entity ent)
            {
                _ent = ent;
            }

            protected override int SubSetAttributes(DrawableTraits traits)
            {
                // If the cursor is over the entity, make it colored
                // (whether it's red or yellow will depend on whether
                // there's a mouse-button click, too)

                traits.Color = (short)(_picked ? (_clicked ? 1 : 2) : 0);

                return (int)DrawableAttributes.None;
            }

            protected override void SubViewportDraw(ViewportDraw vd)
            {
                _ent.ViewportDraw(vd);
            }

            protected override bool SubWorldDraw(WorldDraw wd)
            {
                _ent.WorldDraw(wd);

                return true;
            }

            protected override void OnDeviceInput(DeviceInputEventArgs e)
            {
                bool redraw = false;

                if (e.Message == WM_LBUTTONDOWN)
                {
                    _clicked = true;

                    // If we're over the entity, absorb the click
                    // (stops the window selection from happening)

                    if (_picked)
                    {
                        e.Handled = true;
                    }
                    redraw = true;
                }
                else if (e.Message == WM_LBUTTONUP)
                {
                    _clicked = false;
                    redraw = true;
                }

                // Only update the graphics if things have changed

                if (redraw)
                {
                    TransientManager.CurrentTransientManager.UpdateTransient(
                      this, new IntegerCollection()
                    );

                    // Force a Windows message, as we may have absorbed the
                    // click event (and this also helps when unclicking)

                    ForceMessage();
                }

                base.OnDeviceInput(e);
            }

            private void ForceMessage()
            {
                // Set the cursor without ectually moving it - enough to
                // generate a Windows message

                System.Drawing.Point pt =
                  System.Windows.Forms.Cursor.Position;
                System.Windows.Forms.Cursor.Position =
                  new System.Drawing.Point(pt.X, pt.Y);
            }

            protected override void OnPointInput(PointInputEventArgs e)
            {
                bool wasPicked = _picked;

                _picked = false;

                Curve cv = _ent as Curve;
                if (cv != null)
                {
                    Point3d pt =
                      cv.GetClosestPointTo(e.Context.ComputedPoint, false);
                    if (
                      pt.DistanceTo(e.Context.ComputedPoint) <= 0.1
                        // Tolerance.Global.EqualPoint is too small
                    )
                    {
                        _picked = true;
                    }
                }

                // Only update the graphics if things have changed

                if (_picked != wasPicked)
                {
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
        public void TransientSelection()
        {
            // Create a line and pass it to the SelectableTransient
            // This makes cleaning up much more straightforward

            _ln = new Line(Point3d.Origin, new Point3d(10, 10, 0));
            _st = new SelectableTransient(_ln);

            // Tell AutoCAD to call into this transient's extended
            // protocol when appropriate

            Transient.CapturedDrawable = _st;

            // Go ahead and draw the transient

            TransientManager.CurrentTransientManager.AddTransient(
              _st, TransientDrawingMode.DirectShortTerm,
              128, new IntegerCollection()
            );
        }

        [CommandMethod("TRU")]
        public void RemoveTransientSelection()
        {
            // Removal is performed by setting to null

            Transient.CapturedDrawable = null;

            // Erase the transient graphics and dispose of the transient

            if (_st != null)
            {
                TransientManager.CurrentTransientManager.EraseTransient(
                  _st,
                  new IntegerCollection()
                );
                _st.Dispose();
                _st = null;
            }

            // And dispose of our line

            if (_ln != null)
            {
                _ln.Dispose();
                _ln = null;
            }
        }
    }
}
