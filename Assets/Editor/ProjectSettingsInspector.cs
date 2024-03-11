using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Unity.MLAgents;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;

[CustomEditor(typeof(ProjectSettings))]
public class ProjectSettingsInspector : Editor
{
    private static Dictionary<Type, bool> _isUnfolded = new();

    private ProjectSettings _projectSettings;


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AddProjectAssignFieldsToIspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate File Name"))
        {
            _projectSettings.GenerateFilename();
        }

        if (GUILayout.Button("Load Experiment Setting"))
        {
            string path = Application.dataPath;
            path = EditorUtility.OpenFilePanel("Supervisor Model Selection", Path.Combine(Directory.GetParent(path).ToString(), "config", "experiment_config"), "json");

            if (path == "")
            {
                return;
            }

            LoadProjectSettings(path, _projectSettings);

            Debug.Log("Experiment settings loaded!");
        }

        if (Event.current.type == EventType.Repaint && !Application.isPlaying)
        {
            Validator.ValidateProjectSettings(_projectSettings);
            _projectSettings.UpdateSettings();

            PersistProgrammaticChanges(_projectSettings);
        }
    }


    private void OnEnable()
    {
        _projectSettings = (ProjectSettings)target;
    }

    private void AddProjectAssignFieldsToIspector()
    {
        List<(Component, FieldInfo)> fields = _projectSettings.GetProjectAssignFieldsForSupervisor();
        fields = fields.Concat(_projectSettings.GetProjectAssignFieldsForFocusAgent()).Concat(_projectSettings.GetProjectAssignFieldsForTasks()).ToList();

        InitFoldOut(fields);

        Component previousComponent = null;

        foreach ((Component, FieldInfo) entry in fields)
        {
            if(previousComponent != entry.Item1)
            {
                AddFoldOutToInspector(entry.Item1);
                previousComponent = entry.Item1;
            }

            if (_isUnfolded[entry.Item1.GetType()])
            {
                AddHeaderToInspector(entry.Item2);

                ProjectAssignAttribute projectAssignAttribute = (ProjectAssignAttribute)Attribute.GetCustomAttribute(entry.Item2, typeof(ProjectAssignAttribute));
                
                if (!projectAssignAttribute.Hide)
                {
                    AddFieldToInspector(entry);
                }
            }
        }
    }

    private void InitFoldOut(List<(Component, FieldInfo)> fields)
    {
        List<Type> types = fields.Select(x => x.Item1.GetType()).Distinct().ToList();

        foreach (Type type in types)
        {
            if (!_isUnfolded.ContainsKey(type))
            {
                _isUnfolded[type] = false;
            }
        }
    }

    private void AddFoldOutToInspector(Component component)
    {
        _isUnfolded[component.GetType()] = EditorGUILayout.Foldout(_isUnfolded[component.GetType()], component.GetType().Name, EditorStyles.foldoutHeader);
    }

    private void AddHeaderToInspector(FieldInfo field)
    {
        ProjectAssignAttribute projectAssignAttribute = (ProjectAssignAttribute)Attribute.GetCustomAttribute(field, typeof(ProjectAssignAttribute));

        if (projectAssignAttribute.Header != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(projectAssignAttribute.Header, EditorStyles.boldLabel);
        }
    }

    private void AddFieldToInspector((Component, FieldInfo) entry)
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

    private void LoadProjectSettings(string paths, ProjectSettings projectSettings)
    {
        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(paths);

        Validator.ValidateExperimentSettings(settings);

        SceneManagement.ProjectSettings = projectSettings;
        SceneManagement.ConfigScene(settings);
    }

    //Unity does not realize the object has been changed and does not properly re-serialize it. When an object is edited in the editor, the object
    //instance is not actually edited, but the serialized data instead. When objects are directly changed, Unity might just discard it and use its
    //old serialized data. Calling EditorUtility.SetDirty on the object let Unity know it was changed. Alternatively, the SerializedObject API could
    //be used to edit the object, which is what the inspector is using. This is in this case problematic since BehaviorParameters use getter and
    //setters for its fields which cannot be serialized.
    private void PersistProgrammaticChanges(ProjectSettings projectSettings)
    {
        Agent[] agents = projectSettings.Agents;
        Supervisor.SupervisorAgent supervisorAgent2 = projectSettings.SupervisorAgent;

        for (int i = 0; i < agents.Length; i++)
        {
            EditorUtility.SetDirty(agents[i]);
            EditorUtility.SetDirty(agents[i].GetComponent<Unity.MLAgents.Policies.BehaviorParameters>());
        }
        EditorUtility.SetDirty(supervisorAgent2);
    }
}
