using Castle.Components.DictionaryAdapter.Xml;
using Supervisor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEditor.MaterialProperty;
using static UnityEngine.GraphicsBuffer;
using Component = UnityEngine.Component;

public static class ProjectSettingsMapper
{
    public static void JsonSettingsToProjectSettings(JsonSettings settings, IProjectSettings projectSettings)
    {
        List<Component> managedComponents = projectSettings.GetManagedComponentsFor(settings.type);

        foreach (var member in settings.members)
        {
            MemberInfo memberInfo = settings.type.GetMember(member.Key.FirstCharToUpper()).FirstOrDefault();

            if (memberInfo != null)
            {
                var converter = TypeDescriptor.GetConverter(memberInfo.GetUnderlyingType());
                var result = converter.ConvertFrom(member.Value);

                foreach(Component managedComponent in managedComponents)
                {
                    memberInfo.SetValue(managedComponent, result);
                }
            }
        }
    }

    public static void HyperparametersToProjectSettings(Dictionary<Type, ISettings> settings, IProjectSettings projectSettings)
    {
        Hyperparameters hyperparameters = (Hyperparameters)settings[typeof(Hyperparameters)];
        PerformanceMeasurement performanceMeasurement = projectSettings.GetManagedComponentFor<PerformanceMeasurement>();
        SupervisorSettings supervisorSettings = ((SupervisorSettings)settings[typeof(SupervisorSettings)]);
        SupervisorAgent supervisorAgent = GetSupervisorAgent(supervisorSettings.supervisorChoice, projectSettings, supervisorSettings);

        supervisorAgent.FocusActiveTask = hyperparameters.focusActiveTask != null ? hyperparameters.focusActiveTask.GetValueOrDefault() : supervisorAgent.FocusActiveTask;
        supervisorAgent.HideInactiveTasks = hyperparameters.hideInactiveTasks != null ? hyperparameters.hideInactiveTasks.GetValueOrDefault() : supervisorAgent.HideInactiveTasks;
        supervisorAgent.TimeScale = hyperparameters.timeScale != null ? hyperparameters.timeScale.GetValueOrDefault() : supervisorAgent.TimeScale;

        performanceMeasurement.IsAbcSimulation = hyperparameters.abcSimulation != null ? hyperparameters.abcSimulation.GetValueOrDefault() : performanceMeasurement.IsAbcSimulation;

        projectSettings.TasksGameObjects = GetPrefabs(hyperparameters.tasks);
        projectSettings.GetManagedComponentFor<BehaviorMeasurementBehavior>().SaveBehavioralData = hyperparameters.saveBehavioralData != null ? hyperparameters.saveBehavioralData.GetValueOrDefault() : projectSettings.GetManagedComponentFor<BehaviorMeasurementBehavior>().SaveBehavioralData;

        AssignValesToTasks(hyperparameters, projectSettings);
    }

    public static void SupervisorSettingsToProjectSettings(SupervisorSettings supervisorSettings, IProjectSettings projectSettings, ExperimentSettings experimentSettings = null)
    {
        //must be called separately, otherwise the SupervisorAgent (which is updated based on SupervisorChoice.Set) would not be updated according to the supervisorSettings.
        projectSettings.SupervisorChoice = GetSupervisorChoice(supervisorSettings);
        if (experimentSettings != null && !experimentSettings.aMSSupport.GetValueOrDefault()) projectSettings.SupervisorChoice = SupervisorChoice.NoSupport;
        
        SupervisorAgent supervisorAgent = GetSupervisorAgent(supervisorSettings.supervisorChoice, projectSettings, supervisorSettings);

        projectSettings.Mode = supervisorAgent.Mode = GetSupervisorMode(supervisorSettings) != null ? GetSupervisorMode(supervisorSettings).GetValueOrDefault() : supervisorAgent.Mode;
        if (projectSettings.SupervisorChoice == SupervisorChoice.SupervisorAgentRandom) ((SupervisorAgentRandom)supervisorAgent).DecisionRequestIntervalRangeInSeconds = supervisorSettings.decisionRequestIntervalRangeInSeconds != null ? supervisorSettings.decisionRequestIntervalRangeInSeconds.GetValueOrDefault() : ((SupervisorAgentRandom)supervisorAgent).DecisionRequestIntervalRangeInSeconds;
        supervisorAgent.DecisionRequestIntervalInSeconds = supervisorSettings.decisionRequestIntervalInSeconds != null ? supervisorSettings.decisionRequestIntervalInSeconds.GetValueOrDefault() : supervisorAgent.DecisionRequestIntervalInSeconds;
        supervisorAgent.DifficultyIncrementInterval = supervisorSettings.difficultyIncrementInterval != null ? supervisorSettings.difficultyIncrementInterval.GetValueOrDefault() : supervisorAgent.DifficultyIncrementInterval;
        supervisorAgent.SetConstantDecisionRequestInterval = supervisorSettings.setConstantDecisionRequestInterval != null ? supervisorSettings.setConstantDecisionRequestInterval.GetValueOrDefault() : supervisorAgent.FocusActiveTask;
        supervisorAgent.DecisionPeriod = supervisorSettings.decisionPeriod != null ? supervisorSettings.decisionPeriod.GetValueOrDefault() : supervisorAgent.DecisionPeriod;
        supervisorAgent.AdvanceNoticeInSeconds = supervisorSettings.advanceNoticeInSeconds != null ? supervisorSettings.advanceNoticeInSeconds.GetValueOrDefault() : supervisorAgent.AdvanceNoticeInSeconds;
        supervisorAgent.VectorObservationSize = supervisorSettings.vectorObservationSize != null ? supervisorSettings.vectorObservationSize.GetValueOrDefault() : supervisorAgent.VectorObservationSize;
    }

    public static void BalancingTaskSettingsToProjectSettings(BalancingTaskSettings balancingTaskSettings, IProjectSettings projectSettings)
    {
        BallAgent ballagent = projectSettings.GetManagedComponentFor<BallAgent>();

        ballagent.GlobalDrag = balancingTaskSettings.globalDrag != null ? balancingTaskSettings.globalDrag.GetValueOrDefault() : ballagent.GlobalDrag;
        ballagent.UseNegativeDragDifficulty = balancingTaskSettings.useNegativeDragDifficulty != null ? balancingTaskSettings.useNegativeDragDifficulty.GetValueOrDefault() : ballagent.UseNegativeDragDifficulty;
        ballagent.BallAgentDifficulty = balancingTaskSettings.ballAgentDifficulty != null ? balancingTaskSettings.ballAgentDifficulty.GetValueOrDefault() : ballagent.BallAgentDifficulty;
        ballagent.BallAgentDifficultyDivisionFactor = balancingTaskSettings.ballAgentDifficultyDivisionFactor != null ? balancingTaskSettings.ballAgentDifficultyDivisionFactor.GetValueOrDefault() : ballagent.BallAgentDifficultyDivisionFactor;
        ballagent.BallStartingRadius = balancingTaskSettings.ballStartingRadius != null ? balancingTaskSettings.ballStartingRadius.GetValueOrDefault() : ballagent.BallStartingRadius;
        ballagent.ResetSpeed = balancingTaskSettings.resetSpeed != null ? balancingTaskSettings.resetSpeed.GetValueOrDefault() : ballagent.ResetSpeed;
        ballagent.ResetPlatformToIdentity = balancingTaskSettings.resetPlatformToIdentity != null ? balancingTaskSettings.resetPlatformToIdentity.GetValueOrDefault() : ballagent.ResetPlatformToIdentity;
        ballagent.DecisionPeriod = balancingTaskSettings.decisionPeriod != null ? balancingTaskSettings.decisionPeriod.GetValueOrDefault() : ballagent.DecisionPeriod;
        ballagent.IsTerminatingTask = balancingTaskSettings.isTerminatingTask != null ? balancingTaskSettings.isTerminatingTask.GetValueOrDefault() : ballagent.IsTerminatingTask;
    }

    public static void Ball3DAgentHumanCognitionSettingsToProjectSettings(Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings, IProjectSettings projectSettings)
    {
        Ball3DAgentHumanCognition ball3DAgentHumanCognition = projectSettings.GetManagedComponentFor<Ball3DAgentHumanCognition>();

        ball3DAgentHumanCognition.NumberOfBins = ball3DAgentHumanCognitionSettings.numberOfBins != null ? ball3DAgentHumanCognitionSettings.numberOfBins.GetValueOrDefault() : ball3DAgentHumanCognition.NumberOfBins;
        ball3DAgentHumanCognition.ShowBeliefState = ball3DAgentHumanCognitionSettings.showBeliefState != null ? ball3DAgentHumanCognitionSettings.showBeliefState.GetValueOrDefault() : ball3DAgentHumanCognition.ShowBeliefState;
        ball3DAgentHumanCognition.NumberOfSamples = ball3DAgentHumanCognitionSettings.numberOfSamples != null ? ball3DAgentHumanCognitionSettings.numberOfSamples.GetValueOrDefault() : ball3DAgentHumanCognition.NumberOfSamples;
        ball3DAgentHumanCognition.Sigma = ball3DAgentHumanCognitionSettings.sigma != null ? ball3DAgentHumanCognitionSettings.sigma.GetValueOrDefault() : ball3DAgentHumanCognition.Sigma;
        ball3DAgentHumanCognition.SigmaMean = ball3DAgentHumanCognitionSettings.sigmaMean != null ? ball3DAgentHumanCognitionSettings.sigmaMean.GetValueOrDefault() : ball3DAgentHumanCognition.SigmaMean;
        ball3DAgentHumanCognition.UpdatePeriod = ball3DAgentHumanCognitionSettings.updatePeriode != null ? ball3DAgentHumanCognitionSettings.updatePeriode.GetValueOrDefault() : ball3DAgentHumanCognition.UpdatePeriod;
        ball3DAgentHumanCognition.ObservationProbability = ball3DAgentHumanCognitionSettings.observationProbability != null ? ball3DAgentHumanCognitionSettings.observationProbability.GetValueOrDefault() : ball3DAgentHumanCognition.ObservationProbability;
        ball3DAgentHumanCognition.ConstantReactionTime = ball3DAgentHumanCognitionSettings.constantReactionTime != null ? ball3DAgentHumanCognitionSettings.constantReactionTime.GetValueOrDefault() : ball3DAgentHumanCognition.ConstantReactionTime;
        ball3DAgentHumanCognition.OldDistributionPersistenceTime = ball3DAgentHumanCognitionSettings.oldDistributionPersistenceTime != null ? ball3DAgentHumanCognitionSettings.oldDistributionPersistenceTime.GetValueOrDefault() : ball3DAgentHumanCognition.OldDistributionPersistenceTime;
        ball3DAgentHumanCognition.FullVision = ball3DAgentHumanCognitionSettings.fullVision != null ? ball3DAgentHumanCognitionSettings.fullVision.GetValueOrDefault() : ball3DAgentHumanCognition.FullVision;
    }

    public static void BehavioralDataCollectionSettingsToProjectSettings(BehavioralDataCollectionSettings behavioralDataCollectionSettings, IProjectSettings projectSettings, MeasurementSettings measurementSettings)
    {
        BehaviorMeasurementBehavior balancingTaskBehaviourMeasurement = projectSettings.GetManagedComponentFor<BehaviorMeasurementBehavior>();
        PerformanceMeasurement performanceMeasurement = projectSettings.GetManagedComponentFor<PerformanceMeasurement>();

        performanceMeasurement.MeasurePerformance = behavioralDataCollectionSettings.measurePerformance != null ? behavioralDataCollectionSettings.measurePerformance.GetValueOrDefault() : performanceMeasurement.MeasurePerformance;
        balancingTaskBehaviourMeasurement.NumberOfTimeBins = behavioralDataCollectionSettings.numberOfTimeBins != null ? behavioralDataCollectionSettings.numberOfTimeBins.GetValueOrDefault() : balancingTaskBehaviourMeasurement.NumberOfTimeBins;
        balancingTaskBehaviourMeasurement.FileNameForBehavioralData = behavioralDataCollectionSettings.fileNameForBehavioralData != null ? behavioralDataCollectionSettings.fileNameForBehavioralData : balancingTaskBehaviourMeasurement.FileNameForBehavioralData;
        balancingTaskBehaviourMeasurement.UpdateExistingModelBehavior = behavioralDataCollectionSettings.updateExistingModelBehavior != null ? behavioralDataCollectionSettings.updateExistingModelBehavior.GetValueOrDefault() : balancingTaskBehaviourMeasurement.UpdateExistingModelBehavior;
        balancingTaskBehaviourMeasurement.IsRawDataCollected = behavioralDataCollectionSettings.isRawDataCollected != null ? behavioralDataCollectionSettings.isRawDataCollected.GetValueOrDefault() : balancingTaskBehaviourMeasurement.IsRawDataCollected;
        balancingTaskBehaviourMeasurement.MaxNumberOfActions = behavioralDataCollectionSettings.maxNumberOfActions != null ? behavioralDataCollectionSettings.maxNumberOfActions.GetValueOrDefault() : balancingTaskBehaviourMeasurement.MaxNumberOfActions;

        BallStateInformation ballStateInformation = (BallStateInformation)MeasurementSettings.Data[typeof(BallStateInformation)];

        ballStateInformation.NumberOfAreaBinsPerDirection = behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData == null ? ballStateInformation.NumberOfAreaBinsPerDirection : Math.Sqrt(behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData.GetValueOrDefault()) % 1 == 0 ? ((int)Math.Sqrt(behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData.GetValueOrDefault())) : ((int)Math.Sqrt(behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData.GetValueOrDefault())) + 1;
        ballStateInformation.NumberOfBallVelocityBinsPerAxis = behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData != null ? behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData.GetValueOrDefault() : ballStateInformation.NumberOfBallVelocityBinsPerAxis;
        ballStateInformation.NumberOfAngleBinsPerAxis = behavioralDataCollectionSettings.numberOfAngleBinsPerAxis != null ? behavioralDataCollectionSettings.numberOfAngleBinsPerAxis.GetValueOrDefault() : ballStateInformation.NumberOfAngleBinsPerAxis;
        ballStateInformation.NumberOfDistanceBins_ballPosition = behavioralDataCollectionSettings.numberOfDistanceBins != null ? behavioralDataCollectionSettings.numberOfDistanceBins.GetValueOrDefault() : ballStateInformation.NumberOfDistanceBins_ballPosition;
        ballStateInformation.NumberOfDistanceBins_velocity = behavioralDataCollectionSettings.numberOfDistanceBins_velocity != null ? behavioralDataCollectionSettings.numberOfDistanceBins_velocity.GetValueOrDefault() : ballStateInformation.NumberOfDistanceBins_velocity;
        ballStateInformation.NumberOfActionBinsPerAxis = behavioralDataCollectionSettings.numberOfActionBinsPerAxis != null ? behavioralDataCollectionSettings.numberOfActionBinsPerAxis.GetValueOrDefault() : ballStateInformation.NumberOfActionBinsPerAxis;
        ballStateInformation.NumberOfDistanceBins_angle = behavioralDataCollectionSettings.numberOfDistanceBins_angle != null ? behavioralDataCollectionSettings.numberOfDistanceBins_angle.GetValueOrDefault() : ballStateInformation.NumberOfDistanceBins_angle;

        measurementSettings.WriteSettingsToDisk();
    }

    public static void PerformanceMeasurementSettingsToProjectSettings(PerformanceMeasurementSettings performanceMeasurementSettings, IProjectSettings projectSettings)
    {
        PerformanceMeasurement performanceMeasurement = projectSettings.GetManagedComponentFor<PerformanceMeasurement>();

        performanceMeasurement.MeasurePerformance = true;
        performanceMeasurement.MaxNumberEpisodes = performanceMeasurementSettings.maxNumberEpisodes != null ? performanceMeasurementSettings.maxNumberEpisodes.GetValueOrDefault() : performanceMeasurement.MaxNumberEpisodes;
        performanceMeasurement.MinimumScoreForMeasurement = performanceMeasurementSettings.minimumScoreForMeasurement != null ? performanceMeasurementSettings.minimumScoreForMeasurement.GetValueOrDefault() : performanceMeasurement.MinimumScoreForMeasurement;
        performanceMeasurement.FileNameForScores = performanceMeasurementSettings.fileNameForScores;
        performanceMeasurement.PlayerName = performanceMeasurementSettings.playerName;
    }

    public static void ExperimentSettingsToProjectSettings(ExperimentSettings experimentSettings, SupervisorSettings supervisorSettings, IProjectSettings projectSettings)
    {
        SupervisorAgent supervisorAgent = GetSupervisorAgent(supervisorSettings.supervisorChoice, projectSettings, supervisorSettings);
        projectSettings.GameMode = experimentSettings.gameMode.GetValueOrDefault();
        supervisorAgent.StartCountdownAt = experimentSettings.startCountdownAt != null ? experimentSettings.startCountdownAt.GetValueOrDefault() : supervisorAgent.StartCountdownAt;
    }


    private static string GetBackingFieldName(string propertyName)
    {
        return $"<{propertyName}>k__BackingField";
    }

    private static GameObject[] GetPrefabs(string[] names)
    {
        GameObject[] taskGameObjects = new GameObject[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            taskGameObjects[i] = Resources.Load<GameObject>(names[i]);
        }

        return taskGameObjects;
    }

    private static SupervisorAgent GetSupervisorAgent(string supervisorChoice, IProjectSettings projectSettings, SupervisorSettings supervisorSettings)
    {
        Type type = Util.GetType(supervisorChoice);
        SupervisorAgent supervisorAgent;

        if (type == null)
        {
            supervisorAgent = supervisorSettings.randomSupervisor.GetValueOrDefault() ? projectSettings.GetManagedComponentFor<SupervisorAgentRandom>() : projectSettings.GetManagedComponentFor<SupervisorAgent>();
        }
        else
        {
            supervisorAgent = (SupervisorAgent)projectSettings.GetManagedComponentFor(type);
        }

        return supervisorAgent;
    }

    private static SupervisorChoice GetSupervisorChoice(SupervisorSettings supervisorSettings)
    {
        if (supervisorSettings.supervisorChoice != null)
        {
            SupervisorChoice.TryParse(supervisorSettings.supervisorChoice, out SupervisorChoice supervisorChoice);
            return supervisorChoice;
        }
        else
        {
            if (supervisorSettings.randomSupervisor.GetValueOrDefault())
            {
                return SupervisorChoice.SupervisorAgentRandom;
            }
            else
            {
                return SupervisorChoice.SupervisorAgent;
            }
        }
    }

    private static Supervisor.Mode? GetSupervisorMode(SupervisorSettings supervisorSettings)
    {
        if (supervisorSettings.mode != null)
        {
            if (!Supervisor.Mode.TryParse(supervisorSettings.mode.FirstCharToUpper(), out Supervisor.Mode mode))
            {
                Supervisor.Mode.TryParse("Supervisor." + supervisorSettings.mode.FirstCharToUpper(), out mode);
            }
            return mode;
        }

        return null;
    }

    private static void AssignValesToTasks(Hyperparameters hyperparameters, IProjectSettings projectSettings)
    {
        foreach (ITask task in projectSettings.Tasks)
        {
            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                ((ICrTask)task).UseFocusAgent = hyperparameters.useFocusAgent != null ? hyperparameters.useFocusAgent.GetValueOrDefault() : ((ICrTask)task).UseFocusAgent;
            }
            task.IsAutonomous = hyperparameters.autonomous != null ? hyperparameters.autonomous.GetValueOrDefault() : task.IsAutonomous;
        }
    }
}
