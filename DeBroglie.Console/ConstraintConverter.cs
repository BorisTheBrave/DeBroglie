using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DeBroglie.Console
{
    public class ConstraintConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ModelConfig);
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
            ConstraintConfig constraintConfig;
            switch (type)
            {
                case PathConfig.TypeString:
                    constraintConfig = new PathConfig();
                    break;
                case BorderConfig.TypeString:
                    constraintConfig = new BorderConfig();
                    break;
                default:
                    throw new Exception($"Unrecognized constraint type {type}");
            }
            serializer.Populate(jsonObject.CreateReader(), constraintConfig);
            return constraintConfig;
        }
    }
}
