using System.IO;
using Unity.MLAgents;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public static class SceneManagement
{
    public static IProjectSettings ProjectSettings { get; set; }

    public static string BackUpScene()
    {
        string backupPath = Path.Combine(UnityEngine.Application.dataPath, "Scenes", "SupervisorML_Backup.unity");

        File.Copy(Path.Combine(UnityEngine.Application.dataPath, "Scenes", "SupervisorML.unity"), backupPath, true);

        return backupPath;
    }

    public static void RestoreScene(string scenePath)
    {
        string path = Path.Combine("Assets", "Scenes", "SupervisorML.unity");
        var backupScene = EditorSceneManager.OpenScene(scenePath);
        EditorSceneManager.SaveScene(backupScene, path);
        EditorSceneManager.CloseScene(backupScene, true);
    }

    public static Scene GetScene()
    {
        return EditorSceneManager.OpenScene(Path.Combine(UnityEngine.Application.dataPath, "Scenes", "SupervisorML.unity"));
    }

    //NOTE: Not valid combinations (e.g. NumberOfTimeBins > 1 and supervisor is not random agent) are corrected in the projectSettings class and therefore, must not be considered here.
    public static string ConfigSceneWithBackUp(Dictionary<Type, ISettings> settings)
    {
        string backupPath = BackUpScene();
        var currentScene = EditorSceneManager.OpenScene(Path.Combine("Assets", "Scenes", "SupervisorML.unity"));
        LoadProjectSettings(currentScene);

        ConfigScene(settings);

        EditorSceneManager.SaveScene(currentScene);
        EditorSceneManager.CloseScene(currentScene, true);

        return backupPath;
    }

    public static void ConfigScene(Dictionary<Type, ISettings> settings)
    {
        ProjectSettings.Mode = Mode.DefaultMode;

        AssignProjectSettings(settings);

        LoadModelsInProjectSettings(settings);

        ProjectSettings.UpdateSettings();

        PersistProgrammaticChanges(ProjectSettings);
    }

    public static void ConfigIsRawDataCollected(bool isRawDataCollected)
    {
        var currentScene = EditorSceneManager.OpenScene(Path.Combine("Assets", "Scenes", "SupervisorML.unity"));
        ProjectSettings = global::ProjectSettings.GetProjectSettings(currentScene);

        ProjectSettings.GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().IsRawDataCollected = isRawDataCollected;

        ProjectSettings.UpdateSettings();

        PersistProgrammaticChanges(ProjectSettings);

        EditorSceneManager.SaveScene(currentScene);
        EditorSceneManager.CloseScene(currentScene, true);
    }


    private static void AssignProjectSettings(Dictionary<Type, ISettings> settings)
    {
        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        if (!Util.HasDefaultValuesForFields(hyperparameters)) { ProjectSettingsMapper.HyperparametersToProjectSettings(settings, ProjectSettings); }

        if (settings.ContainsKey(typeof(ExperimentSettings)))
        {
            ExperimentSettings experimentSettings = settings[typeof(ExperimentSettings)] as ExperimentSettings;
            ProjectSettingsMapper.ExperimentSettingsToProjectSettings(experimentSettings, ProjectSettings);
        }
        if (settings.ContainsKey(typeof(PerformanceMeasurementSettings)))
        {
            PerformanceMeasurementSettings performanceMeasurementSettings = settings[typeof(PerformanceMeasurementSettings)] as PerformanceMeasurementSettings;
            if (!Util.HasDefaultValuesForFields(performanceMeasurementSettings)) { ProjectSettingsMapper.PerformanceMeasurementSettingsToProjectSettings(performanceMeasurementSettings, ProjectSettings); }
        }
        if (settings.ContainsKey(typeof(SupervisorSettings)))
        {
            SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
            if (!Util.HasDefaultValuesForFields(supervisorSettings)) { ProjectSettingsMapper.SupervisorSettingsToProjectSettings(supervisorSettings, ProjectSettings); }
        }
        if (settings.ContainsKey(typeof(BehavioralDataCollectionSettings)))
        {
            BehavioralDataCollectionSettings behavioralDataCollectionSettings = settings[typeof(BehavioralDataCollectionSettings)] as BehavioralDataCollectionSettings;
            if (!Util.HasDefaultValuesForFields(behavioralDataCollectionSettings)) { ProjectSettingsMapper.BehavioralDataCollectionSettingsToProjectSettings(behavioralDataCollectionSettings, ProjectSettings); }
        }
        if (settings.ContainsKey(typeof(Ball3DAgentHumanCognitionSettings)))
        {
            Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings = settings[typeof(Ball3DAgentHumanCognitionSettings)] as Ball3DAgentHumanCognitionSettings;
            if (hyperparameters.tasks.Contains("Ball3DAgentHumanCognition") || hyperparameters.tasks.Contains("Ball3DAgentHumanCognitionSingleProbabilityDistribution") && !Util.HasDefaultValuesForFields(ball3DAgentHumanCognitionSettings))
            {
                ProjectSettingsMapper.Ball3DAgentHumanCognitionSettingsToProjectSettings(ball3DAgentHumanCognitionSettings, ProjectSettings);
            }
        }
        if (settings.ContainsKey(typeof(BalancingTaskSettings)))
        {
            BalancingTaskSettings balancingTaskSettings = settings[typeof(BalancingTaskSettings)] as BalancingTaskSettings;
            if (!Util.HasDefaultValuesForFields(balancingTaskSettings)) { ProjectSettingsMapper.BalancingTaskSettingsToProjectSettings(balancingTaskSettings, ProjectSettings); }
        }
    }

    private static void LoadModelsInProjectSettings(Dictionary<Type, ISettings> settings)
    {
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        Dictionary <string, string> models = hyperparameters.taskModels;

        if (supervisorSettings.randomSupervisor)
        {
            string dummyModel = "AUI.asset";
            models[typeof(Supervisor.SupervisorAgent).FullName] = dummyModel;
        }
        else if (hyperparameters.supervisorModelName != null && hyperparameters.supervisorModelName != "")
        {
            models[typeof(Supervisor.SupervisorAgent).FullName] = hyperparameters.supervisorModelName;
        }

        if (hyperparameters.useFocusAgent)
        {
            models[typeof(FocusAgent).Name] = hyperparameters.focusAgentModelName;
        }

        AddModelsToProjectsettings(models);
    }

    private static void AddModelsToProjectsettings(Dictionary<string, string> models)
    {
        ProjectSettings.AITentiveModels = new();

        foreach (var kvp in models)
        {
            if(kvp.Value != null && kvp.Value != "")
            {
                ProjectSettings.AITentiveModels = ProjectSettings.AITentiveModels.Concat(GetTaskModels(Path.Combine("Assets", "Models", kvp.Value), Util.GetType(kvp.Key))).ToList();
            }
            
        }
    }

    private static List<AITentiveModel> GetTaskModels(string path, Type type)
    {
        List<AITentiveModel> taskModels = new();

        if (File.GetAttributes(@path).HasFlag(FileAttributes.Directory))
        {
            var d = new DirectoryInfo(@path);
            foreach (FileInfo fi in d.GetFiles("*", SearchOption.AllDirectories))
            {
                AITentiveModel taskModel = (AITentiveModel)AssetDatabase.LoadAssetAtPath(fi.FullName.Substring(fi.FullName.IndexOf("Assets")), typeof(AITentiveModel));

                if (taskModel != null && taskModel.GetType() == type)
                {
                    taskModels.Add(taskModel);
                }
            }
        }
        else
        {
            if(Path.GetExtension(path) != ".asset")
            {
                Debug.LogError(string.Format("The file {0} is not a valid model file.", path));
            }
            else
            {
                taskModels.Add((AITentiveModel)AssetDatabase.LoadAssetAtPath(path, typeof(AITentiveModel)));
            }
            
        }

        return taskModels;
    }

    private static void LoadProjectSettings(Scene currentScene)
    {
        //"projectSettings is null" leads to problems (e.g. after BuildScriptTests.TrainingEnvironmentExecutionTest), see here:
        //https://stackoverflow.com/a/72072517/11986067 or https://forum.unity.com/threads/different-types-of-null.559879/
        if (ProjectSettings == null || object.ReferenceEquals(ProjectSettings, null) || (object)ProjectSettings == null || ProjectSettings is null || ProjectSettings.Equals(null))
        {
            ProjectSettings = global::ProjectSettings.GetProjectSettings(currentScene);
        }
    }   

    //Unity does not realize that the object has been changed and does not properly re-serialize it. When an object is edited in the editor, the object
    //instance is not actually edited, but the serialized data instead. When objects are directly changed, Unity might just discard it and use its
    //old serialized data. Calling EditorUtility.SetDirty on the object let Unity know it was changed. Alternatively, the SerializedObject API could
    //be used to edit the object, which is what the inspector is using. This is in this case problematic since BehaviorParameters use getter and
    //setters for its fields which cannot be serialized.
    private static void PersistProgrammaticChanges(IProjectSettings projectSettings)
    {
        Agent[] agents = projectSettings.Agents;
        Supervisor.SupervisorAgent supervisorAgent2 = projectSettings.SupervisorAgent;

        for (int i = 0; i < agents.Length; i++)
        {
            EditorUtility.SetDirty(agents[i]);
            EditorUtility.SetDirty(agents[i].GetComponent<Unity.MLAgents.Policies.BehaviorParameters>());
        }

        if (supervisorAgent2 is not null)
        {
            EditorUtility.SetDirty(supervisorAgent2);
        }
    }
}
