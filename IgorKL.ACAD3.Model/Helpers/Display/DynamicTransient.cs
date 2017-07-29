using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace IgorKL.ACAD3.Model.Helpers.Display
{
    public class DynamicTransient : IDisposable
    {
        private List<ViewBag> _markers;
        private TransientManager _tm;
        private bool _isDisplaying;

        public DynamicTransient()
        {
            this._markers = new List<ViewBag>();
            this._tm = TransientManager.CurrentTransientManager;
        }

        public void AddMarker(DBObject marker, object tag = null,TransientDrawingMode viewMode = TransientDrawingMode.Highlight, short colorIndex = 128)
        {
            lock (_markers)
            {
                _markers.Add(new ViewBag() { Element = marker, ViewMode = viewMode, Tag = tag, ColorIndex = colorIndex });
            }
        }

        public int RemoveMarkersAt(DBObject marker)
        {
            lock (_markers)
            {
                return _markers.RemoveAll(x => x.Element == marker);
            }
        }

        public int RemoveMarkersAt(object tag)
        {
            lock (_markers)
            {
                return _markers.RemoveAll(x => x.Tag == tag);
            }
        }

        public int RemoveMarkersAt(TransientDrawingMode viewMode)
        {
            lock (_markers)
            {
                return _markers.RemoveAll(x => x.ViewMode == viewMode);
            }
        }

        public T FindAt<T>(Predicate<T> mach)
            where T : DBObject
        {
            lock (_markers)
            {
                for (int i = 0; i < _markers.Count; i++)
                {
                    if (!(_markers is T))
                        continue;
                    if (mach((T)_markers[i].Element))
                        return (T)_markers[i].Element;
                }
                return default(T);
            }
        }

        public IEnumerable<DBObject> FindAllAtTag(object tag)
        {
            lock (_markers)
            {
                var res = _markers.FindAll(bag => bag.Tag == tag);
                return res.Select(x => x.Element);
            }
        }

        public List<T> FindAllAtTag<T>(object tag)
            where T:DBObject
        {
            lock (_markers)
            {
                var res = _markers.FindAll(bag => bag.Tag == tag);
                return new List<T>(res.Select(x => x.Element).Cast<T>());
            }
        }

        private void Display(DBObject marker, TransientDrawingMode viewMode, short colorIndex)
        {
            IntegerCollection intCol
                            = new IntegerCollection();
            
            _tm.AddTransient
                (
                    marker,
                    viewMode,
                    //128,
                    colorIndex,
                    intCol
                );

            _isDisplaying = true;
        }

        public void Display()
        {
            foreach (ViewBag viewBag in _markers)
            {
                Display(viewBag.Element, viewBag.ViewMode, viewBag.ColorIndex);
            }
        }

        public List<DBObject> GetClonedMarkers()
        {
            lock (_markers)
            {
                List<DBObject> res = new List<DBObject>(_markers.Count());
                foreach (var marker in _markers)
                    res.Add((DBObject)(marker.Element).Clone());
                return res;
            }
        }

        public void ClearTransientGraphics()
        {
            if (_isDisplaying)
            {
                IntegerCollection intCol = new IntegerCollection();
                if (_markers != null)
                {
                    lock (_markers)
                    {
                        DBObject marker;
                        for (int i = 0; i < _markers.Count; i++)
                        {
                            marker = _markers[i].Element;
                            _tm.EraseTransient(marker, intCol);
                            marker.Dispose();

                        }
                    }
                }
            }
            _markers.Clear();
            _isDisplaying = false;
        }

        public void Dispose()
        {
            ClearTransientGraphics();
        }
    }

    public class ViewBag:IDisposable
    {
        public ViewBag()
        {
             //this.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.None, 128);
        }

        public DBObject Element { get; set; }
        public TransientDrawingMode ViewMode { get; set; }
        public object Tag{get;set;}
        public short ColorIndex { get; set; }

        public void Dispose()
        {
            if (this.Element != null)
                if (!this.Element.IsDisposed)
                    this.Element.Dispose();
        }
    }

}

