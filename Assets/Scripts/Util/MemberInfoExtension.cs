using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

public static class MemberInfoExtension
{
    public static object GetValue(this MemberInfo memberInfo, object forObject)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Field:
                return ((FieldInfo)memberInfo).GetValue(forObject);
            case MemberTypes.Property:
                return ((PropertyInfo)memberInfo).GetValue(forObject);
            default:
                //Debug.LogWarning(string.Format("Member type {0} is not implemented.", memberInfo.MemberType));
                return null;
        }
    }

    public static void SetValue(this MemberInfo memberInfo, object obj, object value)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Field:
                ((FieldInfo)memberInfo).SetValue(obj, value);
                break;
            case MemberTypes.Property:
                ((PropertyInfo)memberInfo).SetValue(obj, value);
                break;
            default:
                //Debug.LogWarning(string.Format("Member type {0} is not implemented.", memberInfo.MemberType));
                break;
        }
    }

    public static Type GetUnderlyingType(this MemberInfo memberInfo)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Event:
                return ((EventInfo)memberInfo).EventHandlerType;
            case MemberTypes.Field:
                return ((FieldInfo)memberInfo).FieldType;
            case MemberTypes.Method:
                return ((MethodInfo)memberInfo).ReturnType;
            case MemberTypes.Property:
                return ((PropertyInfo)memberInfo).PropertyType;
            default:
                //Debug.LogWarning(string.Format("Member type {0} is not implemented.", memberInfo.MemberType));
                return null;
        }
    }
}
