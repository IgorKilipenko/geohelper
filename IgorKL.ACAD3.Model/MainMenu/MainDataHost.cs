using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgorKL.ACAD3.Model.MainMenu
{

    public interface IDataHost
    {
        void SetData(string name, object value);
        bool TryGetData(string name, out object data);

    }

    public class HostProvider
    {


        private const string resName = "ICmd_Settings.resources";

        private System.Collections.Concurrent.ConcurrentDictionary<string, object> _savedIems;
        object _host;
        string _hostName;
        string _path;

        public event EventHandler<KeyValueEventArgs> ValueSaved;

        public HostProvider(object host)
        {
            _savedIems = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();
            _host = host;
            _hostName = host.GetType().FullName + "__";

            _path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create)
                + "\\" + "ТочностиНЕТ";
            if (!System.IO.Directory.Exists(_path))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_path);
                }
                catch (Exception ex)
                {
                    Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                }
            }
            _path += "\\" + resName;

        }



        public T Read<T>(string key)
        {
            T value;
            TryRead<T>(key, out value);
            return value;
        }

        public T Read<T>(string key, T defaultValue)
        {
            T value;
            if (TryRead<T>(key, out value))
                return value;
            else
                return defaultValue;
        }

        public bool TryRead<T>(string name, out T value)
        {
            value = default(T);
            /*using (var stream = Properties.Resources.ResourceManager.GetStream(_hostName + name))
            {
                if (stream != null)
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
                    {
                        string xml = sr.ReadToEnd();
                        value = Deserialize<T>(xml);
                        return true;
                    }
                }
            }
            return false;*/
            string xml;
            bool res = _readXmlResorce(_hostName + name, out xml);
            if (res)
                value = Deserialize<T>(xml);
            return res;
        }

        public bool Write<T>(string name, T value)
        {
            /*using (var stream = Properties.Resources.ResourceManager.GetStream(_hostName + name))
            {
                if (stream != null)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(stream))
                    {
                        string xml = Serialize<T>(value);
                        sw.Write(xml);
                        return true;
                    }
                }
            }*/
            string newXmlValue = Serialize<T>(value);
            _addXmlResource(_hostName + name, newXmlValue);
            On_ValueSaved(new KeyValueEventArgs(name, value, typeof(T)));
            return true;
        }

        private void _addXmlResource(string key, string xml)
        {
            //string path = System.IO.Path.GetFullPath(@".\" + resName);
            
            using (System.Resources.IResourceWriter writer = new System.Resources.ResourceWriter(/*Properties.Resources.ResourceManager.BaseName*/ _path))
            {
                byte[] buffer = Encoding.Unicode.GetBytes(xml);
                writer.AddResource(key.ToLower(), buffer);
                writer.Generate();
            }
        }

        private bool _readXmlResorce(string key, out string xml)
        {
            xml = null;
            string path = System.IO.Path.GetFullPath(_path);
            if (System.IO.File.Exists(_path))
                using (System.Resources.ResourceReader rdr = new System.Resources.ResourceReader(_path))
                {
                    byte[] buffer = null;
                    /*string typeName;
                    rdr.GetResourceData(key.ToLower(), out typeName, out buffer);*/

                    var dic = rdr.GetEnumerator();
                    while (dic.MoveNext())
                    {
                        if (dic.Key.ToString() == key.ToLower())
                            buffer = (byte[])dic.Value;
                    }

                    if (buffer != null && buffer.Length > 0)
                    {
                        xml = Encoding.Unicode.GetString(buffer);
                        return true;
                    }
                }

            return false;
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

        /*public void Serialize()
        {
            foreach (var data in _savedIems)
            {
                string sv = Serialize(data.Value);
                if (TryRead(data.Key))
                    _addNewResource(data.Key, sv);
                else
                    Write(data.Key, sv);
            }
        }*/

        protected virtual void On_ValueSaved(KeyValueEventArgs e)
        {
            if (ValueSaved != null)
                ValueSaved(this, e);
        }

        public class KeyValueEventArgs:EventArgs
        {
            public KeyValueEventArgs(string name, object value, Type valueType)
            {
                Name = name;
                Value = value;
                ValueType = valueType;
            }

            public string Name { get; private set; }
            public object Value { get; private set; }
            public Type ValueType { get; private set; }
        }
    }
}
