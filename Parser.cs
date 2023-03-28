using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModLib
{
    /// <summary>
    /// A simple class for parsing fabric minecraft mod files
    /// </summary>
    public class Parser
    {

        private static string ExtractFabricJson(string modPath)
        {
            if (string.IsNullOrEmpty(modPath)) throw new Exception("Invalid mod path provided.");
            using (var jarc = ZipFile.OpenRead(modPath))
            {
                var jsonEntry = jarc.GetEntry("fabric.mod.json");
                if (jsonEntry == null) throw new Exception("fabric.mod.json not found in mod.");
                using (var reader = new StreamReader(jsonEntry.Open()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        public class ModMeta
        {
            public string SchemaVersion { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public List<string> Authors { get; set; }
            public Dictionary<string, string> Contact { get; set; }
            //license
            public string Icon { get; set; }
            public string Environment { get; set; }
            //entrypoints
            //mixins - this may be relevant for checking compatibility or some shit 
            //accessWidener
            //custom
            public Dictionary<string, string> Depends { get; set; }
            public Dictionary<string, string> Breaks { get; set; }

            public ModMeta FromModFile(string modPath)
            {
                var data = ExtractFabricJson(modPath);
                if (string.IsNullOrEmpty(data)) throw new Exception("Failed to read fabric.mod.json");
                
                // parse & sanity check
                var json = JObject.Parse(data);
                if (!json.ContainsKey("schemaVersion")) throw new BadJsonException("Missing 'schemaVersion'");
                if (!json.ContainsKey("name")) throw new BadJsonException("Missing 'name'");
                if (!json.ContainsKey("description")) throw new BadJsonException("Missing 'description'");
                if (!json.ContainsKey("authors")) throw new BadJsonException("Missing 'authors'");
                if (!json.ContainsKey("contact")) throw new BadJsonException("Missing 'contact'");
                //if (!json.ContainsKey("icon")) throw new BadJsonException("Missing 'icon'"); // todo not 100% sure if mods "need" an icon in the json
                if (!json.ContainsKey("environment")) throw new BadJsonException("Missing 'environment'");
                
                //var json = JsonConvert.DeserializeObject<dynamic>(data);
                //if (json.schemaVersion == null || json.name == null || json.id == null || json.version == null || json.description == null || json.authors == null
                //    || json.contact == null || json.icon == null || json.environment == null) throw new Exception("invalid or corrupt fabric.mod.json");
                
                SchemaVersion = (string)json["schemaVersion"];
                Name = (string)json["name"];
                Version = (string)json["version"];
                Description = (string)json["description"];

                Authors = new List<string>();
                var authorsArray = (JArray)json["authors"];
                if (authorsArray == null) throw new BadJsonException("Missing 'authors'"); // should never happen but
                foreach (var author in authorsArray) Authors.Add((string)author);
                
                Contact = ParseDict(json, "contact");
                //if (json.icon != null) Icon = json.icon.ToString();
                if (json.ContainsKey("icon")) Icon = (string)json["icon"];
                Environment = (string)json["environment"];
                if (json.ContainsKey("depends")) Depends = ParseDict(json, "depends");
                if (!json.ContainsKey("breaks")) return this;

                Breaks = ParseDict(json, "breaks");
                return this;
            }
        }
        
        private static Dictionary<string, string> ParseDict(JObject json, string valueName)
        {
            //var mod = JObject.Parse(json);
            //var result = new Dictionary<string, string>();

            if (!(json.GetValue(valueName) is JObject value)) return null;
            var contactDictionary = value.Properties()
                .Select(p => new KeyValuePair<string, string>(p.Name, p.Value.ToString()))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            return contactDictionary;
        }
        
        public class Properties
        {
            private Dictionary<string, string> _data;

            public string Get(string field) { return _data.ContainsKey(field) ? _data[field] : null; }
            public string Get(string field, string defaultValue) { return Get(field) == null ? defaultValue : Get(field); }
            public void Set(string field, object value)
            {
                if (!_data.ContainsKey(field)) _data.Add(field, value.ToString());
                else _data[field] = value.ToString();
            }

            public Properties FromString(string propData)
            {
                if (string.IsNullOrEmpty(propData)) return null;
                _data = new Dictionary<string, string>();
                using (var sr = new StringReader(propData))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!ShouldParse(line)) continue;
                        int index = line.IndexOf('=');
                        string key = line.Substring(0, index).Trim();
                        if (_data.ContainsKey(key)) continue;
                        string value = line.Substring(index + 1).Trim();
                        if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'"))) value = value.Substring(1, value.Length - 2);
                        _data.Add(key, value);
                    }
                }
                return this;
            }

            private static bool ShouldParse(string line)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("'")) return false;
                return line.Contains("=");
            }
        }
        
        // Exceptions
        public class BadJsonException : Exception
        {
            public BadJsonException(string message) : base(message)
            {
                
            }
        }
        
    }
}