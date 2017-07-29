using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model.Drawing.Helpers
{
    public class HostProvider
    {
        private System.Collections.Concurrent.ConcurrentDictionary<string, string> _savedIems;

        public HostProvider()
        {
            _savedIems = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        }

        public void Add(string key, string value)
        {
            if (!_savedIems.ContainsKey(key))
            {
                if (_savedIems.TryAdd(key, value))
                    _addNewResource(key, value);
            }
            else
            {
                _savedIems[key] = value;
                _addNewResource(key, value);
            }
        }

        public string Get(string key, string defVal)
        {
            /*using (var stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(Properties.Resources.ResourceManager.BaseName))
            {
                System.Resources.IResourceReader reader = new System.Resources.ResourceReader(stream);
                System.Collections.IDictionaryEnumerator dic = reader.GetEnumerator();

                while (dic.MoveNext())
                    if ((dic.Key as string) == key)
                        return dic.Value as string;

                return null;
            }*/

            using (var stream = Properties.Resources.ResourceManager.GetStream(key))
            {
                if (stream != null)
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
                    {
                        return sr.ReadToEnd();
                    }
                }
                else
                {
                    Add(key, defVal);
                    return defVal;
                }
            }
        }

        private void _addNewResource(string key, string value)
        {
            System.Resources.IResourceWriter writer = new System.Resources.ResourceWriter(Properties.Resources.ResourceManager.BaseName);
            writer.AddResource(key, value);
            writer.Close();
        }

        public static String Serialize<T>(T t)
        {
            using (System.IO.StringWriter sw = new System.IO.StringWriter())
            using (System.Xml.XmlWriter xw = System.Xml.XmlWriter.Create(sw))
            {
                new System.Xml.Serialization.XmlSerializer(typeof(T)).Serialize(xw, t);
                return sw.GetStringBuilder().ToString();
            }
        }

        public static T Deserialize<T>(String s_xml)
        {
            using (System.Xml.XmlReader xw = System.Xml.XmlReader.Create(new System.IO.StringReader(s_xml)))
                return (T)new System.Xml.Serialization.XmlSerializer(typeof(T)).Deserialize(xw);
        }
    }
}
