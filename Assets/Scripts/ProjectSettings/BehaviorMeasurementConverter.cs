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


/**
* Disclaimer: This class is under construction and must be further developed to support the new, generic version of the BehaviorMeasurement class
* for different tasks. Currently the implementation only works for two equal balancing tasks (e.g. because of the problematic BehavioralData format).
* Nevertheless, the behavior measurement class saves the raw data, s.t. after implementing the reading of the IStateInformation, the conversion of 
* the raw data to the discrete data can be performed. Try to resolve the "ConvertRawToBinDataTest" function to fix this issue.
**
public static class BehaviorMeasurementConverter
{
    private static readonly ProfilerMarker s_convertRawToBinDataPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData");
    private static readonly ProfilerMarker s_createHashCodeDictPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.CreateHashCodeDict");
    private static readonly ProfilerMarker s_initSourceBallAgentPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.initSourceBallAgent");
    private static readonly ProfilerMarker s_initActiveBallAgentPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.initActiveBallAgent");
    private static readonly ProfilerMarker s_updateActiveInstancePerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.UpdateActiveInstance");
    private static readonly ProfilerMarker s_collectDataPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.CollectData");
    private static readonly ProfilerMarker s_resetMeasurementPerfMarker = new ProfilerMarker("BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData.ResetMeasurement");

    public static void ConvertRawToBinData(SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, BehavioralDataCollectionSettings behavioralDataCollectionSettings, string rawDataPath)
    {
        s_convertRawToBinDataPerfMarker.Begin();

        List<BehavioralData> behaviouralDataList = Util.ReadDatafromCSV<BehavioralData>(rawDataPath);

        Dictionary<Type, ISettings> data = MeasurementSettings.Data;

        ITask[] taskMock = new ITask[2];
        taskMock[0] = Substitute.For<ITask>();
        taskMock[1] = Substitute.For<ITask>();

        IStateInformation stateInformation = data[typeof(BallStateInformation)] as IStateInformation;

        BallStateInformation[] ballStateInformation = new BallStateInformation[2];
        ballStateInformation[0] = new BallStateInformation();
        ballStateInformation[1] = new BallStateInformation();

        taskMock[0].StateInformation.Returns(ballStateInformation[0]);
        taskMock[1].StateInformation.Returns(ballStateInformation[1]);
        taskMock[0].StateInformation.UpdateMeasurementSettings(stateInformation);
        taskMock[1].StateInformation.UpdateMeasurementSettings(stateInformation);

        (BehaviorMeasurement, ISupervisorAgent) val = CreateBehaviourMeasurment(supervisorSettings, hyperparameters, behavioralDataCollectionSettings, taskMock);
        BehaviorMeasurement behaviourMeasurement = val.Item1;
        s_createHashCodeDictPerfMarker.Begin();
        Dictionary<int, ITask> hashCodeDict = CreateHashCodeDict(taskMock, behaviouralDataList);
        s_createHashCodeDictPerfMarker.End();

        int PreviousActiveAgentHashCode = behaviouralDataList[0].TargetTaskId;

        int count = 0;
        ITask targetTask;
        ITask sourceTask;

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        foreach (BehavioralData behaviouralData in behaviouralDataList)
        {
            //LogToFile.LogPropertiesFieldsOfObject(behaviouralData);
            
            count += 1;

            if (count % 100 == 0) Debug.Log(string.Format("{0:F1}% Steps", (float)count / behaviouralDataList.Count * 100));

            s_initActiveBallAgentPerfMarker.Begin();
            targetTask = hashCodeDict[behaviouralData.TargetTaskId];
            targetTask.ClearReceivedCalls(); //the performance drops drastically after a certain number of call if not performed
            targetTask.StateInformation.UpdateStateInformation(Array.IndexOf(taskMock, targetTask) == 0 ? behaviouralData.StateA : behaviouralData.StateB);
            s_initActiveBallAgentPerfMarker.End();

            //Debug.Log("targetTask: " + hashCodeDict[behaviouralData.TargetTaskId] + " \t Array.Index: " + Array.IndexOf(taskMock, targetTask) + " \t targetTask.StateInformation: " + ((BallStateInformation)targetTask.StateInformation).PlatformAngleX);

            if (behaviouralData.TimeSinceLastSwitch != 0) //TimeSinceLastSwitch is only 0 before the first task switch occurs, therefore sourceBallAgent should not be null in any other scenario s.t. a NullReferenceException would indicate a bug
            {
                s_initSourceBallAgentPerfMarker.Begin();
                sourceTask = hashCodeDict[behaviouralData.SourceTaskId];
                sourceTask.ClearReceivedCalls();
                sourceTask.StateInformation.UpdateStateInformation(Array.IndexOf(taskMock, sourceTask) == 0 ? behaviouralData.StateA : behaviouralData.StateB);
                s_initSourceBallAgentPerfMarker.End();
            }

            if (behaviouralData.TargetTaskId != PreviousActiveAgentHashCode && behaviouralData.TimeBetweenSwitches != 0)
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


            s_collectDataPerfMarker.Begin();
            behaviourMeasurement.CollectData(Array.IndexOf(taskMock, targetTask) == 0 ? behaviouralData.StateA.PerformedActions : behaviouralData.StateB.PerformedActions, (ITask)targetTask, behaviouralData.TimeSinceLastSwitch);
            s_collectDataPerfMarker.End();

            PreviousActiveAgentHashCode = behaviouralData.TargetTaskId;
        }
        stopWatch.Stop();

        Debug.Log(string.Format("Time elapsed during conversion: {0} seconds", (float)stopWatch.ElapsedMilliseconds / 1000));

        int numberofTimeBins = behavioralDataCollectionSettings.numberOfTimeBins > 1 ? behavioralDataCollectionSettings.numberOfTimeBins.GetValueOrDefault() : 1;

        behaviourMeasurement.SaveReactionTimeToJSON(Util.ConvertRawPathToReactionTimeDataPath(rawDataPath, stateInformation.GetRelationalDimensions(stateInformation.GetType(), numberofTimeBins), supervisorSettings, "BSI"));
        behaviourMeasurement.SaveBehavioralDataToJSON(Util.ConvertRawPathToBehavioralDataPath(rawDataPath, stateInformation.BehaviorDimensions, supervisorSettings, "BSI"));
        behaviourMeasurement.SaveReactionTimeToCSV(Util.ConvertRawPathToSimDataPath(rawDataPath));

        s_convertRawToBinDataPerfMarker.End();
    }


    private static (BehaviorMeasurement, ISupervisorAgent) CreateBehaviourMeasurment(SupervisorSettings supervisorSettings, Hyperparameters hyperparameters, BehavioralDataCollectionSettings behavioralDataCollectionSettings, ITask[] tasksMock)
    {
        ISupervisorAgent supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgent>();
        supervisorAgentMock.Tasks.Returns(tasksMock);
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(supervisorSettings.decisionRequestIntervalInSeconds.GetValueOrDefault());
        ISupervisorAgentRandom supervisorAgentRandomMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();
        supervisorAgentRandomMock.DecisionRequestIntervalInSeconds.Returns(supervisorSettings.decisionRequestIntervalInSeconds.GetValueOrDefault());
        supervisorAgentRandomMock.DecisionRequestIntervalRangeInSeconds.Returns(supervisorSettings.decisionRequestIntervalRangeInSeconds.GetValueOrDefault());
        supervisorAgentRandomMock.Tasks.Returns(tasksMock);

        BehaviorMeasurement behaviourMeasurement = new(
            supervisorAgent: supervisorSettings.randomSupervisor.GetValueOrDefault() ? supervisorAgentRandomMock : supervisorAgentMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: behavioralDataCollectionSettings.fileNameForBehavioralData,
            numberOfTimeBins: behavioralDataCollectionSettings.numberOfTimeBins.GetValueOrDefault(),
            maxNumberOfActions: behavioralDataCollectionSettings.maxNumberOfActions.GetValueOrDefault(),
            supervisorSettings: supervisorSettings,
            hyperparameters: hyperparameters,
            isSimulation: true,
            isAbcSimulation: true,
            sampleSize: -1);

        return (behaviourMeasurement, supervisorSettings.randomSupervisor.GetValueOrDefault() ? supervisorAgentRandomMock : supervisorAgentMock);
    }


    private static Dictionary<int, ITask> CreateHashCodeDict(ITask[] taskMocks, List<BehavioralData> behaviouralDataList)
    {
        Dictionary<int, ITask> hashCodeDict = new()
        {
            { behaviouralDataList[0].TargetTaskId, taskMocks[0] }
        };

        int ballAgentId = 1;

        foreach (BehavioralData behaviouralData in behaviouralDataList)
        {
            if (!hashCodeDict.ContainsKey(behaviouralData.TargetTaskId))
            {
                hashCodeDict.Add(behaviouralData.TargetTaskId, taskMocks[ballAgentId]);
                ballAgentId += 1;

                //There could be more than 2 hash codes in case the game is stopped and then resumed
                if (ballAgentId == taskMocks.Length)
                {
                    ballAgentId = 0;
                }
            }
        }

        return hashCodeDict;
    }
}
**/