using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class HyperparametersBase : ISettings
{
    public int version { get; set; }
    public bool? autonomous;
    public string? supervisorModelName;
    public float? timeScale;
    public bool? abcSimulation;
    public string focusAgentModelName;
    public bool? saveBehavioralData;

    public HyperparametersBase(HyperparametersBase hyperparametersBase)
    {
        this.version = hyperparametersBase.version;
        this.autonomous = hyperparametersBase.autonomous;
        this.supervisorModelName = hyperparametersBase.supervisorModelName;
        this.timeScale = hyperparametersBase.timeScale;
        this.abcSimulation = hyperparametersBase.abcSimulation;
        this.focusAgentModelName = hyperparametersBase.focusAgentModelName;
        this.saveBehavioralData = hyperparametersBase.saveBehavioralData;
    }

    public HyperparametersBase() { }
}



[Serializable]
public class HyperparametersV0 : HyperparametersBase
{
    public int? decisionPeriod;
    public bool? focusActivePlatform;
    public bool? hideInactivePlatform;
    public bool? resetPlatformToIdentity;
    public int? numberOfPlatforms;
    public string agentChoice;
    public bool? trainBallAgent;
    public bool? trainSupervisor;
    public string ballAgentModelName;

    public HyperparametersV0() { }

    public HyperparametersV0(HyperparametersBase source) : base(source) { }
}



[Serializable]
public class Hyperparameters : HyperparametersBase
{
    public bool? focusActiveTask;
    public bool? hideInactiveTasks;
    public bool? useFocusAgent;
    public string[] tasks;
    public Dictionary<string, string> taskModels;

    public Hyperparameters() { }

    public Hyperparameters(HyperparametersBase source) : base(source) { }
}


/// <summary>
/// Contains settings that should only be allowed when loaded in the editor but not for evaluation or training.
/// </summary>
[Serializable]
public class ExperimentSettings : ISettings
{
    public int version { get; set; }
    public bool? gameMode;
    public bool? aMSSupport;
    public int? startCountdownAt;

    public ExperimentSettings() { }
}


[Serializable]
public class JsonSettings : ISettings
{
    public int version { get; set; }
    public Type type;
    public Dictionary<string, string> members;

    public JsonSettings(int version, Type type, Dictionary<string, string> fields)
    {
        this.version = version;
        this.type = type;
        this.members = fields;
    }
}


[Serializable]
public class JsonAgentSettings : JsonSettings, IAgentSettings
{
    public int? decisionPeriod { get; set; }
    public string baseClassName { get; set; }

    public JsonAgentSettings(int version, Type type, Dictionary<string, string> fields, int? decisionPeriod) : base(version, type, fields)
    {
        this.decisionPeriod = decisionPeriod;
        this.baseClassName = type.Name;
    }
}


[Serializable]
public class PerformanceMeasurementSettings : ISettings
{
    public int version { get; set; }
    public int? maxNumberEpisodes;
    public int? minimumScoreForMeasurement;
    public string fileNameForScores;
    public string playerName;

    public PerformanceMeasurementSettings() { }
}



public class SupervisorSettingsBase : IAgentSettings
{
    public int version { get; set; }
    public string mode;
    public bool? randomSupervisor;
    public string supervisorChoice;
    public int? vectorObservationSize;
    public bool? setConstantDecisionRequestInterval;
    public float? decisionRequestIntervalInSeconds;
    public float? decisionRequestIntervalRangeInSeconds;
    public int? difficultyIncrementInterval;
    public int? decisionPeriod { get; set; }
    public string baseClassName { get; set; }

    public float? advanceNoticeInSeconds;

    public SupervisorSettingsBase() { }

    public SupervisorSettingsBase(SupervisorSettingsBase supervisorSettingsBase)
    {
        this.mode = supervisorSettingsBase.mode;
        this.randomSupervisor = supervisorSettingsBase.randomSupervisor;
        this.supervisorChoice = supervisorSettingsBase.supervisorChoice;
        this.vectorObservationSize = supervisorSettingsBase.vectorObservationSize;
        this.setConstantDecisionRequestInterval = supervisorSettingsBase.setConstantDecisionRequestInterval;
        this.decisionRequestIntervalInSeconds = supervisorSettingsBase.decisionRequestIntervalInSeconds;
        this.decisionRequestIntervalRangeInSeconds = supervisorSettingsBase.decisionRequestIntervalRangeInSeconds;
        this.difficultyIncrementInterval = supervisorSettingsBase.difficultyIncrementInterval;
        this.decisionPeriod = supervisorSettingsBase.decisionPeriod;
        this.advanceNoticeInSeconds = supervisorSettingsBase.advanceNoticeInSeconds;
        this.baseClassName = "SupervisorAgent";
    }

    public SupervisorSettingsBase(bool randomSupervisor, bool setConstantDecisionRequestInterval, float decisionRequestIntervalInSeconds, float decisionRequestIntervalRangeInSeconds, int difficultyIncrementInterval, int decisionPeriod, float advanceNoticeInSeconds, string supervisorChoice = null)
    {
        this.randomSupervisor = randomSupervisor;
        this.supervisorChoice = supervisorChoice;
        this.setConstantDecisionRequestInterval = setConstantDecisionRequestInterval;
        this.decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds;
        this.decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds;
        this.difficultyIncrementInterval = difficultyIncrementInterval;
        this.decisionPeriod = decisionPeriod;
        this.advanceNoticeInSeconds = advanceNoticeInSeconds;
        this.baseClassName = "SupervisorAgent";
    }
}



[Serializable]
public class SupervisorSettingsV0 : SupervisorSettingsBase
{
    public float? globalDrag;
    public bool? useNegativeDragDifficulty;
    public int? ballAgentDifficulty;
    public double? ballAgentDifficultyDivisionFactor;
    public float? ballStartingRadius;
    public float? resetSpeed;

    public SupervisorSettingsV0() { }

    public SupervisorSettingsV0(SupervisorSettingsBase source) : base(source) { }

    public SupervisorSettingsV0(bool randomSupervisor, bool setConstantDecisionRequestInterval, float decisionRequestIntervalInSeconds, float decisionRequestIntervalRangeInSeconds, float globalDrag, bool useNegativeDragDifficulty, int difficultyIncrementInterval, int decisionPeriod, float advanceNoticeInSeconds, int ballAgentDifficulty, double ballAgentDifficultyDivisionFactor, float ballStartingRadius, float resetSpeed, string supervisorChoice = null)
    {
        this.randomSupervisor = randomSupervisor;
        this.supervisorChoice = supervisorChoice;
        this.setConstantDecisionRequestInterval = setConstantDecisionRequestInterval;
        this.decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds;
        this.decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds;
        this.globalDrag = globalDrag;
        this.useNegativeDragDifficulty = useNegativeDragDifficulty;
        this.difficultyIncrementInterval = difficultyIncrementInterval;
        this.decisionPeriod = decisionPeriod;
        this.advanceNoticeInSeconds = advanceNoticeInSeconds;
        this.ballAgentDifficulty = ballAgentDifficulty;
        this.ballAgentDifficultyDivisionFactor = ballAgentDifficultyDivisionFactor;
        this.ballStartingRadius = ballStartingRadius;
        this.resetSpeed = resetSpeed;
    }
}


[Serializable]
public class SupervisorSettings : SupervisorSettingsBase
{
    public SupervisorSettings() { }

    public SupervisorSettings(SupervisorSettingsBase source) : base(source) { }

    public SupervisorSettings(bool randomSupervisor, bool setConstantDecisionRequestInterval, float decisionRequestIntervalInSeconds, float decisionRequestIntervalRangeInSeconds, int difficultyIncrementInterval, int decisionPeriod, float advanceNoticeInSeconds, string supervisorChoice = null) : base(randomSupervisor, setConstantDecisionRequestInterval, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, difficultyIncrementInterval, decisionPeriod, advanceNoticeInSeconds, supervisorChoice) { }
}



public class BalancingTaskSettings : IAgentSettings
{
    public int version { get; set; }
    public float? globalDrag;
    public bool? useNegativeDragDifficulty;
    public int? ballAgentDifficulty;
    public double? ballAgentDifficultyDivisionFactor;
    public float? ballStartingRadius;
    public float? resetSpeed;
    public bool? resetPlatformToIdentity;
    public bool? isTerminatingTask;
    public int? decisionPeriod { get; set; }

    public string baseClassName { get; set; } = "BallAgent";

    public BalancingTaskSettings() { }

    public BalancingTaskSettings(BalancingTaskSettings balancingTaskSettings)
    {
        this.globalDrag = balancingTaskSettings.globalDrag;
        this.useNegativeDragDifficulty = balancingTaskSettings.useNegativeDragDifficulty;
        this.ballAgentDifficulty = balancingTaskSettings.ballAgentDifficulty;
        this.ballAgentDifficultyDivisionFactor = balancingTaskSettings.ballAgentDifficultyDivisionFactor;
        this.ballStartingRadius = balancingTaskSettings.ballStartingRadius;
        this.resetSpeed = balancingTaskSettings.resetSpeed;
        this.resetPlatformToIdentity = balancingTaskSettings.resetPlatformToIdentity;
        this.decisionPeriod = balancingTaskSettings.decisionPeriod;
    }

    public BalancingTaskSettings(int version, float globalDrag, bool useNegativeDragDifficulty, int ballAgentDifficulty, double ballAgentDifficultyDivisionFactor, float ballStartingRadius, float resetSpeed, bool resetPlatformToIdentity, int decisionPeriod)
    {
        this.version = version;
        this.globalDrag = globalDrag;
        this.useNegativeDragDifficulty = useNegativeDragDifficulty;
        this.ballAgentDifficulty = ballAgentDifficulty;
        this.ballAgentDifficultyDivisionFactor = ballAgentDifficultyDivisionFactor;
        this.ballStartingRadius = ballStartingRadius;
        this.resetSpeed = resetSpeed;
        this.resetPlatformToIdentity = resetPlatformToIdentity;
        this.decisionPeriod = decisionPeriod;
    }
}



[Serializable]
public class Ball3DAgentHumanCognitionSettings : ISettings
{
    public int version { get; set; }
    public int? numberOfBins;
    public bool? showBeliefState;
    public int? numberOfSamples;
    public double? sigma;
    public double? sigmaMean;
    public float? updatePeriode;
    public double? observationProbability;
    public double? constantReactionTime;
    public float? oldDistributionPersistenceTime;
    public bool? fullVision;

    public Ball3DAgentHumanCognitionSettings() { }

    public Ball3DAgentHumanCognitionSettings(Ball3DAgentHumanCognitionSettings ball3DAgentHumanCognitionSettings)
    {
        this.version = ball3DAgentHumanCognitionSettings.version;
        this.numberOfBins = ball3DAgentHumanCognitionSettings.numberOfBins;
        this.showBeliefState = ball3DAgentHumanCognitionSettings.showBeliefState;
        this.numberOfSamples = ball3DAgentHumanCognitionSettings.numberOfSamples;
        this.sigma = ball3DAgentHumanCognitionSettings.sigma;
        this.sigmaMean = ball3DAgentHumanCognitionSettings.sigmaMean;
        this.updatePeriode = ball3DAgentHumanCognitionSettings.updatePeriode;
        this.observationProbability = ball3DAgentHumanCognitionSettings.observationProbability;
        this.constantReactionTime = ball3DAgentHumanCognitionSettings.constantReactionTime;
        this.oldDistributionPersistenceTime = ball3DAgentHumanCognitionSettings.oldDistributionPersistenceTime;
        this.fullVision = ball3DAgentHumanCognitionSettings.fullVision;
    }
}



public class Ball3DAgentHumanCognitionSettingsV0 : Ball3DAgentHumanCognitionSettings
{
    public bool? useFocusAgent;

    public Ball3DAgentHumanCognitionSettingsV0() { }

    public Ball3DAgentHumanCognitionSettingsV0(Ball3DAgentHumanCognitionSettings source) : base(source) { }
}



[Serializable]
public class BehavioralDataCollectionSettings : ISettings
{
    public int version { get; set; }
    public bool? measurePerformance;
    public bool? collectDataForComparison;
    public bool? updateExistingModelBehavior;
    public bool? isRawDataCollected;
    public string comparisonFileName;
    public int? comparisonTimeLimit;
    public int? maxNumberOfActions;
    public string fileNameForBehavioralData;
    public int? numberOfAreaBins_BehavioralData;
    public int? numberOfBallVelocityBinsPerAxis_BehavioralData;
    public int? numberOfAngleBinsPerAxis;
    public int? numberOfDistanceBins;
    public int? numberOfDistanceBins_velocity;
    public int? numberOfDistanceBins_angle;
    public int? numberOfActionBinsPerAxis;
    public int? numberOfTimeBins;  //is ignored if supervisor is not random agent

    public BehavioralDataCollectionSettings() { }
}
