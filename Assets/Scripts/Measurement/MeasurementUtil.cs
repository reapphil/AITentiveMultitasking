using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeasurementUtil
{
    public static Tuple<int, int> GetTuple(int a, int b)
    {
        return new Tuple<int, int>(Math.Min(a, b), Math.Max(a, b));
    }

    public static string GetTupleName(Tuple<int, int> tuple, ITask[] tasks)
    {
        string n1 = Util.ShortenString(tasks[tuple.Item1].GetType().Name);
        string n2 = Util.ShortenString(tasks[tuple.Item2].GetType().Name);

        if (tasks[tuple.Item1].GetType().Equals(tasks[tuple.Item2].GetType()))
        {
            return n1;
        }

        return string.Format("{0}_{1}", n1, n2);
    }

    public static string GetMeasurementName(Type t1, Type t2)
    {
        string n1 = Util.ShortenString(t1.Name);
        string n2 = Util.ShortenString(t2.Name);

        if (t1.Equals(t2))
        {
            return n1;
        }

        return n1[0] < n2[0] ? $"{n1}_{n2}" : $"{n2}_{n1}";
    }

    public static (Type, Type) GetMeasurementTuple(Type t1, Type t2)
    {
        return t1.Name[0] < t2.Name[0] ? (t1, t2) : (t2, t1);
    }
}
