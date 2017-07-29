using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model
{
    public class ObjectIdNamedCollection : Dictionary<Autodesk.AutoCAD.DatabaseServices.ObjectId, string>, System.Collections.Specialized.INotifyCollectionChanged
    {
        public ObjectIdNamedCollection()
            :base()
        { }

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        /*public new void Add(Autodesk.AutoCAD.DatabaseServices.ObjectId id, string name)
        {
            base.Add(id, name);
            On_CollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add));
        }

        public new bool Remove(Autodesk.AutoCAD.DatabaseServices.ObjectId id)
        {
            bool res = base.Remove(id);
            if (res)
            {
                On_CollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Remove));
            }
            return res;
        }*/
        
        public new void Clear()
        {
            base.Clear();
            On_CollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        protected virtual void On_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }
    }

    public class DynObjectIDNamedCollection :
        System.Collections.ObjectModel.ObservableCollection<KeyValuePair<Autodesk.AutoCAD.DatabaseServices.ObjectId, string>>
    {
        public DynObjectIDNamedCollection()
            :base()
        {
            
        }
    }
}
