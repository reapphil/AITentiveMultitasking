using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TransformExtension
{
    public static Transform GetChildByName(this Transform transform, string name)
    {
        foreach (Transform child in transform)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    public static Transform GetChildInHierarchyByName(this Transform transform, string name)
    {
        Transform result = null;

        foreach (Transform child in transform)
        {
            if (child.name == name)
            {
                return child;
            }
            else
            {
                result = GetChildInHierarchyByName(child.transform, name);
                
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    public static List<Transform> GetChildren(this Transform transform)
    {
        List<Transform> childs = new List<Transform>();

        return GetChildren(transform, childs);
    }

    private static List<Transform> GetChildren(Transform transform, List<Transform> childs)
    {
        foreach (Transform child in transform)
        {
            childs.Add(child);
            childs.Concat(GetChildren(child, childs));
        }

        return childs;
    }
}
