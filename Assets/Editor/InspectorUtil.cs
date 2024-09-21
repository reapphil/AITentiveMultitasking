using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class InspectorUtil
{
    public static void AddFieldToInspector((Component, FieldInfo) entry)
    {
        TooltipAttribute tooltipAttribute = (TooltipAttribute)Attribute.GetCustomAttribute(entry.Item2, typeof(TooltipAttribute));

        GUIContent content = new GUIContent(Util.FormatFieldName(entry.Item2.Name), tooltipAttribute != null ? tooltipAttribute.tooltip : "");

        object value = entry.Item2.FieldType switch
        {
            var type when type == typeof(int) => EditorGUILayout.IntField(label: content, entry.Item2.GetValue(entry.Item1) is int @int ? @int : 0),
            var type when type == typeof(string) => EditorGUILayout.TextField(label: content, entry.Item2.GetValue(entry.Item1) is string @str ? @str : ""),
            var type when type == typeof(float) => EditorGUILayout.FloatField(label: content, entry.Item2.GetValue(entry.Item1) is float @flt ? @flt : 0f),
            var type when type == typeof(double) => EditorGUILayout.DoubleField(label: content, entry.Item2.GetValue(entry.Item1) is double @double ? @double : 0f),
            var type when type == typeof(bool) => EditorGUILayout.Toggle(label: content, entry.Item2.GetValue(entry.Item1) is bool @bool ? @bool : false),
            _ => EditorGUILayout.ObjectField(label: content, obj: (UnityEngine.Object)entry.Item2.GetValue(entry.Item1), entry.Item2.FieldType, allowSceneObjects: true),
        };

        entry.Item2.SetValue(entry.Item1, value);
    }
}
