using System;

namespace IgorKL.ACAD3.Model {
    [AttributeUsage(AttributeTargets.Method)]
    public class RibbonCommandButtonAttribute : Attribute {
        public RibbonCommandButtonAttribute(string name, string groupName, bool isCivilCmd = false)
            : base() {
            Name = name;
            GroupName = groupName;
            IsCivilCmd = isCivilCmd;
        }

        public string Name { get; private set; }
        public string GroupName { get; private set; }
        public bool IsCivilCmd { get; private set; }
    }
}
