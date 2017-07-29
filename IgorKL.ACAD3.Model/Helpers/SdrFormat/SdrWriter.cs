using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model.Helpers.SdrFormat
{
    public class SdrWriter
    {
        public class SdrFormatter
        {
            /*const string had = @"00NMSDR33                               113111
10NM>RED EXPORT 33  121111
13CCPlane Curvature Correction: Yes                             ";*/

            public static SdrLine CreateSdrLine(bool isStn)
            {
                SdrLine line = new SdrLine();
                line.AddField(0, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 4 });
                line.AddField(1, new SdrField() { Direction = SdrField.FieldDirection.RTL, Length = 16 });
                line.AddField(2, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 16 });
                line.AddField(3, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 16 });
                line.AddField(4, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 16 });
                line.AddField(5, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 16 });
                if (isStn)
                    line.AddField(6, new SdrField() { Direction = SdrField.FieldDirection.LTR, Length = 16 });
                return line;
            }
        }

        public struct SdrPoint
        {
            public double x;
            public double y;
            public double h;
            public int n;
            public string code;
            public string pointCode;
            public string description;
        }

        public class SdrField
        {
            string value = null;

            public int Length { get; set; }
            public string Value { get { return value; } set { this.value = value; } }
            public FieldDirection Direction { get; set; }

            public override string ToString()
            {
                if (Value != null)
                    return Direction == FieldDirection.LTR ? Value.ToString().PadRight(this.Length) : Value.ToString().PadLeft(this.Length);
                else return "";
            }

            public enum FieldDirection
            {
                LTR = 0,
                RTL
            }

        }

        public class SdrLine
        {
            private Dictionary<int, SdrField> fields = new Dictionary<int, SdrField>();

            public void AddField(int number, SdrField field)
            {
                this.fields.Add(number, field);
            }

            public Dictionary<int, SdrField> Fields
            {
                get { return fields; }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (int n in fields.Keys)
                {
                    //if (n != null)
                    sb.Append(fields[n].ToString());
                }
                return sb.ToString();
            }

            public SdrLine CreateNewLine()
            {
                SdrLine line = new SdrLine();
                foreach (var f in this.fields)
                    line.AddField(f.Key, f.Value);
                return line;
            }
        }
    }
}
