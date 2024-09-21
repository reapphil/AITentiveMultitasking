using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    public static void RemoveFrom<T>(this List<T> lst, int from)
    {
        lst.RemoveRange(from, lst.Count - from);
    }
}
