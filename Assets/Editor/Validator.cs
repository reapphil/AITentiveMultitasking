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
        SupervisorSettings supervisorSettings = null;

        if (settings.ContainsKey(typeof(SupervisorSettings)))
        {
            supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;
        }

        Assert.IsFalse(hyperparameters.saveBehavioralData);
        Assert.AreNotEqual(0, hyperparameters.tasks.Length);

        if (!hyperparameters.autonomous.GetValueOrDefault() && !supervisorSettings.randomSupervisor.GetValueOrDefault())
        {
            Assert.AreNotEqual(0, supervisorSettings.vectorObservationSize);
        }
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
        Assert.IsTrue(supervisorSettings.randomSupervisor.GetValueOrDefault() || hyperparameters.supervisorModelName != "");
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

        if (hyperparameters.useFocusAgent.GetValueOrDefault())
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
        Assert.IsTrue(supervisorSettings.randomSupervisor.GetValueOrDefault() || hyperparameters.supervisorModelName != "");
        Assert.AreNotEqual(0, hyperparameters.tasks.Length);

        Assert.AreNotEqual(0, supervisorSettings.vectorObservationSize);
        Assert.AreEqual(0, supervisorSettings.advanceNoticeInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalInSeconds);
        Assert.AreNotEqual(0, supervisorSettings.decisionRequestIntervalRangeInSeconds);
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

        if (hyperparameters.useFocusAgent.GetValueOrDefault())
        {
            Assert.AreNotEqual("", hyperparameters.focusAgentModelName);
            Assert.AreNotEqual(null, hyperparameters.focusAgentModelName);
        }
    }

    public static void ValidateProjectSettings(ProjectSettings projectSettings)
    {
        Assert.AreNotEqual(0, projectSettings.TasksGameObjects.Length, "ProjectSettings invalid: Number of tasks should not be 0.");

        if (projectSettings.AtLeastOneTaskUsesFocusAgent())
        {
            Assert.IsFalse(projectSettings.GetFocusModels().IsNullOrEmpty(), "ProjectSettings invalid: Focus agent is not defined.");
        }

        if (!projectSettings.SupervisorIsRandomSupervisor() && projectSettings.SupervisorChoice != SupervisorChoice.NoSupport)
        {
            Assert.IsFalse(projectSettings.GetSupervisorModels().IsNullOrEmpty(), "ProjectSettings invalid: Supervisor agent is not defined.");
        }
    }

    public static void ValidateExperimentSettings(Dictionary<Type, ISettings> settings)
    {
        Assert.IsTrue(settings.ContainsKey(typeof(Hyperparameters)));
        Assert.IsTrue(settings.ContainsKey(typeof(SupervisorSettings)));

        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;
        ExperimentSettings experimentSettings = settings[typeof(ExperimentSettings)] as ExperimentSettings;
        SupervisorSettings supervisorSettings = settings[typeof(SupervisorSettings)] as SupervisorSettings;

        Assert.AreNotEqual(null, experimentSettings.gameMode, "ProjectSettings invalid: Mode is not defined.");

        if (hyperparameters.useFocusAgent.GetValueOrDefault() && !experimentSettings.gameMode.GetValueOrDefault())
        {
            Assert.AreNotEqual("", hyperparameters.focusAgentModelName, "ProjectSettings invalid: Focus agent is not defined.");
        }

        if (!supervisorSettings.randomSupervisor.GetValueOrDefault() && experimentSettings.aMSSupport.GetValueOrDefault())
        {
            Assert.AreNotEqual("", hyperparameters.supervisorModelName, "ProjectSettings invalid: Supervisor agent is not defined.");
        }

        Assert.AreNotEqual(0, hyperparameters.tasks.Length, "ProjectSettings invalid: Number of tasks should not be 0.");
    }
}
