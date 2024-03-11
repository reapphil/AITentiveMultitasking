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
    public static void EnrichModels()
    {
        List<string> args = APIHelper.GetArgs();
        string confFile = args[0];
        string modelPath = Path.GetDirectoryName(args[0]);

        string[] parts = confFile.Split('.');
        Assert.AreEqual("json", parts[parts.Length - 1]);

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(confFile);

        Dictionary<string, string> taskModels = ((Hyperparameters)settings[typeof(Hyperparameters)]).taskModels;

        List<string> loadedModels = new();

        foreach (string task in ((Hyperparameters)settings[typeof(Hyperparameters)]).tasks)
        {
            string baseType = Util.GetType(task).GetLowestBaseTypeInHierarchyOf(typeof(Agent)).Name;

            if (!taskModels.ContainsKey(baseType) && !loadedModels.Contains(baseType))
            {
                string newPath = Rename(modelPath, baseType);

                AITentiveModel aITentiveModel = ScriptableObject.CreateInstance<AITentiveModel>();

                AssetDatabase.ImportAsset(newPath);
                NNModel asset = AssetDatabase.LoadAssetAtPath(newPath, typeof(NNModel)) as NNModel;

                aITentiveModel.Model = asset;
                aITentiveModel.Type = baseType;
                aITentiveModel.DecisionPeriod = GetTaskSettings(baseType, settings) != null ? GetTaskSettings(baseType, settings).decisionPeriod : 0;
                aITentiveModel.SupervisorSettings = (SupervisorSettings)settings[typeof(SupervisorSettings)];


                AssetDatabase.CreateAsset(aITentiveModel, string.Format("{0}.asset", GetModelPath(modelPath, baseType)));
                AssetDatabase.SaveAssets();

                loadedModels.Add(baseType);
            }
        }
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

    private static ITaskSettings GetTaskSettings(string baseType, Dictionary<Type, ISettings> settings)
    {
        foreach (var kvp in settings)
        {
            if (kvp.Value.GetType().GetInterfaces().Contains(typeof(ITaskSettings)))
            {
                ITaskSettings taskSettings = (ITaskSettings)kvp.Value;

                if (taskSettings.baseClassName == baseType)
                {
                    return taskSettings;
                }
            }
        }

        return null;
    }
}
