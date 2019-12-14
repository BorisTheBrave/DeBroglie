using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DeBroglie.Console.Config
{
    public class ConstraintArrayConverter : JsonConverter
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
            var jsonArray = JArray.Load(reader);
            var constraints = new List<ConstraintConfig>();
            foreach (var jsonObject in jsonArray)
            {
                var type = jsonObject["type"].Value<string>();
                ConstraintConfig constraintConfig;
                switch (type)
                {
                    case PathConfig.TypeString:
                        constraintConfig = new PathConfig();
                        break;
                    case EdgedPathConfig.TypeString:
                        constraintConfig = new EdgedPathConfig();
                        break;
                    case BorderConfig.TypeString:
                        constraintConfig = new BorderConfig();
                        break;
                    case FixedTileConfig.TypeString:
                        constraintConfig = new FixedTileConfig();
                        break;
                    case MaxConsecutiveConfig.TypeString:
                        constraintConfig = new MaxConsecutiveConfig();
                        break;
                    case MirrorConfig.TypeString:
                        constraintConfig = new MirrorConfig();
                        break;
                    case CountConfig.TypeString:
                        constraintConfig = new CountConfig();
                        break;
                    default:
                        throw new ConfigurationException($"Unrecognized constraint type {type}");
                }
                serializer.Populate(jsonObject.CreateReader(), constraintConfig);
                constraints.Add(constraintConfig);
            }
            return constraints;
        }
    }
}
