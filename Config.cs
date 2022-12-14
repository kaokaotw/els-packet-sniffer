using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace MapleShark
{
    public sealed class Config
    {
        public string Interface = "";
        public ushort LowPort = 8585;
        public ushort HighPort = 8586;
        public List<Definition> Definitions = new List<Definition>();

        private static Config sInstance = null;
        internal static Config Instance
        {
            get
            {
                if (sInstance == null)
                {
                    if (!File.Exists("Config.xml"))
                    {
                        sInstance = new Config();
                        sInstance.Save();
                    }
                    else
                    {
                        using (XmlReader xr = XmlReader.Create("Config.xml"))
                        {
                            XmlSerializer xs = new XmlSerializer(typeof(Config));
                            sInstance = xs.Deserialize(xr) as Config;
                        }
                    }
                }
                return sInstance;
            }
        }

		internal Definition GetDefinition(ushort pBuild, ushort pLocale, bool pOutbound, ushort pOpcode) {
			return Definitions.Find(d => d.Locale == pLocale && d.Build == pBuild && d.Outbound == pOutbound && d.Opcode == pOpcode);
		}

        internal void SaveProperties()
        {
            Dictionary<ushort, Dictionary<ushort, SortedDictionary<ushort, string>>>[] headerList = new Dictionary<ushort, Dictionary<ushort, SortedDictionary<ushort, string>>>[2];
            for (int i = 0; i < 2; i++)
            {
                headerList[i] = new Dictionary<ushort, Dictionary<ushort, SortedDictionary<ushort, string>>>();
            }
            foreach (var d in Definitions)
            {
                if (d.Opcode == 0xFFFF) return;
                byte outbound = (byte)(d.Outbound ? 1 : 0);

                if (!headerList[outbound].ContainsKey(d.Locale))
                    headerList[outbound].Add(d.Locale, new Dictionary<ushort, SortedDictionary<ushort, string>>());
                if (!headerList[outbound][d.Locale].ContainsKey(d.Build))
                    headerList[outbound][d.Locale].Add(d.Build, new SortedDictionary<ushort, string>());
                if (!headerList[outbound][d.Locale][d.Build].ContainsKey(d.Opcode))
                    headerList[outbound][d.Locale][d.Build].Add(d.Opcode, d.Name);
                else
                    headerList[outbound][d.Locale][d.Build][d.Opcode] = d.Name;
            };

            for (int i = 0; i < 2; i++)
            {
                foreach (var dict in headerList[i])
                {
                    string map = "Scripts" + Path.DirectorySeparatorChar + dict.Key.ToString();
                    if (!Directory.Exists(map))
                        Directory.CreateDirectory(map);

                    foreach (KeyValuePair<ushort, SortedDictionary<ushort, string>> kvp in dict.Value)
                    {
                        string map2 = map + Path.DirectorySeparatorChar + kvp.Key;
                        if (!Directory.Exists(map2))
                            Directory.CreateDirectory(map2);

                        string buff = "";
                        buff += "# Generated by MapleShark\r\n";
                        foreach (KeyValuePair<ushort, string> kvp2 in kvp.Value)
                        {
                            buff += string.Format("{0} = 0x{1:X4}\r\n", kvp2.Value == "" ? "# NOT SET: " : kvp2.Value.Replace(' ', '_'), kvp2.Key);
                        }
                        File.WriteAllText(map2 + Path.DirectorySeparatorChar + (i == 0 ? "send" : "recv") + ".properties", buff);
                    }

                }
            }
        }

        internal static string GetPropertiesFile(bool pOutbound, byte pLocale, ushort pVersion)
        {
            return System.Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Scripts" + Path.DirectorySeparatorChar + pLocale.ToString() + Path.DirectorySeparatorChar + pVersion.ToString() + Path.DirectorySeparatorChar + (pOutbound ? "send" : "recv") + ".properties";
        }

        internal void Save()
        {
            XmlWriterSettings xws = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = true,
                OmitXmlDeclaration = true
            };
            using (XmlWriter xw = XmlWriter.Create("Config.xml", xws))
            {
                XmlSerializer xs = new XmlSerializer(typeof(Config));
                xs.Serialize(xw, this);
            }
            if (!Directory.Exists("Scripts")) Directory.CreateDirectory("Scripts");
            SaveProperties();
        }
    }
}
