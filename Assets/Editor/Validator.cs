using NUnit.Framework;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using Castle.Core.Internal;

public static class Validator
{
    public static void ValidateTraining(Dictionary<Type, ISettings> settings)
    {
        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;

        Assert.IsFalse(hyperparameters.saveBehavioralData);
        Assert.AreNotEqual(0, hyperparameters.tasks.Length);

        Assert.AreNotEqual(0, supervisorSettings.vectorObservationSize);
    }

    public static void ValidateAbc(Dictionary<Type, ISettings> settings)
    {
        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
        BalancingTaskSettings balancingTaskSettings = settings[typeof(BalancingTaskSettings)] as BalancingTaskSettings;
        Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings = settings[typeof(Ball3DAgentHumanCognitionSettings)] as Ball3DAgentHumanCognitionSettings;
        BehavioralDataCollectionSettings behavioralDataCollectionSettings = settings[typeof(BehavioralDataCollectionSettings)] as BehavioralDataCollectionSettings; 

        Assert.IsFalse(hyperparameters.autonomous);
        //Assert.IsTrue(hyperparameters.agentChoice == "Ball3DAgentHumanCognition" || hyperparameters.agentChoice == "Ball3DAgentHumanCognitionSingleProbabilityDistribution");
        Assert.AreNotEqual("", hyperparameters.taskModels["BallAgent"]);
        Assert.IsTrue(hyperparameters.taskModels.ContainsKey("BallAgent"));
        Assert.IsTrue(supervisorSettings.randomSupervisor || hyperparameters.supervisorModelName != "");
        Assert.IsTrue(hyperparameters.saveBehavioralData);
        Assert.AreNotEqual(0, hyperparameters.tasks.Length);

        Assert.AreNotEqual(0, supervisorSettings.vectorObservationSize);
        Assert.AreEqual(0, supervisorSettings.advanceNoticeInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalRangeInSeconds);
        Assert.AreNotEqual(0, balancingTaskSettings.ballStartingRadius);
        Assert.AreNotEqual(0, balancingTaskSettings.resetSpeed);

        Assert.IsFalse(ball3DAgentHumanCognitionSettings.fullVision);

        Assert.IsFalse(behavioralDataCollectionSettings.collectDataForComparison);
        Assert.IsFalse(behavioralDataCollectionSettings.updateExistingModelBehavior);
        Assert.AreEqual(0, behavioralDataCollectionSettings.maxNumberOfActions);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfAngleBinsPerAxis);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfDistanceBins);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfDistanceBins_velocity);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfActionBinsPerAxis);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfTimeBins);

        if (hyperparameters.useFocusAgent)
        {
            Assert.AreNotEqual("", hyperparameters.focusAgentModelName);
            Assert.AreNotEqual(null, hyperparameters.focusAgentModelName);
        }
    }

    public static void ValidateEvaluation(Dictionary<Type, ISettings> settings)
    {
        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
        BalancingTaskSettings balancingTaskSettings = settings[typeof(BalancingTaskSettings)] as BalancingTaskSettings;
        Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings = settings[typeof(Ball3DAgentHumanCognitionSettings)] as Ball3DAgentHumanCognitionSettings;
        BehavioralDataCollectionSettings behavioralDataCollectionSettings = settings[typeof(BehavioralDataCollectionSettings)] as BehavioralDataCollectionSettings;

        Assert.IsTrue(hyperparameters.taskModels.ContainsKey("BallAgent"));
        Assert.IsTrue(supervisorSettings.randomSupervisor || hyperparameters.supervisorModelName != "");
        Assert.AreNotEqual(0, hyperparameters.tasks.Length);

        Assert.AreNotEqual(0, supervisorSettings.vectorObservationSize);
        Assert.AreEqual(0, supervisorSettings.advanceNoticeInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalRangeInSeconds);
        Assert.AreNotEqual(0, balancingTaskSettings.ballStartingRadius);
        Assert.AreNotEqual(0, balancingTaskSettings.resetSpeed);

        if (hyperparameters.tasks.Contains("Ball3DAgentHumanCognition") || hyperparameters.tasks.Contains("Ball3DAgentHumanCognitionSingleProbabilityDistribution"))
        {
            Assert.AreNotEqual(0, ball3DAgentHumanCognitionSettings.numberOfBins);
            Assert.AreNotEqual(0, ball3DAgentHumanCognitionSettings.numberOfSamples);
            Assert.AreNotEqual(0, ball3DAgentHumanCognitionSettings.observationProbability);
            Assert.IsFalse(ball3DAgentHumanCognitionSettings.fullVision);
        }

        Assert.AreNotEqual(0, behavioralDataCollectionSettings.maxNumberOfActions);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfAngleBinsPerAxis);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfDistanceBins);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfDistanceBins_velocity);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfActionBinsPerAxis);
        Assert.AreNotEqual(0, behavioralDataCollectionSettings.numberOfTimeBins);

        if (hyperparameters.useFocusAgent)
        {
            Assert.AreNotEqual("", hyperparameters.focusAgentModelName);
            Assert.AreNotEqual(null, hyperparameters.focusAgentModelName);
        }
    }

    public static void ValidateProjectSettings(ProjectSettings projectSettings)
    {
        Assert.AreNotEqual(0, projectSettings.TasksGameObjects.Length, "ProjectSettings invalid: Number of tasks should not be 0.");

        if (projectSettings.AtLeastOneTaskUsesFocusAgent() && !projectSettings.ModeIsGameMode())
        {
            Assert.IsFalse(projectSettings.GetFocusModels().IsNullOrEmpty(), "ProjectSettings invalid: Focus agent is not defined.");
        }

        if (!projectSettings.SupervisorIsRandomSupervisor() && projectSettings.Mode != Mode.GameModeNoSupervisor)
        {
            Assert.IsFalse(projectSettings.GetSupervisorModels().IsNullOrEmpty(), "ProjectSettings invalid: Supervisor agent is not defined.");
        }
    }

    public static void ValidateExperimentSettings(Dictionary<Type, ISettings> settings)
    {
        Assert.IsTrue(settings.ContainsKey(typeof(Hyperparameters)));
        Assert.IsTrue(settings.ContainsKey(typeof(SupervisorSettings)));
        Assert.IsTrue(settings.ContainsKey(typeof(ExperimentSettings)));

        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
        ExperimentSettings experimentSettings = settings[typeof(ExperimentSettings)] as ExperimentSettings;

        Assert.AreNotEqual(0, hyperparameters.tasks.Length, "ProjectSettings invalid: Number of tasks should not be 0.");

        if (hyperparameters.useFocusAgent && !IsGameMode(experimentSettings.mode))
        {
            Assert.AreNotEqual("", hyperparameters.focusAgentModelName, "ProjectSettings invalid: Focus agent is not defined.");
        }

        if (!supervisorSettings.randomSupervisor && experimentSettings.mode != "GameModeNoSupervisor")
        {
            Assert.AreNotEqual("", hyperparameters.supervisorModelName, "ProjectSettings invalid: Supervisor agent is not defined.");
        }
    }


    private static bool IsGameMode(string mode)
    {
        return mode == "GameModeSupervisor" || mode == "GameModeNoSupervisor" || mode == "GameModeNotification";
    }
}
