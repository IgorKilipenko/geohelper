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
    public class SimpleDynamicTransient:IDisposable
    {
        private List<DBObject> _markers;
        private TransientManager _tm; 

        public SimpleDynamicTransient()
        {
            this._markers = new List<DBObject>();
            this._tm = TransientManager.CurrentTransientManager;
        }

        public void AddMarker(DBObject marker)
        {
            lock (_markers)
            {
                _markers.Add(marker);
            }
        }

        public bool RemoveMarkerAt(DBObject marker)
        {
            lock (_markers)
            {
                return _markers.Remove(marker);
            }
        }
        public bool RemoveMarkerAt<T>(Predicate<T> mach)
            where T:DBObject
        {
            lock (_markers)
            {
                for (int i = 0; i < _markers.Count; i++)
                {
                    if (!(_markers is T))
                        continue;
                    return mach((T)_markers[i]);
                }
                return false;
            }
        }

        public T FindAt<T>(Predicate<T> mach)
            where T:DBObject
        {
            lock (_markers)
            {
                for (int i = 0; i < _markers.Count; i++)
                {
                    if (!(_markers is T))
                        continue;
                    if (mach((T)_markers[i]))
                        return (T)_markers[i];
                }
                return default(T);
            }
        }

        private void Display(DBObject marker)
        {
            IntegerCollection intCol
                            = new IntegerCollection();

            _tm.AddTransient
                (
                    marker,
                    TransientDrawingMode.Highlight,
                    128,
                    intCol
                );
        }

        public void Display()
        {
            foreach (DBObject marker in _markers)
            {
                Display(marker);
            }
        }

        public List<DBObject> GetClonedMarkers()
        {
            lock (_markers)
            {
                List<DBObject> res = new List<DBObject>(_markers.Count());
                foreach (var marker in _markers)
                    res.Add((DBObject)marker.Clone());
                return res;
            }
        }

        public void ClearTransientGraphics()
        {
            IntegerCollection intCol = new IntegerCollection();
            if (_markers != null)
            {
                lock (_markers)
                {
                    DBObject marker;
                    for (int i = 0; i < _markers.Count; i++)
                    {
                        marker = _markers[i];
                        _tm.EraseTransient(marker, intCol);
                        marker.Dispose();

                    }
                }
            }
            _markers = new List<DBObject>();
        }

        public void Dispose()
        {
            ClearTransientGraphics();
        }
    }
}
