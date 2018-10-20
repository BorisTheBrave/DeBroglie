using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DeBroglie.Console.Config
{

    public class ModelConverter : JsonConverter
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
            var modelType = jsonObject["type"].Value<string>();
            ModelConfig modelConfig;
            switch (modelType)
            {
                case Overlapping.ModelTypeString:
                    modelConfig = new Overlapping();
                    break;
                case Adjacent.ModelTypeString:
                    modelConfig = new Adjacent();
                    break;
                default:
                    throw new Exception($"Unrecognized model type {modelType}");
            }
            serializer.Populate(jsonObject.CreateReader(), modelConfig);
            return modelConfig;
        }
    }
}
