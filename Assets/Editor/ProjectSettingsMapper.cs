using Supervisor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ProjectSettingsMapper
{
    public static void HyperparametersToProjectSettings(Dictionary<Type, ISettings> settings, IProjectSettings projectSettings)
    {
        Hyperparameters hyperparameters = (Hyperparameters)settings[typeof(Hyperparameters)];
        SupervisorAgent supervisorAgent = ((SupervisorSettings)settings[typeof(SupervisorSettings)]).randomSupervisor ? projectSettings.GetManagedComponentFor<SupervisorAgentRandom>() : projectSettings.GetManagedComponentFor<SupervisorAgent>();

        projectSettings.TasksGameObjects = GetPrefabs(hyperparameters.tasks);

        supervisorAgent.FocusActiveTask = hyperparameters.focusActiveTask;
        supervisorAgent.HideInactiveTasks = hyperparameters.hideInactiveTasks;
        supervisorAgent.TimeScale = hyperparameters.timeScale;

        projectSettings.GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().SaveBehavioralData = hyperparameters.saveBehavioralData;

        foreach(ITask task in projectSettings.Tasks)
        {
            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                ((ICrTask)task).UseFocusAgent = hyperparameters.useFocusAgent;
            }
            task.IsAutonomous = hyperparameters.autonomous;
        }
    }

    public static void SupervisorSettingsToProjectSettings(SupervisorSettings supervisorSettings, IProjectSettings projectSettings)
    {
        SupervisorAgent supervisorAgent = supervisorSettings.randomSupervisor ? projectSettings.GetManagedComponentFor<SupervisorAgentRandom>() : projectSettings.GetManagedComponentFor<SupervisorAgent>();

        if(supervisorSettings.supervisorChoice != null)
        {
            SupervisorChoice supervisorChoice;
            SupervisorChoice.TryParse(supervisorSettings.supervisorChoice, out supervisorChoice);
            projectSettings.SupervisorChoice = supervisorChoice;
        }
        else
        {
            if (supervisorSettings.randomSupervisor)
            {
                projectSettings.SupervisorChoice = SupervisorChoice.SupervisorAgentRandom;
                ((SupervisorAgentRandom)supervisorAgent).DecisionRequestIntervalRangeInSeconds = supervisorSettings.decisionRequestIntervalRangeInSeconds;
            }
            else
            {
                projectSettings.SupervisorChoice = SupervisorChoice.SupervisorAgent;
            }

            projectSettings.SupervisorChoice = supervisorSettings.randomSupervisor ? SupervisorChoice.SupervisorAgentRandom : SupervisorChoice.SupervisorAgent;
        }
        supervisorAgent.DecisionRequestIntervalInSeconds = supervisorSettings.decisionRequestIntervalInSeconds;
        
        supervisorAgent.DifficultyIncrementInterval = supervisorSettings.difficultyIncrementInterval;
        supervisorAgent.SetConstantDecisionRequestInterval = supervisorSettings.setConstantDecisionRequestInterval;
        supervisorAgent.DecisionPeriod = supervisorSettings.decisionPeriod;
        supervisorAgent.AdvanceNoticeInSeconds = supervisorSettings.advanceNoticeInSeconds;
    }

    public static void BalancingTaskSettingsToProjectSettings(BalancingTaskSettings balancingTaskSettings, IProjectSettings projectSettings)
    {
        BallAgent ballagent = projectSettings.GetManagedComponentFor<BallAgent>();

        ballagent.GlobalDrag = balancingTaskSettings.globalDrag;
        ballagent.UseNegativeDragDifficulty = balancingTaskSettings.useNegativeDragDifficulty;
        ballagent.BallAgentDifficulty = balancingTaskSettings.ballAgentDifficulty;
        ballagent.BallAgentDifficultyDivisionFactor = balancingTaskSettings.ballAgentDifficultyDivisionFactor;
        ballagent.BallStartingRadius = balancingTaskSettings.ballStartingRadius;
        ballagent.ResetSpeed = balancingTaskSettings.resetSpeed;
        ballagent.ResetPlatformToIdentity = balancingTaskSettings.resetPlatformToIdentity;
        ballagent.DecisionPeriod = balancingTaskSettings.decisionPeriod;
    }

    public static void Ball3DAgentHumanCognitionSettingsToProjectSettings(Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings, IProjectSettings projectSettings)
    {
        Ball3DAgentHumanCognition ball3DAgentHumanCognition = projectSettings.GetManagedComponentFor<Ball3DAgentHumanCognition>();

        ball3DAgentHumanCognition.NumberOfBins = ball3DAgentHumanCognitionSettings.numberOfBins;
        ball3DAgentHumanCognition.ShowBeliefState = ball3DAgentHumanCognitionSettings.showBeliefState;
        ball3DAgentHumanCognition.NumberOfSamples = ball3DAgentHumanCognitionSettings.numberOfSamples;
        ball3DAgentHumanCognition.Sigma = ball3DAgentHumanCognitionSettings.sigma;
        ball3DAgentHumanCognition.SigmaMean = ball3DAgentHumanCognitionSettings.sigmaMean;
        ball3DAgentHumanCognition.UpdatePeriod = ball3DAgentHumanCognitionSettings.updatePeriode;
        ball3DAgentHumanCognition.ObservationProbability = ball3DAgentHumanCognitionSettings.observationProbability;
        ball3DAgentHumanCognition.ConstantReactionTime = ball3DAgentHumanCognitionSettings.constantReactionTime;
        ball3DAgentHumanCognition.OldDistributionPersistenceTime = ball3DAgentHumanCognitionSettings.oldDistributionPersistenceTime;
        ball3DAgentHumanCognition.FullVision = ball3DAgentHumanCognitionSettings.fullVision;
    }

    public static void BehavioralDataCollectionSettingsToProjectSettings(BehavioralDataCollectionSettings behavioralDataCollectionSettings, IProjectSettings projectSettings)
    {
        BalancingTaskBehaviourMeasurementBehaviour balancingTaskBehaviourMeasurement = projectSettings.GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>();
        PerformanceMeasurement performanceMeasurement = projectSettings.GetManagedComponentFor<PerformanceMeasurement>();

        performanceMeasurement.MeasurePerformance = behavioralDataCollectionSettings.measurePerformance;
        balancingTaskBehaviourMeasurement.NumberOfAreaBins_BehavioralData = behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData;
        balancingTaskBehaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData = behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData;
        balancingTaskBehaviourMeasurement.NumberOfAngleBinsPerAxis = behavioralDataCollectionSettings.numberOfAngleBinsPerAxis;
        balancingTaskBehaviourMeasurement.NumberOfTimeBins = behavioralDataCollectionSettings.numberOfTimeBins;
        balancingTaskBehaviourMeasurement.NumberOfDistanceBins = behavioralDataCollectionSettings.numberOfDistanceBins;
        balancingTaskBehaviourMeasurement.NumberOfDistanceBins_velocity = behavioralDataCollectionSettings.numberOfDistanceBins_velocity;
        balancingTaskBehaviourMeasurement.NumberOfActionBinsPerAxis = behavioralDataCollectionSettings.numberOfActionBinsPerAxis;
        balancingTaskBehaviourMeasurement.FileNameForBehavioralData = behavioralDataCollectionSettings.fileNameForBehavioralData;
        balancingTaskBehaviourMeasurement.UpdateExistingModelBehavior = behavioralDataCollectionSettings.updateExistingModelBehavior;
        balancingTaskBehaviourMeasurement.CollectDataForComparison = behavioralDataCollectionSettings.collectDataForComparison;
        balancingTaskBehaviourMeasurement.ComparisonFileName = behavioralDataCollectionSettings.comparisonFileName;
        balancingTaskBehaviourMeasurement.MaxNumberOfActions = behavioralDataCollectionSettings.maxNumberOfActions;
        balancingTaskBehaviourMeasurement.ComparisonTimeLimit = behavioralDataCollectionSettings.comparisonTimeLimit;
        balancingTaskBehaviourMeasurement.IsRawDataCollected = behavioralDataCollectionSettings.isRawDataCollected;
    }

    public static void ExperimentSettingsToProjectSettings(ExperimentSettings experimentSettings, IProjectSettings projectSettings)
    {
        Mode mode;
        Mode.TryParse<Mode>(experimentSettings.mode, out mode);

        projectSettings.Mode = mode;
    }

    public static void PerformanceMeasurementSettingsToProjectSettings(PerformanceMeasurementSettings performanceMeasurementSettings, IProjectSettings projectSettings)
    {
        PerformanceMeasurement performanceMeasurement = projectSettings.GetManagedComponentFor<PerformanceMeasurement>();

        performanceMeasurement.MeasurePerformance = true;
        performanceMeasurement.MaxNumberEpisodes = performanceMeasurementSettings.maxNumberEpisodes;
        performanceMeasurement.MinimumScoreForMeasurement = performanceMeasurementSettings.minimumScoreForMeasurement;
        performanceMeasurement.FileNameForScores = performanceMeasurementSettings.fileNameForScores;
        performanceMeasurement.PlayerName = performanceMeasurementSettings.playerName;
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
}
