using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DeBroglie.Console.Config
{
    public class PathSpecConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AbstractPathSpecConfig);
        }
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var type = jsonObject["type"].Value<string>();
            AbstractPathSpecConfig pathSpecConfig;
            switch (type)
            {
                case PathSpecConfig.TypeString:
                    pathSpecConfig = new PathSpecConfig();
                    break;
                case EdgedPathSpecConfig.TypeString:
                    pathSpecConfig = new EdgedPathSpecConfig();
                    break;
                default:
                    throw new ConfigurationException($"Unrecognized model type {type}");
            }
            serializer.Populate(jsonObject.CreateReader(), pathSpecConfig);
            return pathSpecConfig;
        }
    }
}
