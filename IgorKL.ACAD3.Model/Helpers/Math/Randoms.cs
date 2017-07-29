using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model.Helpers.Math
{
    public static class Randoms
    {
        public static Random RandomGen = new Random(DateTime.Now.Second);
    }
}
