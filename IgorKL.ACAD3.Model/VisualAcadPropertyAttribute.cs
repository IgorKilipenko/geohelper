using System;

namespace IgorKL.ACAD3.Model {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class VisualAcadPropertyAttribute : Attribute {
        public VisualAcadPropertyAttribute(string name) {

        }
    }
}
