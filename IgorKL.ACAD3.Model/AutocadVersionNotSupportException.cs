using System;

namespace IgorKL.ACAD3.Model {
    public class AutocadVersionNotSupportException : NotSupportedException {
        public AutocadVersionNotSupportException()
            : this("Текущая версия автокада не поддерживается") {

        }
        
        public AutocadVersionNotSupportException(string msg)
            : base(msg) {

        }
    }
}
