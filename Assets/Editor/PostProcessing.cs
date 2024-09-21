using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NUnit.Framework;
using System;
using Unity.Barracuda;
using UnityEditor;
using Unity.MLAgents;
using System.Linq;

public class PostProcessing
{

    /// <summary>
    /// The base type of the agents must have the same name as the bahavior name. Use an abstract class (see e.g. Ballagent) or the optimal model 
    /// (see e.g. DrivingAgent) as the base type.
    /// </summary>
    public static void EnrichModels()
    {
        List<string> args = APIHelper.GetArgs();
        string confFile = args[0];
        string modelPath = Path.GetDirectoryName(args[0]);

        Debug.Log(string.Format("Enriching models for config file {0}.", confFile));

        string[] parts = confFile.Split('.');
        Assert.AreEqual("json", parts[parts.Length - 1]);

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(confFile);

        CreateAITentiveModel(modelPath, settings, "SupervisorAgent", "AUI");
        CreateAITentiveModel(modelPath, settings, "FocusAgent");
        EnrichTaskModels(modelPath, settings);
    }

    private static void EnrichTaskModels(string modelPath, Dictionary<Type, ISettings> settings)
    {
        Dictionary<string, string> taskModels = ((Hyperparameters)settings[typeof(Hyperparameters)]).taskModels != null ? ((Hyperparameters)settings[typeof(Hyperparameters)]).taskModels : new();
        List<string> loadedModels = new();

        foreach (string task in ((Hyperparameters)settings[typeof(Hyperparameters)]).tasks)
        {
            Debug.Log(string.Format("Enriching model for task {0}.", task));

            string baseType = Util.GetType(task).GetLowestBaseTypeInHierarchyOf(typeof(Agent)).Name;

            if (!taskModels.ContainsKey(baseType) && !loadedModels.Contains(baseType))
            {
                CreateAITentiveModel(modelPath, settings, baseType);

                loadedModels.Add(baseType);
            }
        }
    }

    private static void CreateAITentiveModel(string modelPath, Dictionary<Type, ISettings> settings, string baseType, string onnxFile = null)
    {
        AITentiveModel aITentiveModel = ScriptableObject.CreateInstance<AITentiveModel>();
        string newPath = "";

        try
        {
            newPath = onnxFile != null ? Rename(modelPath, onnxFile) : Rename(modelPath, baseType);
        }
        catch
        {
            Debug.Log(string.Format("No model found for {0}. Abort model creation.", baseType));
            return;
        }

        AssetDatabase.ImportAsset(newPath);
        NNModel asset = AssetDatabase.LoadAssetAtPath(newPath, typeof(NNModel)) as NNModel;

        aITentiveModel.Model = asset;
        aITentiveModel.Type = baseType;
        aITentiveModel.DecisionPeriod = GetAgentSettings(baseType, settings) != null ? GetAgentSettings(baseType, settings).decisionPeriod.GetValueOrDefault() : 0;

        if (settings.ContainsKey(typeof(SupervisorSettings)))
        {
            aITentiveModel.SupervisorSettings = (SupervisorSettings)settings[typeof(SupervisorSettings)];
        }

        AssetDatabase.CreateAsset(aITentiveModel, string.Format("{0}.asset", onnxFile != null ? GetModelPath(modelPath, onnxFile) : GetModelPath(modelPath, baseType)));
        AssetDatabase.SaveAssets();
    }

    private static string Rename(string modelPath, string baseType)
    {
        string modelName = string.Format("{0}{1}.onnx", baseType, Path.GetFileName(modelPath));

        string oldPath = Path.Combine(modelPath, baseType + ".onnx");
        string newPath = Path.Combine(modelPath, modelName);

        File.Move(oldPath, newPath);

        return newPath;
    }

    private static string GetModelPath(string modelPath, string baseType)
    {
        string modelName = string.Format("{0}{1}", baseType, Path.GetFileName(modelPath));

        return Path.Combine(modelPath, modelName);
    }

    private static IAgentSettings GetAgentSettings(string baseType, Dictionary<Type, ISettings> settings)
    {
        foreach (var kvp in settings)
        {
            if (kvp.Value.GetType().GetInterfaces().Contains(typeof(IAgentSettings)))
            {
                IAgentSettings taskSettings = (IAgentSettings)kvp.Value;

                if (taskSettings.baseClassName == baseType)
                {
                    return taskSettings;
                }
            }
        }

        return null;
    }
}
