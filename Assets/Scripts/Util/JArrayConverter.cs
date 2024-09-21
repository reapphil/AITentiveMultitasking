using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

public class JArrayConverter
{
    public static Array ConvertToSystemArray(JArray jArray, Type elementType, int[] dimensions)
    {
        if (dimensions.Length != GetArrayDepth(jArray))
        {
            throw new ArgumentException(string.Format("The number of dimensions ({0}) does not match the depth of the JArray ({1}).", dimensions.Length, GetArrayDepth(jArray)));
        }

        Array array = Array.CreateInstance(elementType, dimensions);

        CopyJArrayToSystemArray(jArray, array, 0, new int[dimensions.Length]);

        return array;
    }

    private static void CopyJArrayToSystemArray(JArray jArray, Array systemArray, int level, int[] indices)
    {
        for (int i = 0; i < jArray.Count; i++)
        {
            if (jArray[i] is JArray innerArray)
            {
                int[] updatedIndices = new int[indices.Length];
                indices.CopyTo(updatedIndices, 0);
                updatedIndices[level] = i;

                CopyJArrayToSystemArray(innerArray, systemArray, level + 1, updatedIndices);
            }
            else
            {
                indices[level] = i;
                systemArray.SetValue(jArray[i].ToObject(systemArray.GetType().GetElementType()), indices);
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
}
