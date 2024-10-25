using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        // Extract x, y, z values from the JSON
        float x = (float)obj["x"];
        float y = (float)obj["y"];
        float z = (float)obj["z"];

        // Return a new Vector3 object
        return new Vector3(x, y, z);
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        // Begin writing the JSON object
        writer.WriteStartObject();

        // Write each component of the vector (x, y, z)
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);

        writer.WritePropertyName("y");
        writer.WriteValue(value.y);

        writer.WritePropertyName("z");
        writer.WriteValue(value.z);

        // End writing the JSON object
        writer.WriteEndObject();
    }
}
