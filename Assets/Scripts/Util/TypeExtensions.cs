using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class TypeExtensions
{
    public static IEnumerable<Type> GetParentTypes(this Type type)
    {
        // is there any base type?
        if (type == null)
        {
            yield break;
        }

        // return all implemented or inherited interfaces
        foreach (var i in type.GetInterfaces())
        {
            yield return i;
        }

        // return all inherited types
        var currentBaseType = type.BaseType;
        while (currentBaseType != null)
        {
            yield return currentBaseType;
            currentBaseType = currentBaseType.BaseType;
        }
    }

    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(String)) return true;
        return (type.IsValueType & type.IsPrimitive);
    }

    public static Type GetLowestBaseTypeInHierarchyOf(this Type type, Type baseType)
    {
        Type searchedType = type;

        while (searchedType.BaseType.GetInterfaces().Contains(baseType) || searchedType.BaseType.IsSubclassOf(baseType))
        {
            searchedType = searchedType.BaseType;
        }

        return searchedType;
    }

    public static MemberInfo[] GetMemberInfos(this Type type, BindingFlags bindingFlags)
    {
        return type.GetFields(bindingFlags).Cast<MemberInfo>()
            .Concat(type.GetProperties(bindingFlags)).ToArray();
    }

    public static FieldInfo GetBackingField(this Type type, string name, BindingFlags bindingFlags = BindingFlags.Default)
    {
        if (type.GetField(name) != null)
        {
            return type.GetField(name);
        }

        return type.GetField($"<{name}>k__BackingField", bindingFlags);
    }

    public static FieldInfo GetBackingFieldInHierarchy(this Type type, string name, BindingFlags bindingFlags = BindingFlags.Default)
    {
        List<Type> types = type.GetParentTypes().ToList();
        types.Add(type);

        foreach (Type t in types)
        {
            FieldInfo field = t.GetBackingField(name, bindingFlags);
            if (field != null)
            {
                return field;
            }
        }

        return null;
    }
}
