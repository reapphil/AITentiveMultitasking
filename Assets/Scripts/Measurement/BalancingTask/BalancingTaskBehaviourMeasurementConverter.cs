using NSubstitute;
using Supervisor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.MLAgents.Actuators;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class BalancingTaskBehaviourMeasurementConverter
{
    private static readonly ProfilerMarker s_convertRawToBinDataPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData");
    private static readonly ProfilerMarker s_createHashCodeDictPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.CreateHashCodeDict");
    private static readonly ProfilerMarker s_initSourceBallAgentPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.initSourceBallAgent");
    private static readonly ProfilerMarker s_initActiveBallAgentPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.initActiveBallAgent");
    private static readonly ProfilerMarker s_updateActiveInstancePerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.UpdateActiveInstance");
    private static readonly ProfilerMarker s_collectDataPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.CollectData");
    private static readonly ProfilerMarker s_resetMeasurementPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.ResetMeasurement");


    public static void ConvertRawToBinData(SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings, BehavioralDataCollectionSettings behavioralDataCollectionSettings, string rawDataPath)
    {
        s_convertRawToBinDataPerfMarker.Begin();
        List<BehaviouralData> behaviouralDataList = Util.ReadDatafromCSV<BehaviouralData>(rawDataPath);
        IBallAgent[] ballAgentsMock = new IBallAgent[2];
        ballAgentsMock[0] = Substitute.For<IBallAgent>();
        ballAgentsMock[1] = Substitute.For<IBallAgent>();
        ballAgentsMock[0].GetScale().Returns(10);
        ballAgentsMock[1].GetScale().Returns(10);

        (BalancingTaskBehaviourMeasurement, ISupervisorAgent) val = CreateBehaviourMeasurment(supervisorSettings, balancingTaskSettings, behavioralDataCollectionSettings, ballAgentsMock);
        BalancingTaskBehaviourMeasurement behaviourMeasurement = val.Item1;
        s_createHashCodeDictPerfMarker.Begin();
        Dictionary<int, IBallAgent> hashCodeDict = CreateHashCodeDict(ballAgentsMock, behaviouralDataList);
        s_createHashCodeDictPerfMarker.End();

        int PreviousActiveAgentHashCode = behaviouralDataList[0].TargetBallAgentHashCode;

        int count = 0;
        IBallAgent activeBallAgent;
        IBallAgent sourceBallAgent;

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        foreach (BehaviouralData behaviouralData in behaviouralDataList)
        {
            count += 1;

            if (count % 100 == 0) Debug.Log(string.Format("{0:F1}% Steps", (float)count / behaviouralDataList.Count * 100));

            activeBallAgent = hashCodeDict[behaviouralData.TargetBallAgentHashCode];
            sourceBallAgent = behaviouralData.SourceBallAgentHashCode != 0 ? hashCodeDict[behaviouralData.SourceBallAgentHashCode] : null;

            s_initActiveBallAgentPerfMarker.Begin();
            activeBallAgent.ClearReceivedCalls(); //the performance drops drastically after a certain number of call if not performed
            activeBallAgent.GetBallLocalPosition().Returns(new Vector3(behaviouralData.TargetBallLocalPositionX, behaviouralData.TargetBallLocalPositionY, behaviouralData.TargetBallLocalPositionZ));
            activeBallAgent.GetBallVelocity().Returns(new Vector3(behaviouralData.TargetBallVelocityX, behaviouralData.TargetBallVelocityY, behaviouralData.TargetBallVelocityZ));
            activeBallAgent.GetPlatformAngle().Returns(new Vector3(behaviouralData.TargetPlatformAngleX, behaviouralData.TargetPlatformAngleY, behaviouralData.TargetPlatformAngleZ));
            s_initActiveBallAgentPerfMarker.End();

            if (behaviouralData.TimeSinceLastSwitch != 0) //TimeSinceLastSwitch is only 0 before the first task switch occours, therefore sourceBallAgent should not be null in any other scenario s.t. a NullReferenceException would indicate a bug
            {
                s_initSourceBallAgentPerfMarker.Begin();
                sourceBallAgent.ClearReceivedCalls();
                sourceBallAgent.GetBallLocalPosition().Returns(new Vector3(behaviouralData.SourceBallLocalPositionX, behaviouralData.SourceBallLocalPositionY, behaviouralData.SourceBallLocalPositionZ));
                sourceBallAgent.GetBallVelocity().Returns(new Vector3(behaviouralData.SourceBallVelocityX, behaviouralData.SourceBallVelocityY, behaviouralData.SourceBallVelocityZ));
                sourceBallAgent.GetPlatformAngle().Returns(new Vector3(behaviouralData.SourcePlatformAngleX, behaviouralData.SourcePlatformAngleY, behaviouralData.SourcePlatformAngleZ));
                s_initSourceBallAgentPerfMarker.End();
            }

            if (behaviouralData.TargetBallAgentHashCode != PreviousActiveAgentHashCode && behaviouralData.TimeBetweenSwitches != 0)
            {
                s_updateActiveInstancePerfMarker.Begin();
                behaviourMeasurement.UpdateActiveInstance(behaviouralData.TimeBetweenSwitches);
                s_updateActiveInstancePerfMarker.End();
            }

            if (behaviouralData.TimeBetweenSwitches == 0)
            {
                s_resetMeasurementPerfMarker.Begin();
                behaviourMeasurement.ResetMeasurement(null, false);
                s_resetMeasurementPerfMarker.End();
            }

            ActionBuffers actionBuffers = new ActionBuffers(new float[] { behaviouralData.ActionZ, behaviouralData.ActionX }, new int[0]);

            s_collectDataPerfMarker.Begin();
            behaviourMeasurement.CollectData(actionBuffers, activeBallAgent, behaviouralData.TimeSinceLastSwitch);
            s_collectDataPerfMarker.End();

            PreviousActiveAgentHashCode = behaviouralData.TargetBallAgentHashCode;
        }
        stopWatch.Stop();

        Debug.Log(string.Format("Time elapsed during convertion: {0} seconds", (float)stopWatch.ElapsedMilliseconds / 1000));

        behaviourMeasurement.SaveReactionTimeToJSON(Util.ConvertRawPathToReactionTimeDataPath(rawDataPath, behavioralDataCollectionSettings, supervisorSettings));
        behaviourMeasurement.SaveBehavioralDataToJSON(Util.ConvertRawPathToBehavioralDataPath(rawDataPath, behavioralDataCollectionSettings, supervisorSettings));
        behaviourMeasurement.SaveReactionTimeToCSV(Util.ConvertRawPathToSimDataPath(rawDataPath));

        s_convertRawToBinDataPerfMarker.End();
    }


    private static (BalancingTaskBehaviourMeasurement, ISupervisorAgent) CreateBehaviourMeasurment(SupervisorSettings supervisorSettings, BalancingTaskSettings balancingTaskSettings,  BehavioralDataCollectionSettings behavioralDataCollectionSettings, IBallAgent[] ballAgentsMock)
    {
        ISupervisorAgent supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgent>();
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(supervisorSettings.decisionRequestIntervalInSeconds);
        ISupervisorAgentRandom supervisorAgentRandomMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();
        supervisorAgentRandomMock.DecisionRequestIntervalInSeconds.Returns(supervisorSettings.decisionRequestIntervalInSeconds);
        supervisorAgentRandomMock.DecisionRequestIntervalRangeInSeconds.Returns(supervisorSettings.decisionRequestIntervalRangeInSeconds);

        BalancingTaskBehaviourMeasurement behaviourMeasurement = new BalancingTaskBehaviourMeasurement(supervisorSettings.randomSupervisor ? supervisorAgentRandomMock : supervisorAgentMock,
                                                                             ballAgentsMock,
                                                                             updateExistingModelBehavior: false,
                                                                             behavioralDataCollectionSettings.fileNameForBehavioralData,
                                                                             behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData,
                                                                             behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData,
                                                                             behavioralDataCollectionSettings.numberOfAngleBinsPerAxis,
                                                                             behavioralDataCollectionSettings.numberOfTimeBins,
                                                                             behavioralDataCollectionSettings.numberOfDistanceBins,
                                                                             behavioralDataCollectionSettings.numberOfDistanceBins_velocity,
                                                                             behavioralDataCollectionSettings.numberOfActionBinsPerAxis,
                                                                             behavioralDataCollectionSettings.collectDataForComparison,
                                                                             behavioralDataCollectionSettings.comparisonFileName,
                                                                             behavioralDataCollectionSettings.comparisonTimeLimit,
                                                                             behavioralDataCollectionSettings.maxNumberOfActions,
                                                                             supervisorSettings,
                                                                             balancingTaskSettings,
                                                                             isSimulation: true,
                                                                             isAbcSimulation: true,
                                                                             sampleSize: -1);

        return (behaviourMeasurement, supervisorSettings.randomSupervisor ? supervisorAgentRandomMock : supervisorAgentMock);
    }


    private static Dictionary<int, IBallAgent> CreateHashCodeDict(IBallAgent[] ballAgentsMock, List<BehaviouralData> behaviouralDataList)
    {
        Dictionary<int, IBallAgent> hashCodeDict = new Dictionary<int, IBallAgent>();
        hashCodeDict.Add(behaviouralDataList[0].TargetBallAgentHashCode, ballAgentsMock[0]);
        int ballAgentId = 1;

        foreach (BehaviouralData behaviouralData in behaviouralDataList)
        {
            if (!hashCodeDict.ContainsKey(behaviouralData.TargetBallAgentHashCode))
            {
                hashCodeDict.Add(behaviouralData.TargetBallAgentHashCode, ballAgentsMock[ballAgentId]);
                ballAgentId += 1;

                //There could be more than 2 hashcodes in case the game is stopped and then resumed
                if (ballAgentId == ballAgentsMock.Length)
                {
                    ballAgentId = 0;
                }
            }
        }

        return hashCodeDict;
    }
}
