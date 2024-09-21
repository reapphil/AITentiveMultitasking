using PlasticGui.WorkspaceWindow.PendingChanges;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.MLAgents;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeasurementSettings))]
public class MeasurementSettingsInspector : Editor
{
    private MeasurementSettings _measurementSettings;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        _measurementSettings.ProjectSettings.SupervisorAgent.Tasks.DistinctBy(x => x.GetType()).ToList().ForEach(task =>
        {
            EditorGUILayout.LabelField(task.GetType().Name, EditorStyles.boldLabel);

            int fieldCount = 0;

            task.StateInformation.GetType().GetFields(flags).ToList().ForEach(field =>
            {
                ProjectAssignAttribute projectAssignAttribute = (ProjectAssignAttribute)Attribute.GetCustomAttribute(field, typeof(ProjectAssignAttribute));
                
                if (projectAssignAttribute != null)
                {
                    AddFieldToInspector(MeasurementSettings.Data, field, task.StateInformation.GetType());
                    fieldCount++;
                }
            });

            if (fieldCount == 0)
            {
                EditorGUILayout.LabelField("No fields to display.");
            }
        });

        if (GUI.changed)
        {
            PersistProgrammaticChanges(_measurementSettings);
        }
    }


    private void OnEnable()
    {
        _measurementSettings = (MeasurementSettings)target;
    }

    private void AddFieldToInspector(IDictionary<Type, ISettings> settingsDict, FieldInfo field, Type t)
    {
        TooltipAttribute tooltipAttribute = (TooltipAttribute)Attribute.GetCustomAttribute(field, typeof(TooltipAttribute));

        GUIContent content = new GUIContent(Util.FormatFieldName(field.Name), tooltipAttribute != null ? tooltipAttribute.tooltip : "");

        if (!settingsDict.ContainsKey(t))
        {
            settingsDict[t] = Activator.CreateInstance(t) as ISettings;
        }

        ISettings settings = settingsDict[t];

        object value = field.FieldType switch
        {
            var type when type == typeof(int) => EditorGUILayout.IntField(label: content, field.GetValue(settings) is int @int ? @int : 0),
            var type when type == typeof(string) => EditorGUILayout.TextField(label: content, field.GetValue(settings) is string @str ? @str : ""),
            var type when type == typeof(float) => EditorGUILayout.FloatField(label: content, field.GetValue(settings) is float @flt ? @flt : 0f),
            var type when type == typeof(double) => EditorGUILayout.DoubleField(label: content, field.GetValue(settings) is double @double ? @double : 0f),
            var type when type == typeof(bool) => EditorGUILayout.Toggle(label: content, field.GetValue(settings) is bool @bool ? @bool : false),
            _ => EditorGUILayout.ObjectField(label: content, obj: (UnityEngine.Object)field.GetValue(settings), field.FieldType, allowSceneObjects: true),
        };

        field.SetValue(settings, value);
    }

    private void PersistProgrammaticChanges(MeasurementSettings measurementSettings)
    {
        EditorUtility.SetDirty(measurementSettings);
        measurementSettings.WriteSettingsToDisk();
    }
}
