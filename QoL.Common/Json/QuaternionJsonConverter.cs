using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace QoL.Common.Json
{
    public class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public QuaternionJsonConverter() { }

        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) throw new JsonException();
            var arr = JsonSerializer.Deserialize<float[]>(ref reader);
            if (arr == null) throw new JsonException();
            return new Quaternion(arr[0], arr[1], arr[2], arr[3]);
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options) =>
            throw new NotImplementedException();
    }
}
