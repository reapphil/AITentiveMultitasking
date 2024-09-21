using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ConfigVersioning
{
    public static List<ISettings> UnifySettings(string json, Type type)
    {
        Type baseType = type.GetLowestBaseTypeInHierarchyOf(typeof(ISettings));

        JsonSerializerSettings jsonSerializerSettings = new();
        jsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
        ISettings settings;

        try
        {
            settings = JsonConvert.DeserializeObject(json, baseType, jsonSerializerSettings) as ISettings;
        }
        catch (Exception)
        {
            Dictionary<string, string> members = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            settings = new JsonSettings(0, type, members);

            if (members.ContainsKey("decisionPeriod"))
            {
                settings = new JsonAgentSettings(0, type, members, int.Parse(members["decisionPeriod"]));
            }

            return JsonSettingsToSettings((JsonSettings)settings);
        }

        switch (settings.version)
        {
            case 0:
                return V0ToCurrent(json, baseType);
            case 2:
                return new() { JsonConvert.DeserializeObject(json, type) as ISettings };
            default:
                throw new NotImplementedException(String.Format("Version {0} of type {1} not implemented.", settings.version, type));
        }
    }


    private static List<ISettings> JsonSettingsToSettings(JsonSettings jsonSettings)
    {
        switch (jsonSettings.type)
        {
            case Type t when t == typeof(Supervisor.SupervisorAgent) || t.IsSubclassOf(typeof(Supervisor.SupervisorAgent)):
                return JsonSettingsToSupervisorSettings(jsonSettings);
            default:
                return new() { jsonSettings };
        }
    }

    private static List<ISettings> V0ToCurrent(string json, Type type)
    {
        switch (type)
        {
            case Type t when t == typeof(HyperparametersBase):
                return HyperparametersV0ToCurrent(json);
            case Type t when t == typeof(SupervisorSettingsBase):
                return SupervisorSettingsV0ToCurrent(json);
            case Type t when t == typeof(Ball3DAgentHumanCognitionSettings):
                return Ball3DAgentHumanCognitionSettingsV0ToCurrent(json);
            default:
                try
                {
                    return new() { JsonConvert.DeserializeObject(json, type) as ISettings };
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw new NotImplementedException(string.Format("V0 to current conversion not implemented for type {0}", type));
                }
        }
    }

    private static List<ISettings> SupervisorSettingsV0ToCurrent(string json)
    {
        SupervisorSettingsV0 supervisorSettingsV0 = JsonConvert.DeserializeObject<SupervisorSettingsV0>(json);

        SupervisorSettings supervisorSettings = SupervisorSettingsV0ToCurrent(supervisorSettingsV0);
        BalancingTaskSettings balancingTaskSettings = SupervisorSettingsV0ToBalancingTaskSettings(supervisorSettingsV0);

        return new() { supervisorSettings, balancingTaskSettings };
    }

    private static List<ISettings> Ball3DAgentHumanCognitionSettingsV0ToCurrent(string json)
    {
        Ball3DAgentHumanCognitionSettingsV0 ball3DAgentHumanCognitionSettingsV0 = JsonConvert.DeserializeObject<Ball3DAgentHumanCognitionSettingsV0>(json);

        Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings = new Ball3DAgentHumanCognitionSettings(ball3DAgentHumanCognitionSettingsV0);
        Hyperparameters hyperparameters = new Hyperparameters()
        {
            useFocusAgent = ball3DAgentHumanCognitionSettingsV0.useFocusAgent
        };

        return new() { ball3DAgentHumanCognitionSettings, hyperparameters };
    }

    private static List<ISettings> HyperparametersV0ToCurrent(string json)
    {
        HyperparametersV0 hyperparametersV0 = JsonConvert.DeserializeObject<HyperparametersV0>(json);

        Hyperparameters hyperparameters = new Hyperparameters(hyperparametersV0);

        hyperparameters.taskModels = new Dictionary<string, string>();
        hyperparameters.tasks = new string[hyperparametersV0.numberOfPlatforms.GetValueOrDefault()];

        for (int i = 0; i < hyperparametersV0.numberOfPlatforms; i++)
        {
            hyperparameters.tasks[i] = hyperparametersV0.agentChoice;
        }

        if (!hyperparametersV0.trainBallAgent.GetValueOrDefault())
        {
            hyperparameters.taskModels["BallAgent"] = hyperparametersV0.ballAgentModelName;
        }

        hyperparameters.focusActiveTask = hyperparametersV0.focusActivePlatform;
        hyperparameters.hideInactiveTasks = hyperparametersV0.hideInactivePlatform;

        BalancingTaskSettings balancingTaskSettings = new BalancingTaskSettings()
        {
            resetPlatformToIdentity = hyperparametersV0.resetPlatformToIdentity,
            decisionPeriod = hyperparametersV0.decisionPeriod
        };

        return new() { hyperparameters, balancingTaskSettings };
    }

    private static SupervisorSettings SupervisorSettingsV0ToCurrent(SupervisorSettingsBase supervisorSettings)
    {
        return new SupervisorSettings(supervisorSettings);
    }

    private static BalancingTaskSettings SupervisorSettingsV0ToBalancingTaskSettings(SupervisorSettingsV0 supervisorSettingsV0)
    {
        BalancingTaskSettings balancingTaskSettings = new BalancingTaskSettings()
        {
            globalDrag = supervisorSettingsV0.globalDrag,
            useNegativeDragDifficulty = supervisorSettingsV0.useNegativeDragDifficulty,
            ballAgentDifficulty = supervisorSettingsV0.ballAgentDifficulty,
            ballAgentDifficultyDivisionFactor = supervisorSettingsV0.ballAgentDifficultyDivisionFactor,
            ballStartingRadius = supervisorSettingsV0.ballStartingRadius,
            resetSpeed = supervisorSettingsV0.resetSpeed,
            resetPlatformToIdentity = true
        };

        return balancingTaskSettings;
    }

    private static List<ISettings> JsonSettingsToSupervisorSettings(JsonSettings jsonSettings)
    {
        SupervisorSettings supervisorSettings = new SupervisorSettings();
        supervisorSettings.vectorObservationSize = jsonSettings.members.ContainsKey("vectorObservationSize") ? int.Parse(jsonSettings.members["vectorObservationSize"]) : null;
        supervisorSettings.randomSupervisor = jsonSettings.type == typeof(Supervisor.SupervisorAgentRandom);
        supervisorSettings.advanceNoticeInSeconds = jsonSettings.members.ContainsKey("advanceNoticeInSeconds") ? float.Parse(jsonSettings.members["advanceNoticeInSeconds"]) : null;
        supervisorSettings.decisionRequestIntervalInSeconds = jsonSettings.members.ContainsKey("decisionRequestIntervalInSeconds") ? float.Parse(jsonSettings.members["decisionRequestIntervalInSeconds"]) : null;
        supervisorSettings.decisionRequestIntervalRangeInSeconds = jsonSettings.members.ContainsKey("decisionRequestIntervalRangeInSeconds") ? float.Parse(jsonSettings.members["decisionRequestIntervalRangeInSeconds"]) : null;
        supervisorSettings.mode = jsonSettings.members.ContainsKey("mode") ? jsonSettings.members["mode"] : null;
        supervisorSettings.decisionPeriod = jsonSettings.members.ContainsKey("decisionPeriod") ? int.Parse(jsonSettings.members["decisionPeriod"]) : null;
        supervisorSettings.difficultyIncrementInterval = jsonSettings.members.ContainsKey("difficultyIncrementInterval") ? int.Parse(jsonSettings.members["difficultyIncrementInterval"]) : null;
        supervisorSettings.setConstantDecisionRequestInterval = jsonSettings.members.ContainsKey("setConstantDecisionRequestInterval") ? bool.Parse(jsonSettings.members["setConstantDecisionRequestInterval"]) : null;
        supervisorSettings.advanceNoticeInSeconds = jsonSettings.members.ContainsKey("advanceNoticeInSeconds") ? float.Parse(jsonSettings.members["advanceNoticeInSeconds"]) : null;
        supervisorSettings.supervisorChoice = jsonSettings.type.Name;

        return new() { supervisorSettings, jsonSettings };
    }
}
