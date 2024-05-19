using System.Collections.Generic;

namespace IgorKL.ACAD3.Model {
    public class ObjectIdNamedCollection : Dictionary<Autodesk.AutoCAD.DatabaseServices.ObjectId, string>, System.Collections.Specialized.INotifyCollectionChanged {
        public ObjectIdNamedCollection()
            : base() { }

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        public new void Clear() {
            base.Clear();
            On_CollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        protected virtual void On_CollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (CollectionChanged != null) {
                CollectionChanged(this, e);
            }
        }
    }

    public class DynObjectIDNamedCollection :
        System.Collections.ObjectModel.ObservableCollection<KeyValuePair<Autodesk.AutoCAD.DatabaseServices.ObjectId, string>> {
        public DynObjectIDNamedCollection()
            : base() {
        }
    }
}
