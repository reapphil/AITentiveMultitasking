using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

public class JArrayConverter
{
    public static Array ConvertToSystemArray<T>(JArray jArray, Type elementType, int[] dimensions)
    {
        if (dimensions.Length != GetArrayDepth(jArray))
        {
            throw new ArgumentException(string.Format("The number of dimensions ({0}) does not match the depth of the JArray ({1}).", dimensions.Length, GetArrayDepth(jArray)));
        }

        Array array = Array.CreateInstance(typeof(Dictionary<int, (int, T)>), dimensions);

        CopyJArrayToSystemArray<T>(jArray, array, 0, new int[dimensions.Length], elementType);

        return array;
    }

    private static void CopyJArrayToSystemArray<T>(JArray jArray, Array systemArray, int level, int[] indices, Type elementType)
    {
        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new Vector2JsonConverter(), new Vector3JsonConverter() }
        });

        for (int i = 0; i < jArray.Count; i++)
        {
            if (jArray[i] is JArray innerArray)
            {
                int[] updatedIndices = new int[indices.Length];
                indices.CopyTo(updatedIndices, 0);
                updatedIndices[level] = i;

                CopyJArrayToSystemArray<T>(innerArray, systemArray, level + 1, updatedIndices, elementType);
            }
            else
            {
                Dictionary<int, (int, T)> convertedDict = ConvertDictionary<T>(jArray[i].ToObject(elementType));

                indices[level] = i;
                systemArray.SetValue(convertedDict, indices);
            }
        }
    }

    private static int GetArrayDepth(JArray jArray)
    {
        int depth = 0;
        JToken current = jArray;
        while (current is JArray)
        {
            depth++;
            current = ((JArray)current)[0];
        }
        return depth;
    }

    /// <summary>
    /// Data must be converted from the actual type to the object type because of the type variance in C# and the fact that tuples and generic 
    /// collections like List<T> are invariant, meaning that even though e.g. Unity.Vector3 and dynamic are convertible in some contexts, it cannot 
    /// be directly cast between generic types with different type parameters, such as List<(Unity.Vector3, Unity.Vector3)> and 
    /// List<(dynamic, dynamic)>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="inputDict"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Dictionary<int, (int, T)> ConvertDictionary<T>(dynamic inputDict)
    {
        var resultDict = new Dictionary<int, (int, T)>();

        foreach (var kvp in inputDict)
        {
            int key = kvp.Key;
            var intValue = kvp.Value.Item1;
            var data = kvp.Value.Item2;

            // Check if the data is a tuple of (int, double, double)
            if (data is ValueTuple<int, double, double> tupleData)
            {
                // No changes needed, add directly
                resultDict[key] = (intValue, (dynamic)tupleData);
            }
            // Check if the data is a List of tuples (any tuple type)
            else if (data is IList listData && listData.Count > 0 && IsTuple(listData[0]))
            {
                var objectList = new List<(dynamic, dynamic)>();

                foreach (var item in listData)
                {
                    // Use reflection to access tuple components dynamically
                    var itemType = item.GetType();
                    if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
                    {
                        var first = itemType.GetField("Item1").GetValue(item);
                        var second = itemType.GetField("Item2").GetValue(item);
                        objectList.Add((first, second));
                    }
                }

                resultDict[key] = (intValue, (dynamic)objectList);
            }
            else
            {
                throw new InvalidOperationException("Unexpected data type in dictionary.");
            }
        }

        return resultDict;
    }

    private static bool IsTuple(object obj)
    {
        if (obj == null) return false;

        var type = obj.GetType();
        return type.IsGenericType && type.FullName.StartsWith("System.ValueTuple");
    }
}
