using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.MLAgents;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;


[CustomEditor(typeof(ProjectSettings))]
public class ProjectSettingsInspector : Editor
{
    private static Dictionary<Type, bool> _isUnfolded = new();

    private ProjectSettings _projectSettings;

    private List<(Component, FieldInfo)> _fields;


    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying) 
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                //only reload fields if e.g. the task game objects have changed
                ReloadFields();
            }

            AddProjectAssignFieldsToIspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate File Name"))
            {
                _projectSettings.GenerateFilename();
            }

            if (GUILayout.Button("Load Experiment Setting"))
            {
                string path = Application.dataPath;
                path = EditorUtility.OpenFilePanel("Experiment Setting Selection", Path.Combine(Directory.GetParent(path).ToString(), "config", "experiment_config"), "json");

                if (path == "")
                {
                    return;
                }

                LoadProjectSettings(path, _projectSettings);
                ReloadFields();

                Debug.Log("Experiment settings loaded!");
            }
        }
        else
        {
            EditorGUILayout.LabelField("Project Settings cannot be changed during play mode.", EditorStyles.boldLabel);
        }

        if(GUI.changed)
        {
            SynchronizeInputsWithTasksGameObjects();
            Validator.ValidateProjectSettings(_projectSettings);
            _projectSettings.UpdateSettings();

            PersistProgrammaticChanges(_projectSettings);
        }
    }


    private void OnEnable()
    {
        _projectSettings = (ProjectSettings)target;

        ReloadFields();
    }

    private void ReloadFields()
    {
        _fields = _projectSettings.GetProjectAssignFieldsForSupervisor();
        _fields = _fields.Concat(_projectSettings.GetProjectAssignFieldsForFocusAgent()).Concat(_projectSettings.GetProjectAssignFieldsForTasks()).ToList();

        InitFoldOut(_fields);
    }

    private void AddProjectAssignFieldsToIspector()
    {
        Component previousComponent = null;

        foreach ((Component, FieldInfo) entry in _fields)
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

    private Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();

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
            var type when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) => DrawReorderableList(entry.Item1, entry.Item2, type),
            _ => EditorGUILayout.ObjectField(label: content, obj: (UnityEngine.Object)entry.Item2.GetValue(entry.Item1), entry.Item2.FieldType, allowSceneObjects: true),
        };

        if (!(entry.Item2.FieldType.IsGenericType && entry.Item2.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
        {
            entry.Item2.SetValue(entry.Item1, value);
        }
    }

    private object DrawReorderableList(Component component, FieldInfo fieldInfo, Type fieldType)
    {
        string key = component.GetInstanceID() + "." + fieldInfo.Name;
        if (!reorderableLists.ContainsKey(key))
        {
            IList list = (IList)fieldInfo.GetValue(component);
            Type elementType = fieldType.GetGenericArguments()[0];

            ReorderableList reorderableList = new ReorderableList(list, elementType, true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    rect.xMin += 10; // Adjust for the foldout arrow space
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width - 40, rect.height);
                    EditorGUI.LabelField(labelRect, Util.FormatFieldName(fieldInfo.Name));

                    Rect countRect = new Rect(rect.x + rect.width - 40, rect.y, 40, rect.height);
                    EditorGUI.LabelField(countRect, list.Count.ToString(), EditorStyles.label);
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    Rect labelRect = new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(labelRect, $"Element {index}");

                    Rect fieldRect = new Rect(rect.x + 60, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight);
                    object element = list[index];
                    element = DrawElementField(fieldRect, element, elementType);
                    list[index] = element;
                },
                elementHeightCallback = (int index) =>
                {
                    return EditorGUIUtility.singleLineHeight + 2;
                },
                onAddCallback = (ReorderableList l) =>
                {
                    list.Add(Activator.CreateInstance(elementType));
                },
                onRemoveCallback = (ReorderableList l) =>
                {
                    list.RemoveAt(l.index);
                }
            };

            reorderableLists[key] = reorderableList;
        }

        reorderableLists[key].DoLayoutList();
        return fieldInfo.GetValue(component);
    }

    private object DrawElementField(Rect rect, object element, Type elementType)
    {
        if (elementType == typeof(int))
        {
            return EditorGUI.IntField(rect, element is int @int ? @int : 0);
        }
        if (elementType == typeof(string))
        {
            return EditorGUI.TextField(rect, element as string ?? "");
        }
        if (elementType == typeof(float))
        {
            return EditorGUI.FloatField(rect, element is float @flt ? @flt : 0f);
        }
        if (elementType == typeof(double))
        {
            return EditorGUI.DoubleField(rect, element is double @double ? @double : 0f);
        }
        if (elementType == typeof(bool))
        {
            return EditorGUI.Toggle(rect, element is bool @bool ? @bool : false);
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
        {
            return EditorGUI.ObjectField(rect, element as UnityEngine.Object, elementType, allowSceneObjects: true);
        }

        EditorGUI.LabelField(rect, $"Unsupported type {elementType}");
        return element;
    }

    private void LoadProjectSettings(string paths, ProjectSettings projectSettings)
    {
        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(paths);

        Validator.ValidateExperimentSettings(settings);

        SceneManagement.ProjectSettings = projectSettings;
        SceneManagement.ConfigScene(settings);
    }

    private void SynchronizeInputsWithTasksGameObjects()
    {
        while (_projectSettings.Inputs.Count > _projectSettings.TasksGameObjects.Length)
        {
            _projectSettings.Inputs.RemoveAt(_projectSettings.Inputs.Count - 1);
        }

        for (int i = 0; i < _projectSettings.TasksGameObjects.Length; i++)
        {
            if(_projectSettings.Inputs.Count <= i)
            {
                InputActionAsset inputActions = _projectSettings.TasksGameObjects[i].transform.GetChildByName("Agent").GetComponent<PlayerInput>().actions;

                _projectSettings.Inputs.Add(inputActions);
            }
        }
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
