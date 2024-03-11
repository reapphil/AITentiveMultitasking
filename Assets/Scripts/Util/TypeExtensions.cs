using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class TypeExtensions
{
    public static Type GetLowestBaseTypeInHierarchyOf(this Type type, Type baseType)
    {
        Type searchedType = type;

        while (searchedType.BaseType.GetInterfaces().Contains(baseType) || searchedType.BaseType.IsSubclassOf(baseType))
        {
            searchedType = searchedType.BaseType;
        }

        return searchedType;
    }

    public static MemberInfo[] GetAttributes(this Type type, BindingFlags bindingFlags)
    {
        return type.GetFields(bindingFlags).Cast<MemberInfo>()
            .Concat(type.GetProperties(bindingFlags)).ToArray();
    }
}
