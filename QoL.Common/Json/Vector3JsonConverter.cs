using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace QoL.Common.Json
{
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public Vector3JsonConverter() { }

        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) throw new JsonException();
            var arr = JsonSerializer.Deserialize<float[]>(ref reader);
            if (arr == null) throw new JsonException();
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) =>
            throw new NotImplementedException();
    }
}
