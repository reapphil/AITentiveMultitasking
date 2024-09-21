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
using static UnityEngine.EventSystems.EventTrigger;
using System.Linq;

public class BehaviorMeasurementTest
{

    private ISupervisorAgent _supervisorAgentMock;
    private IBallAgent[] _ballAgentsMock;
    private BehaviorMeasurement _behaviourMeasurement;
    private readonly string _fileNameForBehavioralData = "testBehavioralData.json";
    private readonly string _comparisonFileName = Path.Combine("..", "..", "Assets", "Tests", "MeasurementTests", "testComparison.json");
    private BallStateInformation[] _ballStateInformation;
    private SupervisorSettings _supervisorSettings;
    private Hyperparameters _hyperparameters;
    Dictionary<string, int> _measurementSettings;
    private string _sceneBackupPath;
    private static (string, string, string)[] s_paths = new (string, string, string)[] { ("BehaviorTestNoSwitchraw.csv", "BehaviorTestNoSwitch_bBSID225D125D216.json", "BehaviorTestNoSwitch_rt_BSID1D12D5D12.json"),
                                                                               ("BehaviorTestNoTimeMeasurementraw.csv", "BehaviorTestNoTimeMeasurement_bBSID225D125D216.json", "BehaviorTestNoTimeMeasurement_rt_BSID1D12D5D12.json"),
                                                                               ("BehaviorTestResumeraw.csv", "BehaviorTestResume_bBSID225D125D216.json", "BehaviorTestResume_rt_BSID1D12D5D12.json"),
                                                                               ("BehaviorTestResume2raw.csv", "BehaviorTestResume2_bBSID225D125D216.json", "BehaviorTestResume2_rt_BSID1D12D5D12.json"),
                                                                               ("BehaviorTestMultipleTimeraw.csv", "BehaviorTestMultipleTime_bBSID225D125D216.json", "BehaviorTestMultipleTime_rt_BSID1D12D5D12.json")};

    Vector3 _velocityRangeVector;
    Vector3 _angleRangeVector;


    [SetUp]
    public void Initialize()
    {
        _sceneBackupPath = SceneManagement.BackUpScene();

        _ballAgentsMock = new IBallAgent[2];

        _supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgent>();
        _ballAgentsMock[0] = Substitute.For<IBallAgent, ITask>();
        _ballAgentsMock[1] = Substitute.For<IBallAgent, ITask>();

        _supervisorAgentMock.Tasks.Returns(new ITask[] { (ITask)_ballAgentsMock[0], (ITask)_ballAgentsMock[1] });

        Assert.AreNotEqual(_ballAgentsMock[0], _ballAgentsMock[1]);

        _ballAgentsMock[0].GetScale().Returns(10);
        _ballAgentsMock[1].GetScale().Returns(10);

        _supervisorSettings = new SupervisorSettings(false, false, 0, 0, 0, 0, 0);
        _hyperparameters = new Hyperparameters()
        {
            tasks = new string[] { "Ball3DAgentHumanCognition", "Ball3DAgentHumanCognition" },
        };

        _ballStateInformation = new BallStateInformation[2];

        _ballStateInformation[0] = new BallStateInformation();
        _ballStateInformation[1] = new BallStateInformation();
        BallStateInformation measurementSettings = new BallStateInformation();

        measurementSettings.NumberOfActionBinsPerAxis = _ballStateInformation[0].NumberOfActionBinsPerAxis = _ballStateInformation[1].NumberOfActionBinsPerAxis = 5;
        measurementSettings.NumberOfDistanceBins_angle = _ballStateInformation[0].NumberOfDistanceBins_angle = _ballStateInformation[1].NumberOfDistanceBins_angle = 5;
        measurementSettings.NumberOfDistanceBins_ballPosition = _ballStateInformation[0].NumberOfDistanceBins_ballPosition = _ballStateInformation[1].NumberOfDistanceBins_ballPosition = 12;
        measurementSettings.NumberOfDistanceBins_velocity = _ballStateInformation[0].NumberOfDistanceBins_velocity = _ballStateInformation[1].NumberOfDistanceBins_velocity = 12;
        measurementSettings.NumberOfAngleBinsPerAxis = _ballStateInformation[0].NumberOfAngleBinsPerAxis = _ballStateInformation[1].NumberOfAngleBinsPerAxis = 5;
        measurementSettings.NumberOfAreaBinsPerDirection = _ballStateInformation[0].NumberOfAreaBinsPerDirection = _ballStateInformation[1].NumberOfAreaBinsPerDirection = 15;
        measurementSettings.NumberOfBallVelocityBinsPerAxis = _ballStateInformation[0].NumberOfBallVelocityBinsPerAxis = _ballStateInformation[1].NumberOfBallVelocityBinsPerAxis = 6;

        MeasurementSettings.Data[typeof(BallStateInformation)] = measurementSettings;

        ((ITask)_ballAgentsMock[0]).StateInformation.Returns(_ballStateInformation[0]);
        ((ITask)_ballAgentsMock[1]).StateInformation.Returns(_ballStateInformation[1]);

        BallStateInformation.PlatformRadius = 5;

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: _supervisorAgentMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfTimeBins: 1,
            supervisorSettings: _supervisorSettings,
            hyperparameters: _hyperparameters
        );

        _velocityRangeVector = new Vector3(BallStateInformation.VelocityRangeMax.x - BallStateInformation.VelocityRangeMin.x,
                                           BallStateInformation.VelocityRangeMax.y - BallStateInformation.VelocityRangeMin.y,
                                           BallStateInformation.VelocityRangeMax.z - BallStateInformation.VelocityRangeMin.z);

        _angleRangeVector = new Vector3(BallStateInformation.AngleRangeMax.x - BallStateInformation.AngleRangeMin.x,
                                        BallStateInformation.AngleRangeMax.y - BallStateInformation.AngleRangeMin.y,
                                        BallStateInformation.AngleRangeMax.z - BallStateInformation.AngleRangeMin.z);   

        string behaviorPath = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, "BSI", _ballStateInformation[0].BehaviorDimensions);
        string reactionTimePath = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation)));

        try
        {
            File.Delete(behaviorPath);
            File.Delete(reactionTimePath);
            File.Delete(Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters));
        }
        catch (Exception)
        {
            Debug.Log(String.Format("Could not delete path {0} or {1}.", behaviorPath, reactionTimePath));
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
        string path = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation)));

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballStateInformation[1].BallPositionX = _ballAgentsMock[1].GetBallLocalPosition().x;
        _ballStateInformation[1].BallPositionY = _ballAgentsMock[1].GetBallLocalPosition().y;
        _ballStateInformation[1].BallPositionZ = _ballAgentsMock[1].GetBallLocalPosition().z;
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _ballStateInformation[0].NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        _ballStateInformation[1].BallVelocityX = _ballAgentsMock[1].GetBallVelocity().x;
        _ballStateInformation[1].BallVelocityY = _ballAgentsMock[1].GetBallVelocity().y;
        _ballStateInformation[1].BallVelocityZ = _ballAgentsMock[1].GetBallVelocity().z;
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          Vector3.Distance(BallStateInformation.VelocityRangeMax, BallStateInformation.VelocityRangeMin),
                                                          _ballStateInformation[0].NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[1].PlatformAngleX = _ballAgentsMock[1].GetPlatformAngle().x;
        _ballStateInformation[1].PlatformAngleY = _ballAgentsMock[1].GetPlatformAngle().y;
        _ballStateInformation[1].PlatformAngleZ = _ballAgentsMock[1].GetPlatformAngle().z;
        int angleDistanceBin = 0;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] oldEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsFalse(oldEntry[0][distanceBin][angleDistanceBin].ContainsKey(velocityBin));
        Assert.AreEqual(oldEntry.Length, 1);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        //same agent and same action --> no change in reaction time data
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and different action but no behavioral data available --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioral data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        Assert.IsNotEmpty(newEntry);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioral data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsNotEmpty(newEntry);
        Assert.AreNotEqual(oldEntry[0][distanceBin][angleDistanceBin], newEntry[0][distanceBin][angleDistanceBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Reaction Times", (1 / (double)(_ballStateInformation[0].NumberOfDistanceBins_velocity * _ballStateInformation[0].NumberOfDistanceBins_ballPosition * _ballStateInformation[0].NumberOfAngleBinsPerAxis)) * 100));
    }

    [Test]
    public void ResponseTimeMeasurementTimeRangeTest()
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();
        Dictionary<IBallAgent, bool> isActiveInstanceDict = new Dictionary<IBallAgent, bool>();
        isActiveInstanceDict[_ballAgentsMock[0]] = true;
        isActiveInstanceDict[_ballAgentsMock[1]] = true;
        supervisorAgentMock.Tasks.Returns(new ITask[] { (ITask)_ballAgentsMock[0], (ITask)_ballAgentsMock[1] });

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(false, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        //must be reset otherwise the old ones (created in the SetUp function) would be used s.t. e.g. the new value for numberOfTimeBins would not
        //be considered 
        _ballStateInformation[0].ReactionTimes = null;
        _ballStateInformation[1].ReactionTimes = null;
        _ballStateInformation[0].PerformedActions = null;
        _ballStateInformation[1].PerformedActions = null;

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: supervisorAgentMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfTimeBins: 10,
            supervisorSettings: _supervisorSettings,
        hyperparameters: _hyperparameters
        );

        string path = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation), _behaviourMeasurement.NumberOfTimeBins));

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballStateInformation[1].BallPositionX = _ballAgentsMock[1].GetBallLocalPosition().x;
        _ballStateInformation[1].BallPositionY = _ballAgentsMock[1].GetBallLocalPosition().y;
        _ballStateInformation[1].BallPositionZ = _ballAgentsMock[1].GetBallLocalPosition().z;
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _ballStateInformation[0].NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        _ballStateInformation[1].BallVelocityX = _ballAgentsMock[1].GetBallVelocity().x;
        _ballStateInformation[1].BallVelocityY = _ballAgentsMock[1].GetBallVelocity().y;
        _ballStateInformation[1].BallVelocityZ = _ballAgentsMock[1].GetBallVelocity().z;
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          Vector3.Distance(BallStateInformation.VelocityRangeMax, BallStateInformation.VelocityRangeMin),
                                                          _ballStateInformation[0].NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(-20, -5, -10));
        _ballStateInformation[1].PlatformAngleX = _ballAgentsMock[1].GetPlatformAngle().x;
        _ballStateInformation[1].PlatformAngleY = _ballAgentsMock[1].GetPlatformAngle().y;
        _ballStateInformation[1].PlatformAngleZ = _ballAgentsMock[1].GetPlatformAngle().z;
        int angleDistanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetPlatformAngle(), _ballAgentsMock[1].GetPlatformAngle()),
                                                                      Vector3.Distance(BallStateInformation.AngleRangeMin, BallStateInformation.AngleRangeMax),
                                                                      _ballStateInformation[0].NumberOfDistanceBins_angle);

        Assert.IsTrue(angleDistanceBin != 0);

        DateTime t1 = DateTime.Now;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
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
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioral data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreNotEqual(oldEntry[0][distanceBin][angleDistanceBin], newEntry[0][distanceBin][angleDistanceBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Reaction Times", (1 / (double)(_ballStateInformation[0].NumberOfDistanceBins_velocity * _ballStateInformation[0].NumberOfDistanceBins_ballPosition * _ballStateInformation[0].NumberOfDistanceBins_angle * _behaviourMeasurement.NumberOfTimeBins)) * 100));

        //timebin = 4
        double timeBetweenSwitches = 0.45;
        DateTime t2 = t1.AddSeconds(timeBetweenSwitches);
        Assert.AreEqual(oldEntry[4][distanceBin][angleDistanceBin], newEntry[4][distanceBin][angleDistanceBin]);

        _behaviourMeasurement.UpdateActiveInstance(timeBetweenSwitches);
        System.Threading.Thread.Sleep(10);

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreNotEqual(oldEntry[4][distanceBin][angleDistanceBin], newEntry[4][distanceBin][angleDistanceBin]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)
    }

    [Test]
    public void SuspendedCountMeasurementTest()
    {
        string path = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation)));

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballStateInformation[1].BallPositionX = _ballAgentsMock[1].GetBallLocalPosition().x;
        _ballStateInformation[1].BallPositionY = _ballAgentsMock[1].GetBallLocalPosition().y;
        _ballStateInformation[1].BallPositionZ = _ballAgentsMock[1].GetBallLocalPosition().z;
        //Attention: testing dependency to PositionConverter
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[1].GetBallLocalPosition()),
                                                          _ballAgentsMock[0].GetScale(),
                                                          _ballStateInformation[0].NumberOfDistanceBins_ballPosition);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(3, 0, 3));
        _ballStateInformation[1].BallVelocityX = _ballAgentsMock[1].GetBallVelocity().x;
        _ballStateInformation[1].BallVelocityY = _ballAgentsMock[1].GetBallVelocity().y;
        _ballStateInformation[1].BallVelocityZ = _ballAgentsMock[1].GetBallVelocity().z;
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(_ballAgentsMock[0].GetBallVelocity(), _ballAgentsMock[1].GetBallVelocity()),
                                                          Vector3.Distance(BallStateInformation.VelocityRangeMax, BallStateInformation.VelocityRangeMin),
                                                          _ballStateInformation[0].NumberOfDistanceBins_velocity);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        _ballAgentsMock[1].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[1].PlatformAngleX = _ballAgentsMock[1].GetPlatformAngle().x;
        _ballStateInformation[1].PlatformAngleY = _ballAgentsMock[1].GetPlatformAngle().y;
        _ballStateInformation[1].PlatformAngleZ = _ballAgentsMock[1].GetPlatformAngle().z;
        int angleDistanceBin = 0;

        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] oldEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsFalse(oldEntry[0][distanceBin][angleDistanceBin].ContainsKey(velocityBin));
        Assert.AreEqual(oldEntry.Length, 1);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        //same agent and same action --> no change in reaction time data
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and different action but no behavioral data available --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0.5f, 0.5f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Discard reaction time measurement (no behavioral data available)!");
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        Assert.IsNotEmpty(newEntry);
        Assert.AreEqual(oldEntry, newEntry);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioral data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.IsNotEmpty(newEntry);
        
        Assert.AreEqual(1, newEntry[0][distanceBin][angleDistanceBin][velocityBin].Item2.Item1);

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioral data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent and behavioral data available but action in unusual range --> no change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 0f, 0f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, "Suspend reaction time measurement (action in unusual range)!");
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1) (no update since updates of behavioral data only happens when reaction time measurement was not suspended)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (0.5, 0.5)

        //different agent, behavioral data available and action in usual range --> change in reaction time data
        actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveReactionTimeToJSON();
        newEntry = LoadDataFromJSON4D<(int, double, double)>(path);
        Assert.AreEqual(3, newEntry[0][distanceBin][angleDistanceBin][velocityBin].Item2.Item1);
    }

    [Test]
    public void BehavioralDataMeasurementTest()
    {
        string path = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, "BSI", _ballStateInformation[0].BehaviorDimensions);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        //Attention: testing dependency to PositionConverter
        int ballBin = PositionConverter.SquareCoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale()/2, _ballStateInformation[0].NumberOfAreaBinsPerDirection);
        
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z; 
        int velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _velocityRangeVector, _ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, BallStateInformation.VelocityRangeMin);
        
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        int rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _angleRangeVector, _ballStateInformation[0].NumberOfAngleBinsPerAxis, BallStateInformation.AngleRangeMin);

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(1, 0, 1), new Vector3(1, 0, 1))), entry[ballBin][rangeBin][velocityBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Behavioral Data", (1 / (double)((int)Math.Pow(_ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, 3) * (int)Math.Pow(_ballStateInformation[0].NumberOfAreaBinsPerDirection, 2) * (int)Math.Pow(_ballStateInformation[0].NumberOfAngleBinsPerAxis, 3))) * 100));

        actionBuffers = new ActionBuffers(new float[] { 4f, 4f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((2, (new Vector3(5, 0, 5), new Vector3(17, 0, 17))), entry[ballBin][rangeBin][velocityBin]);

        actionBuffers = new ActionBuffers(new float[] { 3f, 6.025f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((3, (new Vector3(11.025f, 0f, 8f), new Vector3(53.300625f, 0f, 26f))), entry[ballBin][rangeBin][velocityBin]);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(3.5f, 0, 2));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        ballBin = PositionConverter.SquareCoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, _ballStateInformation[0].NumberOfAreaBinsPerDirection);
        
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(-1, 0, 1.5f));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _velocityRangeVector, _ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, BallStateInformation.VelocityRangeMin);
        
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(200, 50, 100));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _angleRangeVector, _ballStateInformation[0].NumberOfAngleBinsPerAxis, BallStateInformation.AngleRangeMin);

        actionBuffers = new ActionBuffers(new float[] { 2f, 2f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(2, 0, 2), new Vector3(4, 0, 4))), entry[ballBin][rangeBin][velocityBin]);
        LogAssert.Expect(LogType.Log, String.Format("{0:N4} % Behavioral Data", (2 / (double)((int)Math.Pow(_ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, 3) * (int)Math.Pow(_ballStateInformation[0].NumberOfAreaBinsPerDirection, 2) * (int)Math.Pow(_ballStateInformation[0].NumberOfAngleBinsPerAxis, 3))) * 100));
        Assert.AreEqual(4, _behaviourMeasurement.ActionCount);
    }

    [Test]
    public void BehavioralDataMeasurementHighPrecisionTest()
    {
        string path = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, "BSI", _ballStateInformation[0].BehaviorDimensions);

        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        //Attention: testing dependency to PositionConverter
        int ballBin = PositionConverter.SquareCoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, _ballStateInformation[0].NumberOfAreaBinsPerDirection);
        
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        int velocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _velocityRangeVector, _ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, BallStateInformation.VelocityRangeMin);
        
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        int rangeBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _angleRangeVector, _ballStateInformation[0].NumberOfAngleBinsPerAxis, BallStateInformation.AngleRangeMin);

        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1.025f, 1.005f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entry = LoadDataFromJSON3D<(Vector3, Vector3)>(path);
        Assert.AreEqual((1, (new Vector3(1.005f, 0f, 1.025f), new Vector3(1.010025f, 0f, 1.050625f))), entry[ballBin][rangeBin][velocityBin]);
    }

    [Test]
    public void NonImmediateActionTest()
    {
        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(0, 0, 0));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        _ballAgentsMock[1].GetBallLocalPosition().Returns(new Vector3(1, 1, 1));
        _ballStateInformation[1].BallPositionX = _ballAgentsMock[1].GetBallLocalPosition().x;
        _ballStateInformation[1].BallPositionY = _ballAgentsMock[1].GetBallLocalPosition().y;
        _ballStateInformation[1].BallPositionZ = _ballAgentsMock[1].GetBallLocalPosition().z;
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        _ballAgentsMock[1].GetBallVelocity().Returns(new Vector3(1, 0, 1));
        _ballStateInformation[1].BallVelocityX = _ballAgentsMock[1].GetBallVelocity().x;
        _ballStateInformation[1].BallVelocityY = _ballAgentsMock[1].GetBallVelocity().y;
        _ballStateInformation[1].BallVelocityZ = _ballAgentsMock[1].GetBallVelocity().z;


        //first call --> no change in reaction time data
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 1f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: nan

        _behaviourMeasurement.UpdateActiveInstance(0);
        System.Threading.Thread.Sleep(10);

        actionBuffers = new ActionBuffers(new float[] { 1f, 0.9f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[1]);
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[0]][velocityBin_BehavioralData]: (1, 1)
        //updated ActionSetPerBinBehavioralData[ballBin of _ballAgentsMock[1]][velocityBin_BehavioralData]: (1, 1)

        _behaviourMeasurement.UpdateActiveInstance(0);

        System.Threading.Thread.Sleep(100);

        actionBuffers = new ActionBuffers(new float[] { 0.9f, 1f }, new int[0]);
        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        LogAssert.Expect(LogType.Log, new Regex("Decision Time: 1+"));
    }

    [Test]
    public void UpdateExistingModelBehaviorTest()
    {
        _ballStateInformation[0].NumberOfDistanceBins_angle = _ballStateInformation[1].NumberOfDistanceBins_angle = 125;

        string workingDirectory = Application.dataPath;
        string absolutePath = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting_b.json");
        string absolutePathTemp = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp_b.json");
        File.Delete(absolutePathTemp);
        File.Copy(absolutePath, absolutePathTemp);

        string absolutePathReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExisting_rt.json");
        string absolutePathTempReactionTime = Path.Combine(workingDirectory, "Tests", "MeasurementTests", "testUpdateExistingTemp_rt.json");
        File.Delete(absolutePathTempReactionTime);
        File.Copy(absolutePathReactionTime, absolutePathTempReactionTime);

        string fileNameForBehavioralData = Path.Combine("..", "..", "Assets", "Tests", "MeasurementTests", "testUpdateExistingTemp_b.json");

        string behaviorPath = Util.GetBehavioralDataPath(fileNameForBehavioralData, _supervisorSettings, _hyperparameters, "BSI", _ballStateInformation[0].BehaviorDimensions);
        string reactionTimePath = Util.GetReactionTimeDataPath(fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation)));

        File.Delete(behaviorPath);
        File.Delete(reactionTimePath);

        //must be reset otherwise the old ones (created in the SetUp function) would be used s.t. e.g. the new value for numberOfTimeBins would not
        //be considered 
        _ballStateInformation[0].ReactionTimes = null;
        _ballStateInformation[1].ReactionTimes = null;
        _ballStateInformation[0].PerformedActions = null;
        _ballStateInformation[1].PerformedActions = null;

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: _supervisorAgentMock,
            updateExistingModelBehavior: true,
            fileNameForBehavioralData: fileNameForBehavioralData,
            numberOfTimeBins: 1
        );


        //check loading
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        Dictionary<int, (int, (Vector3, Vector3))>[][] entryBehavioural = LoadDataFromJSON3D<(Vector3, Vector3)>(behaviorPath);

        int[] ballBins = new int[] { 100, 110, 0 };
        int[] angleBins = new int[] { 60, 90, 0 };
        int[] velocityBins = new int[] { 200, 100, 0 };

        Assert.AreEqual((2, (new Vector3(5f, 0f, 5f), new Vector3(17f, 0f, 17f))), entryBehavioural[ballBins[0]][angleBins[0]][velocityBins[0]]);
        Assert.AreEqual((1, (new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 4f))), entryBehavioural[ballBins[1]][angleBins[1]][velocityBins[1]]);
        Assert.AreEqual(3, _behaviourMeasurement.ActionCount);
        Assert.IsFalse(entryBehavioural[ballBins[2]][angleBins[2]].ContainsKey(velocityBins[2]));

        _behaviourMeasurement.SaveReactionTimeToJSON();
        Dictionary<int, (int, (int, double, double))>[][][] entryReactionTimes = LoadDataFromJSON4D<(int, double, double)>(reactionTimePath);

        int[] distanceBins = new int[] { 7, 0 };
        int[] angleBinsTime = new int[] { 70, 0 };
        int[] velocityBins_reaction = new int[] { 8, 0 };

        Assert.AreEqual((2, (2, 10.9695, 120.32993025)), entryReactionTimes[0][distanceBins[0]][angleBinsTime[0]][velocityBins_reaction[0]]);
        Assert.IsFalse(entryReactionTimes[0][distanceBins[1]][angleBinsTime[1]].ContainsKey(velocityBins[1]));


        //check updating
        _ballAgentsMock[0].GetBallLocalPosition().Returns(new Vector3(3.5f, 0, -3.5f));
        _ballStateInformation[0].BallPositionX = _ballAgentsMock[0].GetBallLocalPosition().x;
        _ballStateInformation[0].BallPositionY = _ballAgentsMock[0].GetBallLocalPosition().y;
        _ballStateInformation[0].BallPositionZ = _ballAgentsMock[0].GetBallLocalPosition().z;
        int newBallBin = PositionConverter.SquareCoordinatesToBin(_ballAgentsMock[0].GetBallLocalPosition(), _ballAgentsMock[0].GetScale() / 2, _ballStateInformation[0].NumberOfAreaBinsPerDirection);
        _ballAgentsMock[0].GetPlatformAngle().Returns(new Vector3(20, 5, 10));
        _ballStateInformation[0].PlatformAngleX = _ballAgentsMock[0].GetPlatformAngle().x;
        _ballStateInformation[0].PlatformAngleY = _ballAgentsMock[0].GetPlatformAngle().y;
        _ballStateInformation[0].PlatformAngleZ = _ballAgentsMock[0].GetPlatformAngle().z;
        int newAngleBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetPlatformAngle(), _angleRangeVector, _ballStateInformation[0].NumberOfAngleBinsPerAxis, BallStateInformation.AngleRangeMin);
        _ballAgentsMock[0].GetBallVelocity().Returns(new Vector3(3.5f, 0, -3.5f));
        _ballStateInformation[0].BallVelocityX = _ballAgentsMock[0].GetBallVelocity().x;
        _ballStateInformation[0].BallVelocityY = _ballAgentsMock[0].GetBallVelocity().y;
        _ballStateInformation[0].BallVelocityZ = _ballAgentsMock[0].GetBallVelocity().z;
        int newVelocityBin = PositionConverter.RangeVectorToBin(_ballAgentsMock[0].GetBallVelocity(), _velocityRangeVector, _ballStateInformation[0].NumberOfBallVelocityBinsPerAxis, BallStateInformation.VelocityRangeMin);
        ActionBuffers actionBuffers = new ActionBuffers(new float[] { 0.025f, 0.025f }, new int[0]);

        _behaviourMeasurement.CollectData(actionBuffers, (ITask)_ballAgentsMock[0]);
        _behaviourMeasurement.SaveBehavioralDataToJSON();
        entryBehavioural = LoadDataFromJSON3D<(Vector3, Vector3)>(behaviorPath);
        Assert.AreEqual((2, (new Vector3(5f, 0f, 5f), new Vector3(17f, 0f, 17f))), entryBehavioural[ballBins[0]][angleBins[0]][velocityBins[0]]);
        Assert.AreEqual((1, (new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 4f))), entryBehavioural[ballBins[1]][angleBins[1]][velocityBins[1]]);
        Assert.IsFalse(entryBehavioural[ballBins[2]][angleBins[2]].ContainsKey(velocityBins[2]));
        Assert.AreEqual((1, (new Vector3(0.025f, 0, 0.025f), new Vector3(0.000625f, 0, 0.000625f))).ToString(), entryBehavioural[newBallBin][newAngleBin][newVelocityBin].ToString());
        Assert.AreEqual(4, _behaviourMeasurement.ActionCount);


        File.Delete(absolutePathTemp);
        File.Delete(absolutePathTempReactionTime);
        File.Delete(behaviorPath);
        File.Delete(reactionTimePath);
    }

    [Test]
    public void ConvertRawToBinDataTest()
    {
        ISupervisorAgentRandom supervisorAgentMock = Substitute.For<Supervisor.ISupervisorAgentRandom>();
        supervisorAgentMock.Tasks.Returns(new ITask[] { (ITask)_ballAgentsMock[0], (ITask)_ballAgentsMock[1] });

        float decisionRequestIntervalInSeconds = 0.5f;
        float decisionRequestIntervalRangeInSeconds = 1f;
        supervisorAgentMock.DecisionRequestIntervalInSeconds.Returns(decisionRequestIntervalInSeconds);
        supervisorAgentMock.DecisionRequestIntervalRangeInSeconds.Returns(decisionRequestIntervalRangeInSeconds);
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        string path = Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters);
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            Debug.Log(String.Format("Could not delete path {0}.", path));
        }

        List<BehavioralData> behaviouralDataList = new List<BehavioralData>()
        {
            new BehavioralData(0f,0f,-9080, -9082, new BallStateInformation(-0.772119045f,2.11634541f,-1.47088289f,-0.103927992f,0.08879531f,-0.24924694f,19.579464f,3.07926488f,1.02385044f), new BallStateInformation(-0.8877549f,1.54776f,0.452500343f,0.6505743f,-0.242208049f,1.18963778f,5.809886f,1.47117662f,355.133f),1193,898),
            new BehavioralData(0f,0f,-9080, -9082, new BallStateInformation(-0.7741637f,2.11779165f,-1.47494221f,-0.10237778f,0.07229686f,-0.2029278f,19.579464f,3.07926488f,1.02385044f),new BallStateInformation(-0.8746319f,1.54318058f,0.476318836f,0.6561387f,-0.228972211f,1.19093513f,5.714787f,1.45120084f,355.211029f),1209,898),
            new BehavioralData(0f,0f,-9080, -9082, new BallStateInformation(-0.7762127f,2.11890721f,-1.47807288f,-0.102459513f,0.05581169f,-0.156645313f,19.579464f,3.07926488f,1.02385044f),new BallStateInformation(-0.861414433f,1.53873456f,0.5001459f,0.660860062f,-0.222298667f,1.19134736f,5.62117529f,1.43140817f,355.287842f),1226,898),
            new BehavioralData(-0.0898087844f,-0.186637625f,-9080, -9082, new BallStateInformation(-0.7782321f,2.11970162f,-1.48030233f,-0.101036459f,0.0397059023f,-0.111428484f,19.579464f,3.07926488f,1.02385044f),new BallStateInformation(-0.8481078f,1.53441882f,0.5239763f,0.665314734f,-0.215789273f,1.19151735f,5.432421f,1.39110792f,355.4429f),1261,898),
            new BehavioralData(-0.322198033f,-0.322198033f,-9082, -9080, new BallStateInformation(-0.834771633f,1.529644f,0.547748566f,0.666795135f,-0.2387381f,1.18861449f,5.432421f,1.39110792f,355.4429f),new BallStateInformation(-0.7802205f,2.11662173f,-1.48249531f,-0.09941988f,-0.1539902f,-0.109645635f,19.0838432f,3.051933f,0.8332048f),11,1265), //switch
            new BehavioralData(-0.457758456f,-0.4190269f,-9082, -9080, new BallStateInformation(-0.8200979f,1.538354f,0.572557449f,0.6780339f,-0.160401553f,1.19188726f,4.79067659f,1.44973135f,354.803741f),new BallStateInformation(-0.782177f,2.10973f,-1.48465323f,-0.09782916f,-0.344587147f,-0.107891306f,18.9615078f,3.03046966f,0.824173748f),28,1265),
            new BehavioralData(-0.5449044f,-0.4771242f,-9082, -9080, new BallStateInformation(-0.8042345f,1.55518639f,0.597606659f,0.6846782f,-0.153289f,1.186536f,3.95738387f,1.53916764f,353.89505f),new BallStateInformation(-0.7841277f,2.10202765f,-1.4869051f,-0.0975532457f,-0.385002971f,-0.112926245f,18.8374443f,3.00874186f,0.81507206f),44,1265),
            new BehavioralData(-0.5642702f,-0.4771242f,-9082, -9080, new BallStateInformation(-0.78712225f,1.577621f,0.6224141f,0.6935924f,-0.146248549f,1.1783663f,3.01064634f,1.65883946f,352.8125f),new BallStateInformation(-0.7860403f,2.09664f,-1.4874301f,-0.09563974f,-0.2693452f,-0.026355803f,18.7156658f,2.98745f,0.8061929f),63,1265),
        };
        

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: supervisorAgentMock,
            updateExistingModelBehavior: false,
            isRawDataCollected: true,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfTimeBins: 1,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings,
            hyperparameters: _hyperparameters
        );

        Dictionary<IBallAgent, bool> isActiveInstanceDict = new Dictionary<IBallAgent, bool>();

        IBallAgent ballAgentMockTarget;
        IBallAgent ballAgentMockSource;

        for (int i = 0; i <behaviouralDataList.Count; i++)
        {
            foreach (IBallAgent ballAgent in _ballAgentsMock)
            {
                isActiveInstanceDict[ballAgent] = false;
            }

            if (behaviouralDataList[i].TargetTaskId == -9080)
            {
                ballAgentMockTarget = _ballAgentsMock[0];
                ballAgentMockSource = _ballAgentsMock[1];
            }
            else
            {
                ballAgentMockTarget = _ballAgentsMock[1];
                ballAgentMockSource = _ballAgentsMock[0];
            }

            isActiveInstanceDict[ballAgentMockTarget] = true;
            supervisorAgentMock.GetActiveTaskNumber().Returns(behaviouralDataList[i].TargetTaskId);
            supervisorAgentMock.GetPreviousActiveTaskNumber().Returns(behaviouralDataList[i].SourceTaskId);
            //TODO: supervisorAgentMock.IsActiveInstanceDict.Returns(isActiveInstanceDict);

            BallStateInformation targetState = (BallStateInformation)behaviouralDataList[i].TargetState;
            BallStateInformation sourceState = (BallStateInformation)behaviouralDataList[i].SourceState;

            ((ITask)ballAgentMockTarget).StateInformation.UpdateStateInformation(targetState);
            ((ITask)ballAgentMockSource).StateInformation.UpdateStateInformation(sourceState);

            ActionBuffers actionBuffers = new ActionBuffers(new float[] { behaviouralDataList[i].ActionZ, behaviouralDataList[i].ActionX }, new int[0]);

            _behaviourMeasurement.CollectData(actionBuffers, (ITask)ballAgentMockTarget);

            if(i+1 < behaviouralDataList.Count && behaviouralDataList[i].TargetTaskId != behaviouralDataList[i+1].TargetTaskId)
            {
                _behaviourMeasurement.UpdateActiveInstance(behaviouralDataList[i+1].TimeBetweenSwitches);
            }
        }

        _behaviourMeasurement.SaveBehavioralDataToJSON();
        _behaviourMeasurement.SaveReactionTimeToJSON();
        _behaviourMeasurement.SaveRawBehavioralDataToCSV();

        string rawBehavioralDataPath = Util.GetRawBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters);
        string behavioralDataPath = Util.GetBehavioralDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, "BSI", _ballStateInformation[0].BehaviorDimensions);
        string reactionTimeDataPath = Util.GetReactionTimeDataPath(_fileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(typeof(BallStateInformation), typeof(BallStateInformation)), _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation), _behaviourMeasurement.NumberOfTimeBins));

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
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds,
            setConstantDecisionRequestInterval = false
        };

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfTimeBins = 1
        };

        BehaviorMeasurementConverter.ConvertRawToBinData(supervisorSettings, _hyperparameters, behavioralDataCollectionSettings, rawBehavioralDataPath);

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
        supervisorAgentMock.Tasks.Returns(new ITask[]{ (ITask)_ballAgentsMock[0], (ITask)_ballAgentsMock[1]});
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: supervisorAgentMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfTimeBins: 1,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings,
            hyperparameters: _hyperparameters
        );

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins,
        };

        SupervisorSettings supervisorSettings = new SupervisorSettings
        {
            randomSupervisor = true,
            decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds,
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds,
            setConstantDecisionRequestInterval = false
        };

        string workingDirectory = Util.GetWorkingDirectory();
        string dataPath = Path.Combine(workingDirectory, "Assets", "Tests", "MeasurementTests", "CDTDRI0.8R1GD0.8NDFDII100DP5BD100BDF1S0.1RS7.5");
        string rawBehavioralDataPath = Path.Combine(dataPath, path.Item1);
        string behavioralDataPath = Path.Combine(dataPath, path.Item2);
        string reactionTimePath = Path.Combine(dataPath, path.Item3);

        Dictionary<int, (int, (Vector3, Vector3))>[][] realBehaviouralData = LoadDataFromJSON3D<(Vector3, Vector3)>(behavioralDataPath);
        Dictionary<int, (int, (int, double, double))>[][][] realReactionTime = LoadDataFromJSON4D<(int, double, double)>(reactionTimePath);

        BehaviorMeasurementConverter.ConvertRawToBinData(supervisorSettings, _hyperparameters, behavioralDataCollectionSettings, rawBehavioralDataPath);

        string resultBehavioralDataPath = Util.ConvertRawPathToBehavioralDataPath(rawBehavioralDataPath, _ballStateInformation[0].BehaviorDimensions, supervisorSettings, "BSI");
        string resultReactionTimeDataPath = Util.ConvertRawPathToReactionTimeDataPath(rawBehavioralDataPath, _ballStateInformation[0].GetRelationalDimensions(typeof(BallStateInformation), _behaviourMeasurement.NumberOfTimeBins), supervisorSettings, "BSI");

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
        supervisorAgentMock.Tasks.Returns(new ITask[] { (ITask)_ballAgentsMock[0], (ITask)_ballAgentsMock[1] });
        _supervisorSettings = new SupervisorSettings(true, false, decisionRequestIntervalInSeconds, decisionRequestIntervalRangeInSeconds, 0, 0, 0);

        _behaviourMeasurement = new BehaviorMeasurement(
            supervisorAgent: supervisorAgentMock,
            updateExistingModelBehavior: false,
            fileNameForBehavioralData: this._fileNameForBehavioralData,
            numberOfTimeBins: 1,
            maxNumberOfActions: 0,
            supervisorSettings: _supervisorSettings,
            hyperparameters: _hyperparameters
        );

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = _behaviourMeasurement.UpdateExistingModelBehavior,
            fileNameForBehavioralData = _behaviourMeasurement.FileNameForBehavioralData,
            numberOfTimeBins = _behaviourMeasurement.NumberOfTimeBins
        };

        SupervisorSettings supervisorSettings = new SupervisorSettings
        {
            randomSupervisor = true,
            decisionRequestIntervalInSeconds = decisionRequestIntervalInSeconds,
            decisionRequestIntervalRangeInSeconds = decisionRequestIntervalRangeInSeconds,
            setConstantDecisionRequestInterval = false
        };

        string workingDirectory = Util.GetWorkingDirectory();
        string dataPath = Path.Combine(workingDirectory, "Assets", "Tests", "MeasurementTests", "CDTDRI0.8R1GD0.8NDFDII100DP5BD100BDF1S0.1RS7.5");
        string rawBehavioralDataPath = Path.Combine(dataPath, "ConverterPerformanceTestraw.csv");

        Measure.Method(() => BehaviorMeasurementConverter.ConvertRawToBinData(supervisorSettings, _hyperparameters, behavioralDataCollectionSettings, rawBehavioralDataPath))
            .MeasurementCount(5)
            .Run();
    }


    private Dictionary<int, (int, T)>[][] LoadDataFromJSON3D<T>(string path)
    {
        //array[ballBin]<velocityBin, entry>
        string json = File.ReadAllText(path);

        //array[ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][]>(json);

        return entry;
    }


    private Dictionary<int, (int, T)>[][][] LoadDataFromJSON4D<T>(string path)
    {
        //array[ballBin]<velocityBin, entry>
        string json = File.ReadAllText(path);

        //array[ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][][]>(json);

        return entry;
    }
}
