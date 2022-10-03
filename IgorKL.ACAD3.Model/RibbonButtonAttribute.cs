using System;

namespace IgorKL.ACAD3.Model {
    [AttributeUsage(AttributeTargets.Method)]
    public class RibbonCommandButtonAttribute : Attribute {
        public RibbonCommandButtonAttribute(string name, string groupName)
            : base() {
            Name = name;
            GroupName = groupName;
        }

        public string Name { get; private set; }
        public string GroupName { get; private set; }
    }
}
