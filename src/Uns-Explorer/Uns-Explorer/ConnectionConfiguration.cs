using System.Text.Json;
using Uns.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Uns.Explorer
{
    public class ConnectionConfiguration
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        public Dictionary<string, object> Parameters { get; set; }


        public string Display
        {
            get
            {
                if (Type != null)
                {
                    switch (Type.ToLower())
                    {
                        case "mqtt":
                            var mqttServer = GetParameter("server");
                            var mqttPort = GetParameter("port");
                            return $"{mqttServer}:{mqttPort}";

                        case "sparkplug-b":
                            var sparkplubBServer = GetParameter("server");
                            var sparkplubBPort = GetParameter("port");
                            return $"{sparkplubBServer}:{sparkplubBPort}";
                            break;
                    }                  
                }

                return null;
            }
        }



        public bool ParameterExists(string name)
        {
            return !Parameters.IsNullOrEmpty() && Parameters.ContainsKey(name);
        }

        public string GetParameter(string name)
        {
            if (!Parameters.IsNullOrEmpty())
            {
                Parameters.TryGetValue(name, out var obj);
                if (obj != null) return obj.ToString();
            }

            return null;
        }

        public T GetParameter<T>(string name)
        {
            if (!Parameters.IsNullOrEmpty() && !string.IsNullOrEmpty(name))
            {
                Parameters.TryGetValue(name, out var obj);
                if (obj != null)
                {
                    var x = obj;
                    if (obj.GetType() == typeof(JsonElement) && ((JsonElement)x).ValueKind == JsonValueKind.Number) x = x.ToInt();


                    try
                    {
                        return (T)Convert.ChangeType(obj, typeof(T));
                    }
                    catch { }
                }
            }

            return default(T);
        }

        public void SetParameter(string name, object value)
        {
            if (!string.IsNullOrEmpty(name) && value != null)
            {
                if (Parameters == null) Parameters = new Dictionary<string, object>();
                Parameters.Remove(name);
                Parameters.Add(name, value.ToString());
            }
        }



        public static ConnectionConfiguration ReadYaml(string configurationPath)
        {
            if (!string.IsNullOrEmpty(configurationPath))
            {
                try
                {
                    var text = File.ReadAllText(configurationPath);
                    if (!string.IsNullOrEmpty(text))
                    {
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();

                        return deserializer.Deserialize<ConnectionConfiguration>(text);
                    }
                }
                catch { }
            }

            return null;
        }

        public void SaveYaml(string configurationPath)
        {
            if (!string.IsNullOrEmpty(configurationPath))
            {
                var path = configurationPath + ".yaml";

                try
                {
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    var yaml = serializer.Serialize(this);
                    File.WriteAllText(path, yaml);
                }
                catch { }
            }
        }
    }
}
