using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace IgorKL.ACAD3.Model.CustomWindows
{
    public class AcadWindow : Window
    {
        private const int border = 8;
        private const int header = 23;

        Autodesk.AutoCAD.Windows.Window _win;
        private Point _start;


        public bool IsDragging { get; set; } = false;
        public Point CustomPosition { get; set; } = default(Point);
        public Dock DockPosition { get; set; } = default(Dock);



        protected internal AcadWindow()
        {
            this.Title = "AcadWindow";
            this.Width = 300;
            this.Height = 300;
            this.WindowStyle = WindowStyle.None;
            this.Background = null;
            this.ResizeMode = ResizeMode.NoResize;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ShowInTaskbar = false;
            this.PreviewMouseRightButtonDown += OnMouseRightClick;
        }

        public AcadWindow(Autodesk.AutoCAD.Windows.Window win, Dock dockPos)
            :this()
        {
            _win = win;
            DockPosition = dockPos;

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            helper.Owner = win.Handle;

            this.SizeChanged += OnSizeChanged;

        }

        private void OnSizeChanged(
          object sender, SizeChangedEventArgs e
        )
        {
            var p = GetPosition(this.DesiredSize);
            this.Left = p.X;
            this.Top = p.Y;

            GiveAutoCADFocus();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            GiveAutoCADFocus();
        }

        public static void GiveAutoCADFocus()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
                doc.Window.Focus();
            else
                Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
        }


        protected Point PointOnScreen(Point pt)
        {
            return
              Point.Add(
                _win.DeviceIndependentLocation,
                new Vector(
                  pt.X + border,
                  pt.Y + border + header
                )
              );
        }

        internal void StartDragging()
        {
            if (!this.IsDragging)
            {
                this.IsDragging = true;
                this.MouseMove += OnMouseMove;
                this.MouseRightButtonUp += OnMouseRightButtonUp;
            }
        }

        internal void StopDragging()
        {
            if (this.IsDragging)
            {
                this.MouseMove -= OnMouseMove;
                this.MouseRightButtonUp -= OnMouseRightButtonUp;
                this.IsDragging = false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var wndHelper = new System.Windows.Interop.WindowInteropHelper(this);

            int exStyle =
              (int)AcadWindowStyles.GetWindowLong(
                wndHelper.Handle,
                (int)AcadWindowStyles.GetWindowLongFields.GWL_EXSTYLE
              );

            exStyle |= (int)AcadWindowStyles.ExtendedWindowStyles.WS_EX_TOOLWINDOW;

            AcadWindowStyles.SetWindowLong(
              wndHelper.Handle,
              (int)AcadWindowStyles.GetWindowLongFields.GWL_EXSTYLE,
              (IntPtr)exStyle
            );
        }

        private void OnMouseRightClick(object s, MouseButtonEventArgs e)
        {
            // Swap the cursor (would be nice to get the pan cursor)

            this.Cursor = Cursors.ScrollAll;

            // We want to capture mouse input for the whole screen

            this.CaptureMouse();

            // Store our initial position with the right-clicked point
            // subtracted (we can simply add this point to the cursor
            // location during mouse move to position the dialog)

            _start =
              Point.Subtract(
                new Point(this.Left, this.Top),
                (Vector)this.PointToScreen(e.GetPosition(this))
              );

            // Add our event handlers

            StartDragging();
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Reset the cursor

            this.Cursor = Cursors.Arrow;

            // No longer need screen-level moude capture

            this.ReleaseMouseCapture();

            // Remove our event handlers

            StopDragging();

            // And finally set the custom location to the resting place

            this.DockPosition = Dock.Custom;
            this.CustomPosition = ScreenToPoint(new Point(this.Left, this.Top));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Move our dialog relative to the mouse movement

            var pos = this.PointToScreen(e.GetPosition(this));
            this.Left = _start.X + pos.X;
            this.Top = _start.Y + pos.Y;
        }

        private Point ScreenToPoint(Point pt)
        {
            return
              Point.Subtract(pt, (Vector)PointOnScreen(new Point(0, 0)));
        }

        protected Point GetPosition(Size sz)
        {
            // If at a custom (non-docked) location...

            if (DockPosition == Dock.Custom)
                return PointOnScreen(CustomPosition);

            // Otherwise docked in one of the four corners...

            bool right =
              DockPosition == Dock.TopRight || DockPosition == Dock.BottomRight;
            bool bottom =
              DockPosition == Dock.BottomLeft || DockPosition == Dock.BottomRight;

            var x =
              _win.DeviceIndependentLocation.X +
              (right ?
                _win.DeviceIndependentSize.Width - border - sz.Width :
                border
              );

            var y =
              _win.DeviceIndependentLocation.Y +
              (bottom ?
                _win.DeviceIndependentSize.Height - border - sz.Height :
                border + header - 1
              );

            return new System.Windows.Point((int)x, (int)y);
        }

        public enum Dock
        {
            None,
            Custom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        };

        public static class AcadWindowStyles
        {
            [Flags]
            public enum ExtendedWindowStyles
            {
                // ...
                WS_EX_TOOLWINDOW = 0x00000080,
                // ...
            }

            public enum GetWindowLongFields
            {
                // ...
                GWL_EXSTYLE = (-20),
                // ...
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowLong(
              IntPtr hWnd, int nIndex
            );

            public static IntPtr SetWindowLong(
              IntPtr hWnd, int nIndex, IntPtr dwNewLong
            )
            {
                int error = 0;
                IntPtr result = IntPtr.Zero;

                // Win32 SetWindowLong doesn't clear error on success

                SetLastError(0);

                if (IntPtr.Size == 4)
                {
                    // Use SetWindowLong

                    Int32 tempResult =
                      IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                    error = Marshal.GetLastWin32Error();
                    result = new IntPtr(tempResult);
                }
                else
                {
                    // use SetWindowLongPtr
                    result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                    error = Marshal.GetLastWin32Error();
                }

                if ((result == IntPtr.Zero) && (error != 0))
                {
                    throw new System.ComponentModel.Win32Exception(error);
                }

                return result;
            }

            [DllImport("user32.dll",
             EntryPoint = "SetWindowLongPtr",
             SetLastError = true)]
            private static extern IntPtr IntSetWindowLongPtr(
              IntPtr hWnd, int nIndex, IntPtr dwNewLong
            );

            [DllImport("user32.dll",
             EntryPoint = "SetWindowLong",
             SetLastError = true)]
            private static extern Int32 IntSetWindowLong(
              IntPtr hWnd, int nIndex, Int32 dwNewLong
            );

            private static int IntPtrToInt32(IntPtr intPtr)
            {
                return unchecked((int)intPtr.ToInt64());
            }

            [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
            public static extern void SetLastError(int dwErrorCode);

        }
    }
}
