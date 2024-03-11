using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    public static T GetBaseComponent<T>(this GameObject gameObject)
    {
        return (T)(object)gameObject.GetBaseComponent(typeof(T));
    }

    public static Component GetBaseComponent(this GameObject gameObject, Type t)
    {
        Component[] components = gameObject.GetComponents(t);

        foreach (Component component in components)
        {
            if (!component.GetType().IsSubclassOf(t))
            {
                return component;
            }
        }

        return null;
    }
}
