#if DEBUG_PLACEHOLDERS
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QoLFix
{
    public class QuaternionJsonConverter : JsonConverter
    {
        public QuaternionJsonConverter() { }

        public override bool CanConvert(Type objectType) =>
            objectType == typeof(Quaternion) || objectType == typeof(Quaternion?);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var arr = JArray.Load(reader);
            return new Quaternion((float)arr[0], (float)arr[1], (float)arr[2], (float)arr[3]);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;
    }
}
#endif
