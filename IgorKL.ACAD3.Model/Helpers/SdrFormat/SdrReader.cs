using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IgorKL.ACAD3.Model.Helpers.SdrFormat
{
    public class SdrReader
    {

        public List<_SdrCoord> _SdrCoordParser(string path)
        {
            List<_SdrCoord> res = new List<_SdrCoord>();
            string[] lines = null;
            using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                lines = sr.ReadToEnd().Split(new[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
            foreach (var l in lines)
            {
                _SdrCoord point = _parseSdrLine(l);
                if (point != null)
                    res.Add(point);
            }
            return res;
        }

        public class _SdrCoord
        {
            public string code;
            public string name;
            public double x;
            public double y;
            public double h;
            public string code2;
        }

        public static _SdrCoord _parseSdrLine(string line)
        {
            if (line.Length < 68)
                return null;
            var format = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            _SdrCoord point = new _SdrCoord();
            point.code = line.Substring(0, 4);
            if (point.code != "08TP" && point.code != "08KI" && point.code != "09F1")
                return null;
            line = line.Remove(0, 4);
            point.name = line.Substring(0, 16).TrimStart();
            line = line.Remove(0, 16);
            if (!double.TryParse(line.Substring(0, 16).TrimEnd(), System.Globalization.NumberStyles.Number, format, out point.x))
                return null;
            line = line.Remove(0, 16);
            if (!double.TryParse(line.Substring(0, 16).TrimEnd(), System.Globalization.NumberStyles.Number, format, out point.y))
                return null;
            line = line.Remove(0, 16);
            if (!double.TryParse(line.Substring(0, 16).TrimEnd(), System.Globalization.NumberStyles.Number, format, out point.h))
                return null;
            try
            {
                line = line.Remove(0, 16);
                point.code2 = line.Substring(0, 16).Trim();
            }
            catch { }
            return point;
        }

    }
}
