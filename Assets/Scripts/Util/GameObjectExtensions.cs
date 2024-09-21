using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public static List<Component> GetComponentsInHierarchy<T>(this GameObject gameObject)
    {
        return GetComponentsInHierarchy(gameObject, typeof(T));
    }

    public static List<Component> GetComponentsInHierarchy(this GameObject gameObject, Type t)
    {
        List<Component> components = gameObject.GetComponents(t).ToList();

        foreach (Transform child in gameObject.transform)
        {
            GetComponentsInHierarchy(child.gameObject, t, components);
        }

        return components;
    }


    private static void GetComponentsInHierarchy(this GameObject gameObject, Type t, List<Component> components)
    {
        components.AddRange(gameObject.GetComponents(t));

        foreach (Transform child in gameObject.transform)
        {
            GetComponentsInHierarchy(child.gameObject, t, components);
        }
    }
}
