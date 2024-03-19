using System.Reflection;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
public class ShowIfAttributeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Calculate the property height, if we don't meet the condition and the draw 
        // Mode is DontDraw, then height will be 0.
        bool meetsCondition = MeetsConditions(property);
        var showIfAttribute = this.attribute as ShowIfAttribute;

        if (!meetsCondition && showIfAttribute.Action ==
                                       ActionOnConditionFail.DontDraw)
            return 0;
        return base.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool meetsCondition = MeetsConditions(property);
        // Early out, if conditions met, draw and go.
        if (meetsCondition)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        var showIfAttribute = this.attribute as ShowIfAttribute;
        if (showIfAttribute.Action == ActionOnConditionFail.DontDraw)
        {
            return;
        }
        else if (showIfAttribute.Action == ActionOnConditionFail.JustDisable)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }
    }


    #region Reflection helpers.
    private static MethodInfo GetMethod(object target, string methodName)
    {
        return GetAllMethods(target, m => m.Name.Equals(methodName,
                  StringComparison.InvariantCulture)).FirstOrDefault();
    }

    private static PropertyInfo GetProperty(object target, string propertyName)
    {
        return GetAllProperties(target, f => f.Name.Equals(propertyName,
              StringComparison.InvariantCulture)).FirstOrDefault();
    }

    private static IEnumerable<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate)
    {
        List<Type> types = new List<Type>()
            {
                target.GetType()
            };

        while (types.Last().BaseType != null)
        {
            types.Add(types.Last().BaseType);
        }

        for (int i = types.Count - 1; i >= 0; i--)
        {
            IEnumerable<PropertyInfo> propertyInfos = types[i].GetProperties(BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.NonPublic | 
                BindingFlags.Public | 
                BindingFlags.DeclaredOnly).Where(predicate);

            foreach (var propertyInfo in propertyInfos)
            {
                yield return propertyInfo;
            }
        }
    }

    private static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
    {
        IEnumerable<MethodInfo> methodInfos = target.GetType().GetMethods(BindingFlags.Instance | 
            BindingFlags.Static |
            BindingFlags.NonPublic | 
            BindingFlags.Public).Where(predicate);

        return methodInfos;
    }
    #endregion

    private bool MeetsConditions(SerializedProperty property)
    {
        var showIfAttribute = this.attribute as ShowIfAttribute;
        var target = property.serializedObject.targetObject;
        List<bool> conditionValues = new List<bool>();

        foreach (var condition in showIfAttribute.Conditions)
        {
            PropertyInfo conditionField = GetProperty(target, condition);
            if (conditionField != null &&
                conditionField.PropertyType == typeof(bool))
            {
                conditionValues.Add((bool)conditionField.GetValue(target));
            }

            MethodInfo conditionMethod = GetMethod(target, condition);
            if (conditionMethod != null &&
                conditionMethod.ReturnType == typeof(bool) &&
                conditionMethod.GetParameters().Length == 0)
            {
                conditionValues.Add((bool)conditionMethod.Invoke(target, null));
            }
        }

        if (conditionValues.Count > 0)
        {
            bool met;
            if (showIfAttribute.Operator == ConditionOperator.And)
            {
                met = true;
                foreach (var value in conditionValues)
                {
                    met = met && value;
                }
            }
            else
            {
                met = false;
                foreach (var value in conditionValues)
                {
                    met = met || value;
                }
            }
            return met;
        }
        else
        {
            Debug.LogError("Invalid boolean condition fields or methods used!");
            return true;
        }
    }
}
