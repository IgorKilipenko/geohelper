using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model
{
    public class AutocadVersionNotSupportException:NotSupportedException
    {
        public AutocadVersionNotSupportException()
            :this("Текущая версия автокада не поддерживается")
        {

        }
        public AutocadVersionNotSupportException(string msg)
            :base(msg)
        {

        }
    }
}
