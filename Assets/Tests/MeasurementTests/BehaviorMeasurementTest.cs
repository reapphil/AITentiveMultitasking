using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Supervisor;
using NSubstitute;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.MLAgents.Actuators;
using System.IO;
using CsvHelper;
using System.Globalization;
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using Unity.PerformanceTesting;

public class BehaviorMeasurementTest
{

    private ISupervisorAgent _supervisorAgentMock;
    private IBallAgent[] _ballAgentsMock;
    private BalancingTaskBehaviourMeasurement _behaviourMeasurement;
    private readonly string _fileNameForBehavioralData = "testBehavioralData.json";
    private readonly string _comparisonFileName = Path.Combine("..", "..", "Assets", "Tests", "MeasurementTests", "testComparison.json");
    private BehavioralDataCollectionSettings _behavioralDataCollectionSettings;
    private SupervisorSettings _supervisorSettings;
    private BalancingTaskSettings _balancingTaskSettings;
    private string _sceneBackupPath;
    private static (string, string, string)[] s_paths = new (string, string, string)[] { ("BehaviorTestNoSwitchraw.csv", "BehaviorTestNoSwitchNA196NAN4NV5.json", "BehaviorTestNoSwitch_rt_NT5ND12NVD12NA5.json"),
                                                                               ("BehaviorTestNoTimeMeasurementraw.csv", "BehaviorTestNoTimeMeasurementNA196NAN4NV5.json", "BehaviorTestNoTimeMeasurement_rt_NT5ND12NVD12NA5.json"),
                                                                               ("BehaviorTestResumeraw.csv", "BehaviorTestResumeNA196NAN4NV5.json", "BehaviorTestResume_rt_NT5ND12NVD12NA5.json"),
                                                                               ("BehaviorTestResume2raw.csv", "BehaviorTestResume2NA196NAN4NV5.json", "BehaviorTestResume2_rt_NT5ND12NVD12NA5.json"),
                                                                               ("BehaviorTestMultipleTimeraw.csv", "BehaviorTestMultipleTimeNA196NAN4NV5.json", "BehaviorTestMultipleTime_rt_NT5ND12NVD12NA5.json")};
    

    [SetUp]
    public void Initialize()
    {
        _sceneBackupPath = SceneManagement.BackUpScene();

        _ballAgentsMock = new IBallAgent[2];

        _supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgent>();
        _ballAgentsMock[0] = Substitute.For<IBallAgent>();
        _ballAgentsMock[1] = Substitute.For<IBallAgent>();

        Assert.AreNotEqual(_ballAgentsMock[0], _ballAgentsMock[1]);

        _ballAgentsMock[0].GetScale().Returns(10);
        _ballAgentsMock[1].GetScale().Returns(10);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: _supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 1,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );

        _behavioralDataCollectionSettings = new BehavioralDataCollectionSettings();
        _behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData = _behaviourMeasurement.NumberOfAreaBins_BehavioralData;
        _behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData = _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData;
        _behavioralDataCollectionSettings.numberOfDistanceBins = _behaviourMeasurement.NumberOfDistanceBins_ballPosition;
        _behavioralDataCollectionSettings.numberOfDistanceBins_velocity = _behaviourMeasurement.NumberOfDistanceBins_velocity;
        _behavioralDataCollectionSettings.numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins;
        _behavioralDataCollectionSettings.numberOfAngleBinsPerAxis = _behaviourMeasurement.NumberOfAngleBinsPerAxis;
        _behavioralDataCollectionSettings.numberOfActionBinsPerAxis = _behaviourMeasurement.NumberOfActionBinsPerAxis;

        _supervisorSettings = new SupervisorSettings(false, false, 0, 0, 0, 0, 0);
        _balancingTaskSettings = new BalancingTaskSettings(-1, 0, false, 0, 0, 0, 0, false, 0);

        _ballAgentsMock[0].Received().GetScale();

        (string, string, string) paths = Util.BuildPathsForBehavioralDataFileName(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        try
        {
            File.Delete(paths.Item1);
            File.Delete(paths.Item2);
            File.Delete(Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings));
        }
        catch (Exception)
        {
            Debug.Log(String.Format("Could not delete path {0} or {1}.", paths.Item1, paths.Item2));
        }
    }

    [TearDown]
    public void TeardDown()
    {
        SceneManagement.RestoreScene(_sceneBackupPath);
    }

    [Test]
    public void ResponseTimeMeasurementTest()
    {
        string path = Util.BuildPathsForBehavioralDataFileName(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings).Item2;

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _behaviourMeasurement.NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          _ballAgentsMock[0].GetScale(), 
                                                          _behaviourMeasurement.NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        int angleDistanceBin = 0;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] oldEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsFalse(oldEntry[0][distanceBin][angleDistanceBin].ContainsKey(velocityBin));
        Assert.AreEqual(oldEntry.Length, 1);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        //same agent and same action --> no change in reaction time data
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and different action but no behavioral data available --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioural data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        Assert.IsNotEmpty(newEntry);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioural data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsNotEmpty(newEntry);
        Assert.AreNotEqual(oldEntry[0][distanceBin][angleDistanceBin], newEntry[0][distanceBin][angleDistanceBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Reaction Times", (1 / (double)(_behaviourMeasurement.NumberOfDistanceBins_velocity * _behaviourMeasurement.NumberOfDistanceBins_ballPosition * _behaviourMeasurement.NumberOfAngleBins)) * 100));
    }

    [Test]
    public void ResponseTimeMeasurementTimeRangeTest()
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();
        Dictionary<IBallAgent, bool> isActiveInstanceDict = new Dictionary<IBallAgent, bool>();
        isActiveInstanceDict[_ballAgentsMock[0]] = true;
        isActiveInstanceDict[_ballAgentsMock[1]] = true;
        //TODO: supervisorAgentMock.IsActiveInstanceDict.Returns(isActiveInstanceDict);

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(false, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 10,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );
        _behavioralDataCollectionSettings.numberOfTimeBins = 10;

        string path = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _behaviourMeasurement.NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _behaviourMeasurement.NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(300, 50, 100));
        int angleDistanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetPlatformAngle(), _ballAgentsMock[1].GetPlatformAngle()),
                                                                      Vector3.Distance(_behaviourMeasurement.AngleRangeMin, _behaviourMeasurement.AngleRangeMax),
                                                                      (int)Math.Pow(_behaviourMeasurement.NumberOfAngleBinsPerAxis, 3));

        Assert.IsTrue(angleDistanceBin != 0);

        DateTime t1 = DateTime.Now;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();

        Dictionary<int, (int, (int, double, double))>[][][] oldEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsFalse(oldEntry[0][distanceBin][angleDistanceBin].ContainsKey(velocityBin));
        Assert.AreEqual(10, oldEntry.Length);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and different action but no behavioral data available --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioural data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreNotEqual(oldEntry[0][distanceBin][angleDistanceBin], newEntry[0][distanceBin][angleDistanceBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Reaction Times", (1 / (double)(_behaviourMeasurement.NumberOfDistanceBins_velocity * _behaviourMeasurement.NumberOfDistanceBins_ballPosition * _behaviourMeasurement.NumberOfAngleBins * _behaviourMeasurement.NumberOfTimeBins)) * 100));

        //timebin = 4
        double timeBetweenSwitches = 0.45;
        DateTime t2 = t1.AddSeconds(timeBetweenSwitches);
        Assert.AreEqual(oldEntry[4][distanceBin][angleDistanceBin], newEntry[4][distanceBin][angleDistanceBin]);

        _behaviourMeasurement.UpdateActiveInstance(timeBetweenSwitches);
        System.Threading.Thread.Sleep(10);

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreNotEqual(oldEntry[4][distanceBin][angleDistanceBin], newEntry[4][distanceBin][angleDistanceBin]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)
    }

    [Test]
    public void SuspendedCountMeasurementTest()
    {
        string path = Util.BuildPathsForBehavioralDataFileName(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings).Item2;

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _behaviourMeasurement.NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _behaviourMeasurement.NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        int angleDistanceBin = 0;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] oldEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsFalse(oldEntry[0][distanceBin][angleDistanceBin].ContainsKey(velocityBin));
        Assert.AreEqual(oldEntry.Length, 1);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        //same agent and same action --> no change in reaction time data
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and different action but no behavioral data available --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioural data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        Assert.IsNotEmpty(newEntry);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioural data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsNotEmpty(newEntry);
        Assert.AreEqual(1, newEntry[0][distanceBin][angleDistanceBin][velocityBin].Item2.Item1);

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioural data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioural data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(3, newEntry[0][distanceBin][angleDistanceBin][velocityBin].Item2.Item1);
    }

    [Test]
    public void BehavioralDataMeasurementTest()
    {
        string path = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        //Attention: testing dependency to PositionConverter
        int ballBin = PositionConverter.CoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale()/2, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData));
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        int velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        int rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin);

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(1, 0, 1), new Vector3(1, 0, 1))), entry[ballBin][rangeBin][velocityBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Behavioral Data", (1 / (double)(_behaviourMeasurement.NumberOfVelocityBins_BehavioralData * _behaviourMeasurement.NumberOfAreaBins_BehavioralData * _behaviourMeasurement.NumberOfAngleBins)) * 100));

        actionBuffers = new ActionBuffers(new float[] { 4f, 4f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((2, (new Vector3(5, 0, 5), new Vector3(17, 0, 17))), entry[ballBin][rangeBin][velocityBin]);

        actionBuffers = new ActionBuffers(new float[] { 3f, 6.025f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((3, (new Vector3(11.025f, 0f, 8f), new Vector3(53.300625f, 0f, 26f))), entry[ballBin][rangeBin][velocityBin]);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(3.5f, 0, 2));
        ballBin = PositionConverter.CoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData));
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(-1, 0, 1.5f));
        velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin);

        actionBuffers = new ActionBuffers(new float[] { 2f, 2f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(2, 0, 2), new Vector3(4, 0, 4))), entry[ballBin][rangeBin][velocityBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Behavioral Data", (2 / (double)(_behaviourMeasurement.NumberOfVelocityBins_BehavioralData * _behaviourMeasurement.NumberOfAreaBins_BehavioralData * _behaviourMeasurement.NumberOfAngleBins)) * 100));
        Assert.AreEqual(4, _behaviourMeasurement.ActionCount);
    }

    [Test]
    public void BehavioralDataMeasurementHighPrecisionTest()
    {
        string path = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        //Attention: testing dependency to PositionConverter
        int ballBin = PositionConverter.CoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData));
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        int velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        int rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin);

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1.025f, 1.005f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(1.005f, 0f, 1.025f), new Vector3(1.010025f, 0f, 1.050625f))), entry[ballBin][rangeBin][velocityBin]);
    }

    [Test]
    public void ProportionCollectedComparisonTimesTest()
    {
        string path = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: _supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 1,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: true,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );

        //valid ranges must be considered
        Vector3 v1 = new Vector3(-3, -2, -3);
        Vector3 vt = new Vector3(10, 10, 10);
        Vector3 v2p = Vector3.MoveTowards(v1, vt, PositionConverter.BinToContinuousValue(7, 10, _behaviourMeasurement.NumberOfDistanceBins_ballPosition));
        Vector3 v2v = Vector3.MoveTowards(v1, vt, PositionConverter.BinToContinuousValue(8, 10, _behaviourMeasurement.NumberOfDistanceBins_velocity));

        Vector3 v1a = new Vector3(0, 0, 0);
        Vector3 vta = new Vector3(360, 360, 360);
        Vector3 v2a = Vector3.MoveTowards(v1a, vta, PositionConverter.BinToContinuousValue(70, Vector3.Distance(_behaviourMeasurement.AngleRangeMin, _behaviourMeasurement.AngleRangeMax), (int)Math.Pow(_behaviourMeasurement.NumberOfAngleBinsPerAxis, 3)));

        _ballAgentsMock[0].GetBallLocalPosition().Returns(v1);
        _ballAgentsMock[1].GetBallLocalPosition().Returns(v2p);
        //to match the bin of the comparison file the distance between the angles must be 70 (TODO)
        _ballAgentsMock[1].GetPlatformAngle().Returns(v1a);
        _ballAgentsMock[1].GetPlatformAngle().Returns(v2a);
        _ballAgentsMock[0].GetBallVelocity().Returns(v1);
        _ballAgentsMock[1].GetBallVelocity().Returns(v2v);

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        actionBuffers = new ActionBuffers(new float[] { 1f, 0.9f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (1, 1)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        actionBuffers = new ActionBuffers(new float[] { 0.9f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] entry = LoadDataFromJSON4D<(int, double, double)>(path);

        Assert.IsTrue(entry[0][7][70].ContainsKey(8));
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Comparison Times", 100));
    }

    [Test]
    public void ProportionCollectedComparisonDataTest()
    {
        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: _supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 1,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: true,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );

        _ballAgentsMock[0].GetBallLocalPosition().Returns(PositionConverter.BinToCoordinates(100, 5, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData), 0));
        _ballAgentsMock[0].GetPlatformAngle().Returns(PositionConverter.BinToRangeVector(60, _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin));
        _ballAgentsMock[0].GetBallVelocity().Returns(PositionConverter.BinToRangeVector(200, _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin));

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Comparison Data", 50));

        _ballAgentsMock[0].GetBallLocalPosition().Returns(PositionConverter.BinToCoordinates(110, 5, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData), 0));
        _ballAgentsMock[0].GetPlatformAngle().Returns(PositionConverter.BinToRangeVector(90, _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin));
        _ballAgentsMock[0].GetBallVelocity().Returns(PositionConverter.BinToRangeVector(100, _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin));

        actionBuffers = new ActionBuffers(new float[] { 0.9f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Comparison Data", 100));
    }

    [Test]
    public void ComparisonMaxActionsTest()
    {
        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
                    supervisorAgent: _supervisorAgentMock,
                    ballAgents: _ballAgentsMock,
                    updateExistingModelBehavior: false,
                    fileNameForBehavioralData: this._fileNameForBehavioralData,
                    numberOfAreaBins_BehavioralData: 225,
                    numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
                    numberOfAngleBinsPerAxis_BehavioralData: 5,
                    numberOfTimeBins: 1,
                    numberOfDistanceBins: 12,
                    numberOfDistanceBins_velocity: 12,
                    numberOfActionBinsPerAxis: 5,
                    collectDataForComparison: true,
                    comparisonFileName: this._comparisonFileName,
                    comparisonTimeLimit: 0,
                    maxNumberOfActions: 3
                );

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(200, 50, 100));

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, "Max number of actions reached. Quit Application...");
    }

    [Test]
    public void NonImmediateActionTest()
    {
        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(1, 0, 1));


        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        actionBuffers = new ActionBuffers(new float[] { 1f, 0.9f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[1]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (1, 1)

        _behaviourMeasurement.UpdateActiveInstance(0);

        System.Threading.Thread.Sleep(100);

        actionBuffers = new ActionBuffers(new float[] { 0.9f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, new Regex("Decision Time: 1+"));
    }

    [Test]
    public void UpdateExistingModelBehaviorTest()
    {
        string workingDirectory = Application.dataPath;
        string absolutePath = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting.json");
        string absolutePathTemp = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp.json");
        File.Delete(absolutePathTemp);
        File.Copy(absolutePath, absolutePathTemp);

        string absolutePathReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting_rt.json");
        string absolutePathTempReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp_rt.json");
        File.Delete(absolutePathTempReactionTime);
        File.Copy(absolutePathReactionTime, absolutePathTempReactionTime);

        string fileNameForBehavioralData = Path.Combine("..", "..", "Assets", "Tests", "MeasurementTests", "testUpdateExistingTemp.json");
        (string, string, string) paths = Util.BuildPathsForBehavioralDataFileName(fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        File.Delete(paths.Item1);
        File.Delete(paths.Item2);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: _supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: true,
            fileNameForBehavioralData: fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 1,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );


        //check loading
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entryBehavioural = LoadDataFromJSON3D<(Vector3, Vector3)>(paths.Item1);

        int[] ballBins = new int[] { 100, 110, 0 };
        int[] angleBins = new int[] { 60, 90, 0 };
        int[] velocityBins = new int[] { 200, 100, 0 };

        Assert.AreEqual((2, (new Vector3(5f, 0f, 5f), new Vector3(17f, 0f, 17f))), entryBehavioural[ballBins[0]][angleBins[0]][velocityBins[0]]);
        Assert.AreEqual((1, (new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 4f))), entryBehavioural[ballBins[1]][angleBins[1]][velocityBins[1]]);
        Assert.AreEqual(3, _behaviourMeasurement.ActionCount);
        Assert.IsFalse(entryBehavioural[ballBins[2]][angleBins[2]].ContainsKey(velocityBins[2]));

        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] entryReactionTimes = LoadDataFromJSON4D<(int, double, double)>(paths.Item2);

        int[] distanceBins = new int[] { 7, 0 };
        int[] angleBinsTime = new int[] { 70, 0 };
        int[] velocityBins_reaction = new int[] { 8, 0 };

        Assert.AreEqual((2, (2, 10.9695, 120.32993025)), entryReactionTimes[0][distanceBins[0]][angleBinsTime[0]][velocityBins_reaction[0]]);
        Assert.IsFalse(entryReactionTimes[0][distanceBins[1]][angleBinsTime[1]].ContainsKey(velocityBins[1]));


        //check updating
        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(3.5f, 0, -3.5f));
        int newBallBin = PositionConverter.CoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, (int)Math.Sqrt(_behaviourMeasurement.NumberOfAreaBins_BehavioralData));
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        int newAngleBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _behaviourMeasurement.AngleRangeVector, _behaviourMeasurement.NumberOfAngleBinsPerAxis, _behaviourMeasurement.AngleRangeMin);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(3.5f, 0, -3.5f));
        int newVelocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _behaviourMeasurement.VelocityRangeVector, _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData, _behaviourMeasurement.VelocityRangeMin);
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 0.025f, 0.025f }, new int[0]);

        _behaviourMeasurement.CollectData(actionBuffers, _ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entryBehavioural = LoadDataFromJSON3D<(Vector3, Vector3)>(paths.Item1);
        Assert.AreEqual((2, (new Vector3(5f, 0f, 5f), new Vector3(17f, 0f, 17f))), entryBehavioural[ballBins[0]][angleBins[0]][velocityBins[0]]);
        Assert.AreEqual((1, (new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 4f))), entryBehavioural[ballBins[1]][angleBins[1]][velocityBins[1]]);
        Assert.IsFalse(entryBehavioural[ballBins[2]][angleBins[2]].ContainsKey(velocityBins[2]));
        Assert.AreEqual((1, (new Vector3(0.025f, 0, 0.025f), new Vector3(0.000625f, 0, 0.000625f))).ToString(), entryBehavioural[newBallBin][newAngleBin][newVelocityBin].ToString());
        Assert.AreEqual(4, _behaviourMeasurement.ActionCount);


        File.Delete(absolutePathTemp);
        File.Delete(absolutePathTempReactionTime);
        File.Delete(paths.Item1);
        File.Delete(paths.Item2);
    }

    [Test]
    public void UpdateExistingModelBehaviorAndCollectDataForComparisonInitTest()
    {
        string workingDirectory = Application.dataPath;
        string absolutePath = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting.json");
        string absolutePathTemp = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp.json");
        File.Delete(absolutePathTemp);
        File.Copy(absolutePath, absolutePathTemp);

        string absolutePathReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting_rt.json");
        string absolutePathTempReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp_rt.json");
        File.Delete(absolutePathTempReactionTime);
        File.Copy(absolutePathReactionTime, absolutePathTempReactionTime);

        string fileNameForBehavioralData = Path.Combine("..", "..", "Assets", "Tests", "MeasurementTests", "testUpdateExistingTemp.json"); ;
        (string, string, string) paths = Util.BuildPathsForBehavioralDataFileName(fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        File.Delete(paths.Item1);
        File.Delete(paths.Item2);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: _supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: true,
            fileNameForBehavioralData: fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 1,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: true,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30
        );


        File.Delete(absolutePathTemp);
        File.Delete(absolutePathTempReactionTime);
        File.Delete(paths.Item1);
        File.Delete(paths.Item2);
    }

    [Test]
    public void ConvertRawToBinDataTest()
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        string path = Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            Debug.Log(String.Format("Could not delete path {0}.", path));
        }

        List<BehaviouralData> behaviouralDataList = new List<BehaviouralData>()
        {
            new BehaviouralData(0f,0f,-9080,-0.772119045f,2.11634541f,-1.47088289f,-0.103927992f,0.08879531f,-0.24924694f,19.579464f,3.07926488f,1.02385044f,-9082,-0.8877549f,1.54776f,0.452500343f,0.6505743f,-0.242208049f,1.18963778f,5.809886f,1.47117662f,355.133f,1193,898),
            new BehaviouralData(0f,0f,-9080,-0.7741637f,2.11779165f,-1.47494221f,-0.10237778f,0.07229686f,-0.2029278f,19.579464f,3.07926488f,1.02385044f,-9082,-0.8746319f,1.54318058f,0.476318836f,0.6561387f,-0.228972211f,1.19093513f,5.714787f,1.45120084f,355.211029f,1209,898),
            new BehaviouralData(0f,0f,-9080,-0.7762127f,2.11890721f,-1.47807288f,-0.102459513f,0.05581169f,-0.156645313f,19.579464f,3.07926488f,1.02385044f,-9082,-0.861414433f,1.53873456f,0.5001459f,0.660860062f,-0.222298667f,1.19134736f,5.62117529f,1.43140817f,355.287842f,1226,898),
            new BehaviouralData(-0.0898087844f,-0.186637625f,-9080,-0.7782321f,2.11970162f,-1.48030233f,-0.101036459f,0.0397059023f,-0.111428484f,19.579464f,3.07926488f,1.02385044f,-9082,-0.8481078f,1.53441882f,0.5239763f,0.665314734f,-0.215789273f,1.19151735f,5.432421f,1.39110792f,355.4429f,1261,898),
            new BehaviouralData(-0.322198033f,-0.322198033f,-9082,-0.834771633f,1.529644f,0.547748566f,0.666795135f,-0.2387381f,1.18861449f,5.432421f,1.39110792f,355.4429f,-9080,-0.7802205f,2.11662173f,-1.48249531f,-0.09941988f,-0.1539902f,-0.109645635f,19.0838432f,3.051933f,0.8332048f,11,1265),
            new BehaviouralData(-0.457758456f,-0.4190269f,-9082,-0.8200979f,1.538354f,0.572557449f,0.6780339f,-0.160401553f,1.19188726f,4.79067659f,1.44973135f,354.803741f,-9080,-0.782177f,2.10973f,-1.48465323f,-0.09782916f,-0.344587147f,-0.107891306f,18.9615078f,3.03046966f,0.824173748f,28,1265),
            new BehaviouralData(-0.5449044f,-0.4771242f,-9082,-0.8042345f,1.55518639f,0.597606659f,0.6846782f,-0.153289f,1.186536f,3.95738387f,1.53916764f,353.89505f,-9080,-0.7841277f,2.10202765f,-1.4869051f,-0.0975532457f,-0.385002971f,-0.112926245f,18.8374443f,3.00874186f,0.81507206f,44,1265),
            new BehaviouralData(-0.5642702f,-0.4771242f,-9082,-0.78712225f,1.577621f,0.6224141f,0.6935924f,-0.146248549f,1.1783663f,3.01064634f,1.65883946f,352.8125f,-9080,-0.7860403f,2.09664f,-1.4874301f,-0.09563974f,-0.2693452f,-0.026355803f,18.7156658f,2.98745f,0.8061929f,63,1265),
        };
        

            _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 225,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 6,
            numberOfAngleBinsPerAxis_BehavioralData: 5,
            numberOfTimeBins: 10,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings,
            balancingTaskSettings: _balancingTaskSettings
        );

        _behavioralDataCollectionSettings.numberOfTimeBins = 10;

        Dictionary<IBallAgent, bool> isActiveInstanceDict = new Dictionary<IBallAgent, bool>();

        IBallAgent ballAgentMock;

        foreach (BehaviouralData behaviouralData in behaviouralDataList)
        {   
            foreach (IBallAgent ballAgent in _ballAgentsMock)
            {
                isActiveInstanceDict[ballAgent] = false;
            }

            if (behaviouralData.TargetBallAgentHashCode == -9080)
            {
                ballAgentMock = _ballAgentsMock[0];
            }
            else
            {
                ballAgentMock = _ballAgentsMock[1];
            }

            isActiveInstanceDict[ballAgentMock] = true;
            //TODO: supervisorAgentMock.IsActiveInstanceDict.Returns(isActiveInstanceDict);

            ballAgentMock.GetBallLocalPosition().Returns(new Vector3(behaviouralData.TargetBallLocalPositionX, behaviouralData.TargetBallLocalPositionY, behaviouralData.TargetBallLocalPositionZ));
            ballAgentMock.GetBallVelocity().Returns(new Vector3(behaviouralData.TargetBallVelocityX, behaviouralData.TargetBallVelocityY, behaviouralData.TargetBallVelocityZ));
            ballAgentMock.GetPlatformAngle().Returns(new Vector3(behaviouralData.TargetPlatformAngleX, behaviouralData.TargetPlatformAngleY, behaviouralData.TargetPlatformAngleZ));

            ActionBuffers actionBuffers = new ActionBuffers(new float[] { behaviouralData.ActionZ, behaviouralData.ActionX }, new int[0]);

            _behaviourMeasurement.CollectData(actionBuffers, ballAgentMock);
        }

        _behaviourMeasurement.SaveBehavioralDataToJSON();
        _behaviourMeasurement.SaveReactionTimeToJSON();
        _behaviourMeasurement.SaveRawBehavioralDataToCSV();

        string rawBehavioralDataPath = Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        string behavioralDataPath = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        string reactionTimeDataPath = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        Dictionary<int, (int, (Vector3, Vector3))>[][] behaviouralDataEntry = LoadDataFromJSON3D<(Vector3, Vector3)>(behavioralDataPath);
        Dictionary<int, (int, (int, double, double))>[][][] reactionTimeEntry = LoadDataFromJSON4D<(int, double, double)>(reactionTimeDataPath);

        Assert.IsTrue(File.Exists(behavioralDataPath));
        Assert.IsTrue(File.Exists(reactionTimeDataPath));

        File.Delete(behavioralDataPath);
        File.Delete(reactionTimeDataPath);

        Assert.IsFalse(File.Exists(behavioralDataPath));
        Assert.IsFalse(File.Exists(reactionTimeDataPath));

        SupervisorSettings supervisorSettings = new SupervisorSettings
        {
            randomSupervisor = true,
            decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds,
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds
        };

        BalancingTaskSettings balancingTaskSettings = new BalancingTaskSettings();

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfAreaBins_BehavioralData = _behaviourMeasurement.NumberOfAreaBins_BehavioralData,
            numberOfBallVelocityBinsPerAxis_BehavioralData = _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData,
            numberOfAngleBinsPerAxis = _behaviourMeasurement.NumberOfAngleBinsPerAxis,
            numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins,
            numberOfDistanceBins = _behaviourMeasurement.NumberOfDistanceBins_ballPosition,
            numberOfDistanceBins_velocity = _behaviourMeasurement.NumberOfDistanceBins_velocity,
            numberOfActionBinsPerAxis = _behaviourMeasurement.NumberOfActionBinsPerAxis,
            collectDataForComparison = _behaviourMeasurement.CollectDataForComparison,
            comparisonFileName = _behaviourMeasurement.ComparisonFileName,
            comparisonTimeLimit = _behaviourMeasurement.ComparisonTimeLimit
        };

        BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData(supervisorSettings, balancingTaskSettings, behavioralDataCollectionSettings, rawBehavioralDataPath);

        Debug.Log("behavioralDataPathbehavioralDataPath:" + behavioralDataPath);

        Assert.IsTrue(File.Exists(behavioralDataPath));
        Assert.IsTrue(File.Exists(reactionTimeDataPath));

        Dictionary<int, (int, (Vector3, Vector3))>[][] behaviouralDataBasedOnRawData = LoadDataFromJSON3D<(Vector3, Vector3)>(behavioralDataPath);
        Dictionary<int, (int, (int, double, double))>[][][] reactionTimeBasedOnRawData = LoadDataFromJSON4D<(int, double, double)>(reactionTimeDataPath);

        Assert.AreEqual(behaviouralDataEntry, behaviouralDataBasedOnRawData);
        Assert.AreEqual(reactionTimeEntry, reactionTimeBasedOnRawData);
    }

    [Test]
    public void ConvertRawToBinDataRealDataTest([ValueSource("s_paths")] (string, string, string) path)
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 196,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 5,
            numberOfAngleBinsPerAxis_BehavioralData: 4,
            numberOfTimeBins: 5,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings, 
            balancingTaskSettings: _balancingTaskSettings
        );

        _behavioralDataCollectionSettings.numberOfTimeBins = 5;

        SupervisorSettings supervisorSettings = new SupervisorSettings
        {
            randomSupervisor = true,
            decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds,
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds
        };

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfAreaBins_BehavioralData = _behaviourMeasurement.NumberOfAreaBins_BehavioralData,
            numberOfBallVelocityBinsPerAxis_BehavioralData = _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData,
            numberOfAngleBinsPerAxis = _behaviourMeasurement.NumberOfAngleBinsPerAxis,
            numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins,
            numberOfDistanceBins = _behaviourMeasurement.NumberOfDistanceBins_ballPosition,
            numberOfDistanceBins_velocity = _behaviourMeasurement.NumberOfDistanceBins_velocity,
            numberOfActionBinsPerAxis = _behaviourMeasurement.NumberOfActionBinsPerAxis,
            collectDataForComparison = _behaviourMeasurement.CollectDataForComparison,
            comparisonFileName = _behaviourMeasurement.ComparisonFileName,
            comparisonTimeLimit = _behaviourMeasurement.ComparisonTimeLimit
        };

        string workingDirectory = Util.GetWorkingDirectory();
        string dataPath = Path.Combine(workingDirectory, "Assets", "Tests", "MeasurementTests", "CDTDRI0.8R1GD0.8NDFDII100DP5BD100BDF1S0.1RS7.5");
        string rawBehavioralDataPath = Path.Combine(dataPath, path.Item1);
        string behavioralDataPath = Path.Combine(dataPath, path.Item2);
        string reactionTimePath = Path.Combine(dataPath, path.Item3);

        Dictionary<int, (int, (Vector3, Vector3))>[][] realBehaviouralData = LoadDataFromJSON3D<(Vector3, Vector3)>(behavioralDataPath);
        Dictionary<int, (int, (int, double, double))>[][][] realReactionTime = LoadDataFromJSON4D<(int, double, double)>(reactionTimePath);

        BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData(supervisorSettings, _balancingTaskSettings, behavioralDataCollectionSettings, rawBehavioralDataPath);

        string resultBehavioralDataPath = Util.ConvertRawPathToBehavioralDataPath(rawBehavioralDataPath, behavioralDataCollectionSettings, supervisorSettings);
        string resultReactionTimeDataPath = Util.ConvertRawPathToReactionTimeDataPath(rawBehavioralDataPath, behavioralDataCollectionSettings, supervisorSettings);

        Dictionary<int, (int, (Vector3, Vector3))>[][] resultBehaviouralData = LoadDataFromJSON3D<(Vector3, Vector3)>(resultBehavioralDataPath);
        Dictionary<int, (int, (int, double, double))>[][][] resultReactionTime = LoadDataFromJSON4D<(int, double, double)>(resultReactionTimeDataPath);

        Assert.AreEqual(realBehaviouralData, resultBehaviouralData);
        Assert.AreEqual(realReactionTime, resultReactionTime);
    }

    [Test, Performance]
    public void ConvertRawToBinDataPerformanceTest()
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(
            supervisorAgent: supervisorAgentMock,
            ballAgents: _ballAgentsMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfAreaBins_BehavioralData: 196,
            numberOfBallVelocityBinsPerAxis_BehavioralData: 5,
            numberOfAngleBinsPerAxis_BehavioralData: 4,
            numberOfTimeBins: 5,
            numberOfDistanceBins: 12,
            numberOfDistanceBins_velocity: 12,
            numberOfActionBinsPerAxis: 5,
            collectDataForComparison: false,
            comparisonFileName: this._comparisonFileName,
            comparisonTimeLimit: 30,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings,
            balancingTaskSettings: _balancingTaskSettings
        );

        _behavioralDataCollectionSettings.numberOfTimeBins = 5;

        SupervisorSettings supervisorSettings = new SupervisorSettings
        {
            randomSupervisor = true,
            decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds,
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds
        };

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfAreaBins_BehavioralData = _behaviourMeasurement.NumberOfAreaBins_BehavioralData,
            numberOfBallVelocityBinsPerAxis_BehavioralData = _behaviourMeasurement.NumberOfBallVelocityBinsPerAxis_BehavioralData,
            numberOfAngleBinsPerAxis = _behaviourMeasurement.NumberOfAngleBinsPerAxis,
            numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins,
            numberOfDistanceBins = _behaviourMeasurement.NumberOfDistanceBins_ballPosition,
            numberOfDistanceBins_velocity = _behaviourMeasurement.NumberOfDistanceBins_velocity,
            numberOfActionBinsPerAxis = _behaviourMeasurement.NumberOfActionBinsPerAxis,
            collectDataForComparison = _behaviourMeasurement.CollectDataForComparison,
            comparisonFileName = _behaviourMeasurement.ComparisonFileName,
            comparisonTimeLimit = _behaviourMeasurement.ComparisonTimeLimit
        };

        string workingDirectory = Util.GetWorkingDirectory();
        string dataPath = Path.Combine(workingDirectory, "Assets", "Tests", "MeasurementTests", "CDTDRI0.8R1GD0.8NDFDII100DP5BD100BDF1S0.1RS7.5");
        string rawBehavioralDataPath = Path.Combine(dataPath, "ConverterPerformanceTestraw.csv");

        Measure.Method(() => BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData(supervisorSettings, _balancingTaskSettings, behavioralDataCollectionSettings, rawBehavioralDataPath))
            .MeasurementCount(5)
            .Run();
    }


    private Dictionary<int, (int, T)>[][] LoadDataFromJSON3D<T>(string path)
    {
        //arrray[ballBin]<velocityBin, entry>
        string json = File.ReadAllText(path);

        //arrray[ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][]>(json);

        return entry;
    }


    private Dictionary<int, (int, T)>[][][] LoadDataFromJSON4D<T>(string path)
    {
        //arrray[ballBin]<velocityBin, entry>
        string json = File.ReadAllText(path);

        //arrray[ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][][]>(json);

        return entry;
    }
}
