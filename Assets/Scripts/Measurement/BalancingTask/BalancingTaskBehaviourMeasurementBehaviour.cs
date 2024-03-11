using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using UnityEngine.Assertions;
using Newtonsoft.Json;
using Supervisor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading;

//Core functions are separated for testing purpose. MonoBehaviours cannot be created without a Gameobject. Therfore, testing those classes is hard.
//The Humble Object pattern separates the core functionality into an own class which can be created with the help of the new keyword and therefore
//this class can be tested without a Game Object.
public class BalancingTaskBehaviourMeasurementBehaviour : MonoBehaviour
{
    [field: ProjectAssign(Hide = true)]
    public bool IsAbcSimulation { get; set; }

    [field: ProjectAssign(Hide = true)]
    public int SampleSize { get; set; }

    [field: ProjectAssign(Hide = true)]
    public int SimulationId { get; set; }

    [field: SerializeField, Tooltip("Save the average action set by the model based on the specified NumberOfAreaBins and " +
    "NumberOfBallVelocityBinsPerAxis to CSV."), ProjectAssign]
    public bool SaveBehavioralData { get; set; }

    [field: SerializeField, Tooltip("Updates the saved model behavioural data of the given file."), ProjectAssign]
    public bool UpdateExistingModelBehavior { get; set; }

    [field: SerializeField, Header("Relation Difference to Reaction Time"), Tooltip("Number of bins in which the active time of a platform should " +
    "be divided. The time bins are used to calculate the relation how the reaction time is related to the time a platform was active."), ProjectAssign(Header = "Relation Difference to Reaction Time")]
    public int NumberOfTimeBins { set; get; }

    [field: SerializeField, Tooltip("Behavioral Data is saved to Scores/{File Name}"), ProjectAssign]
    public string FileNameForBehavioralData { get; set; } = "behavioralData.csv";

    [field: SerializeField, Tooltip("The raw data entries are saved additionally to the bin data."), ProjectAssign]
    public bool IsRawDataCollected { get; set; } = true;

    [field: SerializeField, Header("Behavioral Data"), Tooltip("Number of bins for which the behavioral data should be collected."), ProjectAssign(Header = "Behavioral Data")]
    public int NumberOfAreaBins_BehavioralData { get; set; } = 225;

    [field: SerializeField, Tooltip("Number of bins in which the angle of the platform should be divided for the behavioral data."), ProjectAssign]
    public int NumberOfAngleBinsPerAxis { get; set; } = 5;

    [field: SerializeField, Tooltip("Number of bins in which the velocity should be divided for the behavioral data."), ProjectAssign]
    public int NumberOfBallVelocityBinsPerAxis_BehavioralData { get; set; } = 6;

    [field: SerializeField, Tooltip("Number of bins in which the distance should be divided. The " +
        "distance bins are used to calculate the relation how the reaction time is related to the distance between the two balls in case of a task " +
        "switch. "), ProjectAssign]
    public int NumberOfDistanceBins { get; set; } = 12;

    [field: SerializeField, Tooltip("Number of bins in which the distance of the ball velocities should be divided"), ProjectAssign]
    public int NumberOfDistanceBins_velocity { get; set; } = 12;

    [field: SerializeField, Tooltip("Reaction time is set only until the next action set by the player is in the usual action bin. Therefore, if a " +
        "player sets an action which is unusual due the task switch (maybe due to the stress of the new situation) the reaction time is not saved " +
        "until the normal behavior can be seen."), ProjectAssign]
    public int NumberOfActionBinsPerAxis { get; set; } = 5;

    [field: SerializeField, Tooltip("Collects behavioral data until the maximum number of actions is reached."), ProjectAssign]
    public int MaxNumberOfActions { get; set; }

    [field: SerializeField, Header("Comparison Data Collection"), Tooltip("Collects behavioral data until there is an entry for every bin of the " +
        "comparisonFileName or a certain time limit has been exceeded without any procress."), ProjectAssign(Header = "Comparison Data Collection")]
    public bool CollectDataForComparison { get; set; }

    [field: SerializeField, Tooltip("Filename for the behavioral data and the reaction time (use the name for the behavioral data). The reaction time" +
        " file must be in the same directory."), ProjectAssign]
    public string ComparisonFileName { get; set; }

    [field: SerializeField, Tooltip("Collects behavioral data until no new entry was collected for comparisonTimeLimit seconds."), ProjectAssign]
    public int ComparisonTimeLimit { get; set; } = 30;


    internal SupervisorAgent SupervisorAgent { get; private set; }

    internal BallAgent[] BallAgents { get; private set; }

    [field: SerializeField]
    internal Text ProportionCollectedBehavioralData { get; set; }

    [field: SerializeField]
    internal Text Actions { get; set; }

    [field: SerializeField]
    internal Text ProportionCollectedReactionTimes { get; set; }

    internal float MinTime { get; set; }

    internal float MaxTime { get; set; }


    private BalancingTaskBehaviourMeasurement _behaviourMeasurement;


    public void FixedUpdate()
    {
        if (SaveBehavioralData)
        {
            _behaviourMeasurement.CollectionTimerBehavioralData += Time.fixedDeltaTime;
            _behaviourMeasurement.CollectionTimerResponseTime += Time.fixedDeltaTime;
        }
    }


    private void OnEnable()
    {
        if (SaveBehavioralData)
        {
            BallAgent.OnAction += _behaviourMeasurement.CollectData;
            SupervisorAgent.OnTaskSwitchCompleted += _behaviourMeasurement.UpdateActiveInstance;
            SupervisorAgent.EndEpisodeEvent += _behaviourMeasurement.ResetMeasurement;

            if (CollectDataForComparison && ComparisonTimeLimit > 0)
            {
                BallAgent.OnAction += _behaviourMeasurement.CheckTimeLimit;
            }
        }
    }

    private void OnDisable()
    {
        if (SaveBehavioralData)
        {
            BallAgent.OnAction -= _behaviourMeasurement.CollectData;
            SupervisorAgent.OnTaskSwitchCompleted -= _behaviourMeasurement.UpdateActiveInstance;
            SupervisorAgent.EndEpisodeEvent -= _behaviourMeasurement.ResetMeasurement;

            if (CollectDataForComparison && ComparisonTimeLimit > 0)
            {
                BallAgent.OnAction -= _behaviourMeasurement.CheckTimeLimit;
            }

            _behaviourMeasurement.SaveReactionTimeToCSV();

            if (!IsAbcSimulation)
            {
                _behaviourMeasurement.SaveBehavioralDataToJSON();
                _behaviourMeasurement.SaveReactionTimeToJSON();
                if (IsRawDataCollected)
                {
                    _behaviourMeasurement.SaveRawBehavioralDataToCSV();
                }
            }
            else
            {
                string name = Path.GetFileNameWithoutExtension(FileNameForBehavioralData) + ".json";
                _behaviourMeasurement.SaveRawBehavioralDataToCSV(SimulationId);
                _behaviourMeasurement.SaveBehavioralDataToJSON(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}_{2}", SimulationId, "behavior", name)));
                _behaviourMeasurement.SaveReactionTimeToJSON(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}_{2}", SimulationId, "rt", name)));
            }

            Debug.Log(String.Format("Total number of actions collected: {0}", _behaviourMeasurement.ActionCount));
        }
    }

    private void Awake()
    {
        if (SaveBehavioralData)
        {
            SupervisorAgent = SupervisorAgent.GetSupervisor(gameObject);

            BallAgents = SupervisorAgent.GetBallAgents();

            _behaviourMeasurement = new BalancingTaskBehaviourMeasurement(this);
        }

        LogToFile.LogPropertiesFieldsOfComponent(this);
    }
}


public class BalancingTaskBehaviourMeasurement
{
    public Supervisor.ISupervisorAgent SupervisorAgent { private set; get; }

    public IBallAgent[] BallAgents { private set; get; }

    public bool SaveBehavioralData { private set; get; }

    public bool UpdateExistingModelBehavior { private set; get; }

    public string FileNameForBehavioralData { private set; get; }

    public int NumberOfAreaBins_BehavioralData { private set; get; }

    public int NumberOfAngleBinsPerAxis { get; private set; }

    public int NumberOfBallVelocityBinsPerAxis_BehavioralData { private set; get; }

    public int NumberOfVelocityBins_BehavioralData { private set; get; }

    public int NumberOfAngleBins { private set; get; }

    public int NumberOfTimeBins { private set; get; }

    public int NumberOfDistanceBins_ballPosition { private set; get; }

    public int NumberOfDistanceBins_velocity { private set; get; }

    public int NumberOfActionBinsPerAxis { private set; get; }

    public int NumberOfActionBins { private set; get; }

    public bool CollectDataForComparison { private set; get; }

    public string ComparisonFileName { private set; get; }

    public int ComparisonTimeLimit { private set; get; }

    public int MaxNumberOfActions { private set; get; }

    //result of experiment: (3.32, 1.47, 3.52) (only valid for the standard parameters of the platfrom size, etc.)
    public Vector3 VelocityRangeMax { private set; get; } = new Vector3(4f, 2f, 4f);

    //result of experiment: (-3.45, -10.00, -3.51) (only valid for the standard parameters of the platfrom size, etc.)
    public Vector3 VelocityRangeMin { private set; get; } = new Vector3(-4f, -11f, -4f);

    public Vector3 AngleRangeMax { private set; get; } = new Vector3(360.1f, 360.1f, 360.1f);

    public Vector3 AngleRangeMin { private set; get; } = new Vector3(-0.1f, -0.1f, -0.1f);

    //action range is between 1 and -1
    public Vector3 ActionRangeMax { private set; get; } = new Vector3(1, 0, 1);

    public Vector3 ActionRangeMin { private set; get; } = new Vector3(-1, 0, -1);

    public int ActionCount { private set; get; }

    public Vector3 VelocityRangeVector { get; set; }

    public Vector3 AngleRangeVector { get; set; }

    public Vector3 ActionRangeVector { get; set; }

    public bool IsRawDataCollected { get; set; }

    public bool IsSimulation { get; set; }

    public bool IsAbcSimulation { get; set; }

    public int SampleSize { get; set; }

    public int SimulationId { get; set; }


    internal float CollectionTimerBehavioralData { set; get; }

    internal float CollectionTimerResponseTime { set; get; }


    private Text _proportionCollectedBehavioralDataText;

    private Text _actionsText;

    private Text _proportionCollectedReactionTimesText;

    //arrray[ballBin][angleBin]<velocityBin, (count, (Sum(ActionVector), Sum(ActionVector^2)))>
    private Dictionary<int, (int, (Vector3, Vector3))>[][] _actionSetPerBinBehavioralData;

    //arrray[distanceBin][angleBin]<velocityBin, (count, (SuspendedCount, Sum(ReactionTime), Sum(ReactionTime^2)))>
    private Dictionary<int, (int, (int, double, double))>[][][] _reactionTimeInMsDistanceVelocityRelation;

    //arrray[ballBin][angleBin][velocityBin]
    private bool[][][] _actionSetPerBinBehavioralDataComparison;

    //arrray[distanceBin][angleBin][velocityBin]
    private bool[][][][] _reactionTimeInMsDistanceVelocityRelationComparison;

    private int _areaBinCount;

    private int _distanceBinCount;

    private float[] _lastCallsContinuousActions;

    private IBallAgent _previousActiveAgent;

    private IBallAgent _activeAgent;

    private bool _isReactionTimeMeasurementActive;

    private float _platformRadius;

    private int _numberOFBinsPerDirection;

    private int _totalNumberBehavioralDataComparison;

    private int _totalNumberResponseTimeDataComparison;

    private int _areaBinComparisonCount;

    private int _distanceComparisonBinCount;

    private double _timeBetweenSwitches;

    private BehavioralDataCollectionSettings _behavioralDataCollectionSettings;

    private SupervisorSettings _supervisorSettings;

    private BalancingTaskSettings _balancingTaskSettings;

    private List<BehaviouralData> _behaviouralDataList;

    private List<ReactionTime> _reactionTimeList;

    private float _minTime;

    private float _maxTime;

    private Stopwatch _stopWatchLastSwitch = new();

    private int _suspendedReactionTimeCount;

    private int _switchCount = 0;

    private int _reactionTimeMeasurementCount = 0;


    public BalancingTaskBehaviourMeasurement(BalancingTaskBehaviourMeasurementBehaviour behaviourMeasurementBehaviour)
    {
        SupervisorAgent = behaviourMeasurementBehaviour.SupervisorAgent;
        BallAgents = behaviourMeasurementBehaviour.BallAgents;
        SaveBehavioralData = behaviourMeasurementBehaviour.SaveBehavioralData;
        UpdateExistingModelBehavior = behaviourMeasurementBehaviour.UpdateExistingModelBehavior;
        FileNameForBehavioralData = behaviourMeasurementBehaviour.FileNameForBehavioralData;
        NumberOfAreaBins_BehavioralData = behaviourMeasurementBehaviour.NumberOfAreaBins_BehavioralData;
        NumberOfBallVelocityBinsPerAxis_BehavioralData = behaviourMeasurementBehaviour.NumberOfBallVelocityBinsPerAxis_BehavioralData;
        NumberOfAngleBinsPerAxis = behaviourMeasurementBehaviour.NumberOfAngleBinsPerAxis;
        _proportionCollectedBehavioralDataText = behaviourMeasurementBehaviour.ProportionCollectedBehavioralData;
        NumberOfTimeBins = behaviourMeasurementBehaviour.NumberOfTimeBins;
        NumberOfDistanceBins_ballPosition = behaviourMeasurementBehaviour.NumberOfDistanceBins;
        NumberOfDistanceBins_velocity = behaviourMeasurementBehaviour.NumberOfDistanceBins_velocity;
        NumberOfActionBinsPerAxis = behaviourMeasurementBehaviour.NumberOfActionBinsPerAxis;
        _proportionCollectedReactionTimesText = behaviourMeasurementBehaviour.ProportionCollectedReactionTimes;
        CollectDataForComparison = behaviourMeasurementBehaviour.CollectDataForComparison;
        ComparisonFileName = behaviourMeasurementBehaviour.ComparisonFileName;
        ComparisonTimeLimit = behaviourMeasurementBehaviour.ComparisonTimeLimit;
        MaxNumberOfActions = behaviourMeasurementBehaviour.MaxNumberOfActions;
        _actionsText = behaviourMeasurementBehaviour.Actions;
        IsRawDataCollected = behaviourMeasurementBehaviour.IsRawDataCollected;
        IsSimulation = false;
        IsAbcSimulation = behaviourMeasurementBehaviour.IsAbcSimulation;
        SampleSize = behaviourMeasurementBehaviour.SampleSize;
        SimulationId = behaviourMeasurementBehaviour.SimulationId;

        Initialization();
    }


    //Testing constructor
    public BalancingTaskBehaviourMeasurement(Supervisor.ISupervisorAgent supervisorAgent, IBallAgent[] ballAgents, bool updateExistingModelBehavior, string fileNameForBehavioralData, int numberOfAreaBins_BehavioralData, int numberOfBallVelocityBinsPerAxis_BehavioralData, int numberOfAngleBinsPerAxis_BehavioralData, int numberOfTimeBins, int numberOfDistanceBins, int numberOfDistanceBins_velocity, int numberOfActionBinsPerAxis, bool collectDataForComparison, string comparisonFileName, int comparisonTimeLimit, int maxNumberOfActions = 0, SupervisorSettings supervisorSettings = null, BalancingTaskSettings balancingTaskSettings = null, bool isRawDataCollected = true, bool isSimulation = false, bool isAbcSimulation = false, int sampleSize = -1, int simulationId = -1)
    {
        this.SupervisorAgent = supervisorAgent;
        this.BallAgents = ballAgents;
        this.SaveBehavioralData = true;
        this.UpdateExistingModelBehavior = updateExistingModelBehavior;
        this.FileNameForBehavioralData = fileNameForBehavioralData;
        this.NumberOfAreaBins_BehavioralData = numberOfAreaBins_BehavioralData;
        this.NumberOfBallVelocityBinsPerAxis_BehavioralData = numberOfBallVelocityBinsPerAxis_BehavioralData;
        this.NumberOfAngleBinsPerAxis = numberOfAngleBinsPerAxis_BehavioralData;
        this.NumberOfTimeBins = numberOfTimeBins;
        this.NumberOfDistanceBins_ballPosition = numberOfDistanceBins;
        this.NumberOfDistanceBins_velocity = numberOfDistanceBins_velocity;
        this.NumberOfActionBinsPerAxis = numberOfActionBinsPerAxis;
        this.CollectDataForComparison = collectDataForComparison;
        this.ComparisonFileName = comparisonFileName;
        this.ComparisonTimeLimit = comparisonTimeLimit;
        this.MaxNumberOfActions = maxNumberOfActions;
        this._supervisorSettings = supervisorSettings;
        this._balancingTaskSettings = balancingTaskSettings;
        this.IsRawDataCollected = isRawDataCollected;
        this.IsSimulation = isSimulation;
        this.IsAbcSimulation = isAbcSimulation;
        this.SampleSize = sampleSize;
        this.SimulationId = simulationId;

        Initialization();
    }

    public void UpdateActiveInstance(double timeBetweenSwitches, ISupervisorAgent supervisorAgent = null, bool isNewEpisode = false)
    {
        _timeBetweenSwitches = timeBetweenSwitches;
        _stopWatchLastSwitch.Restart();
        _suspendedReactionTimeCount = 0;
        _switchCount += 1;

        _isReactionTimeMeasurementActive = true;

        _previousActiveAgent = _activeAgent;
    }

    public void ResetMeasurement(object sender, bool aborted)
    {
        _timeBetweenSwitches = 0;
        _isReactionTimeMeasurementActive = false;
    }

    /// <summary>
    /// Used if collectDataForComparison. Terminates the application if no new data was collected for comparisonTimeLimit.
    /// </summary>
    /// <param name="actionBuffers"></param>
    /// <param name="ballAgent"></param>
    public void CheckTimeLimit(ActionBuffers actionBuffers, BallAgent ballAgent, double timeSinceLastSwitch)
    {
        if (CollectionTimerBehavioralData > ComparisonTimeLimit && CollectionTimerResponseTime > ComparisonTimeLimit)
        {
            Debug.Log("Time Limit for collecting new data exceeded. Quit Application...");
            Core.Exit();
        }
    }

    /// <summary>
    /// See CollectResponseTimeAtSwitch and CollectBehavioralData for a description how data is collected. Is called everytime an action is requested
    /// (fixed update circle e.g. 0.02s).
    /// </summary>
    /// <param name="actionBuffers"></param>
    /// <param name="ballAgent"></param>
    public void CollectData(ActionBuffers actionBuffers, IBallAgent targetBallAgent, double timeSinceLastSwitch = -1)
    {
        _activeAgent = targetBallAgent;

        if (timeSinceLastSwitch == -1)
        {
            timeSinceLastSwitch = _stopWatchLastSwitch.ElapsedMilliseconds * Time.timeScale;

            if (timeSinceLastSwitch == 0 && _switchCount != 0)
            {
                Debug.LogWarning("Warning: timeSinceLastSwitch is 0 although _switchCount is not 0");
            }
        }

        if (IsRawDataCollected && !IsSimulation)
        {
            CollectRawData(actionBuffers, targetBallAgent, timeSinceLastSwitch);
        }

        CollectResponseTimeAtSwitch(actionBuffers, targetBallAgent, timeSinceLastSwitch);
        CollectBehavioralData(actionBuffers, targetBallAgent);
    }

    public void SaveReactionTimeToJSON(string path = null)
    {
        if (path == null)
        {
            path = Util.GetReactionTimeDataPath(FileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(_reactionTimeInMsDistanceVelocityRelation));

        Debug.Log(String.Format("Write reaction time data to new file {0}", path));
    }

    public void SaveBehavioralDataToJSON(string path = null)
    {
        if (path == null)
        {
            path = Util.GetBehavioralDataPath(FileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(_actionSetPerBinBehavioralData));

        Debug.Log(String.Format("Write behavioral data to new file {0}", path));
    }

    public void SaveRawBehavioralDataToCSV(int simulationId = -1)
    {
        string path = Util.GetRawBehavioralDataPath(FileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        if (simulationId != -1)
        {
            path = Path.Combine(Util.GetScoreDataPath(), simulationId + "raw_" + FileNameForBehavioralData);
            Util.SaveDataToCSV(path, _behaviouralDataList, true);
        }
        else
        {
            Util.SaveDataToCSV(path, _behaviouralDataList);
        }
    }

    public void SaveReactionTimeToCSV(string path = null)
    {
        if (path == null)
        {
            path = Path.Combine(Util.GetScoreDataPath(), SimulationId + FileNameForBehavioralData);
        }

        Util.SaveDataToCSV(path, _reactionTimeList, true);
    }


    private void Initialization()
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        IsRawDataCollected = IsAbcSimulation || IsRawDataCollected;

        InitMinMaxTime();

        NumberOfAreaBins_BehavioralData = Math.Sqrt(NumberOfAreaBins_BehavioralData) % 1 == 0 ? ((int)Math.Sqrt(NumberOfAreaBins_BehavioralData)) : ((int)Math.Sqrt(NumberOfAreaBins_BehavioralData)) + 1;
        NumberOfAreaBins_BehavioralData = NumberOfAreaBins_BehavioralData * NumberOfAreaBins_BehavioralData;

        //float vectorSum = Math.Abs(VelocityRangeMin.x) + Math.Abs(VelocityRangeMax.x) + Math.Abs(VelocityRangeMin.y) + Math.Abs(VelocityRangeMax.y) + Math.Abs(VelocityRangeMin.z) + Math.Abs(VelocityRangeMax.z);
        NumberOfVelocityBins_BehavioralData = (int)Math.Pow(NumberOfBallVelocityBinsPerAxis_BehavioralData, 3);
        NumberOfAngleBins = (int)Math.Pow(NumberOfAngleBinsPerAxis, 3);
        NumberOfActionBins = (int)Math.Pow(NumberOfActionBinsPerAxis, 2);

        VelocityRangeVector = new Vector3(Math.Abs(VelocityRangeMax.x - VelocityRangeMin.x),
                                  Math.Abs(VelocityRangeMax.y - VelocityRangeMin.y),
                                  Math.Abs(VelocityRangeMax.z - VelocityRangeMin.z));

        AngleRangeVector = new Vector3(Math.Abs(AngleRangeMax.x - AngleRangeMin.x),
                          Math.Abs(AngleRangeMax.y - AngleRangeMin.y),
                          Math.Abs(AngleRangeMax.z - AngleRangeMin.z));

        ActionRangeVector = new Vector3(Math.Abs(ActionRangeMax.x - ActionRangeMin.x),
                          Math.Abs(ActionRangeMax.y - ActionRangeMin.y),
                          Math.Abs(ActionRangeMax.z - ActionRangeMin.z));

        ActionCount = 0;
        _areaBinCount = 0;
        _distanceBinCount = 0;
        _areaBinComparisonCount = 0;
        _distanceComparisonBinCount = 0;

        _lastCallsContinuousActions = new float[2] { 0f, 0f };
        _isReactionTimeMeasurementActive = false;

        _platformRadius = BallAgents[0].GetScale() / 2;
        _numberOFBinsPerDirection = (int)Math.Sqrt(NumberOfAreaBins_BehavioralData);

        _suspendedReactionTimeCount = 0;

        _actionSetPerBinBehavioralData = Init2DDictList<(Vector3, Vector3)>(NumberOfAreaBins_BehavioralData, NumberOfAngleBins);
        _reactionTimeInMsDistanceVelocityRelation = Init3DDictList<(int, double, double)>(NumberOfTimeBins, NumberOfDistanceBins_ballPosition, NumberOfAngleBins);

        _behavioralDataCollectionSettings = new BehavioralDataCollectionSettings();
        _behavioralDataCollectionSettings.numberOfAreaBins_BehavioralData = NumberOfAreaBins_BehavioralData;
        _behavioralDataCollectionSettings.numberOfBallVelocityBinsPerAxis_BehavioralData = NumberOfBallVelocityBinsPerAxis_BehavioralData;
        _behavioralDataCollectionSettings.numberOfDistanceBins = NumberOfDistanceBins_ballPosition;
        _behavioralDataCollectionSettings.numberOfDistanceBins_velocity = NumberOfDistanceBins_velocity;
        _behavioralDataCollectionSettings.numberOfTimeBins = NumberOfTimeBins;
        _behavioralDataCollectionSettings.numberOfAngleBinsPerAxis = NumberOfAngleBinsPerAxis;
        _behavioralDataCollectionSettings.numberOfActionBinsPerAxis = NumberOfActionBinsPerAxis;

        if (_supervisorSettings is null)
        {
            _supervisorSettings = new SupervisorSettings(
                SupervisorAgent is Supervisor.SupervisorAgentRandom,
                SupervisorAgent.SetConstantDecisionRequestInterval,
                SupervisorAgent.DecisionRequestIntervalInSeconds,
                SupervisorAgent is Supervisor.SupervisorAgentRandom ? ((Supervisor.SupervisorAgentRandom)SupervisorAgent).DecisionRequestIntervalRangeInSeconds : 0,
                SupervisorAgent.DifficultyIncrementInterval,
                SupervisorAgent.DecisionPeriod,
                SupervisorAgent.AdvanceNoticeInSeconds);
        }

        if (_balancingTaskSettings is null)
        {
            _balancingTaskSettings = new BalancingTaskSettings(
                -1,
                BallAgents[0].GlobalDrag,
                BallAgents[0].UseNegativeDragDifficulty,
                BallAgents[0].BallAgentDifficulty,
                BallAgents[0].BallAgentDifficultyDivisionFactor,
                BallAgents[0].BallStartingRadius,
                BallAgents[0].ResetSpeed,
                BallAgents[0].ResetPlatformToIdentity,
                BallAgents[0].DecisionPeriod);
        }

        CheckParameters();

        _behaviouralDataList = new List<BehaviouralData>();
        _reactionTimeList = new List<ReactionTime>();

        if (UpdateExistingModelBehavior)
        {
            InitExistingData();
        }

        string pathWithConfig = Util.GetBehavioralDataPath(FileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);
        Directory.CreateDirectory(Path.GetDirectoryName(pathWithConfig));

        if (CollectDataForComparison)
        {
            InitComparisonData();
        }

        InitText();
    }

    private void InitMinMaxTime()
    {
        if (SupervisorAgent is Supervisor.ISupervisorAgentRandom)
        {
            Supervisor.ISupervisorAgentRandom s = (Supervisor.ISupervisorAgentRandom)SupervisorAgent;

            this._minTime = s.DecisionRequestIntervalInSeconds - s.DecisionRequestIntervalRangeInSeconds / 2;
            this._maxTime = s.DecisionRequestIntervalInSeconds + s.DecisionRequestIntervalRangeInSeconds / 2;
        }
        else
        {
            NumberOfTimeBins = 1;
            this._minTime = this._maxTime = SupervisorAgent.DecisionRequestIntervalInSeconds;
        }
    }

    private void CheckParameters()
    {
        string m = "Parameter {0} has value 0!";

        Assert.AreNotEqual(0, NumberOfActionBins, string.Format(m, nameof(NumberOfActionBins)));
        Assert.AreNotEqual(0, NumberOfAngleBins, string.Format(m, nameof(NumberOfAngleBins)));
        Assert.AreNotEqual(0, NumberOfAreaBins_BehavioralData, string.Format(m, nameof(NumberOfAreaBins_BehavioralData)));
        Assert.AreNotEqual(0, _numberOFBinsPerDirection, string.Format(m, nameof(_numberOFBinsPerDirection)));
        Assert.AreNotEqual(0, NumberOfDistanceBins_ballPosition, string.Format(m, nameof(NumberOfDistanceBins_ballPosition)));
        Assert.AreNotEqual(0, NumberOfDistanceBins_velocity, string.Format(m, nameof(NumberOfDistanceBins_velocity)));
        Assert.AreNotEqual(0, NumberOfTimeBins, string.Format(m, nameof(NumberOfTimeBins)));
    }

    private Dictionary<int, (int, T)>[][] Init2DDictList<T>(int dim1N, int dim2N)
    {
        Dictionary<int, (int, T)>[][] result = new Dictionary<int, (int, T)>[dim1N][];

        for (int i = 0; i < dim1N; i++)
        {
            result[i] = new Dictionary<int, (int, T)>[dim2N];

            for (int j = 0; j < NumberOfAngleBins; j++)
            {
                result[i][j] = new Dictionary<int, (int, T)>();
            }
        }

        return result;
    }

    private Dictionary<int, (int, T)>[][][] Init3DDictList<T>(int dim1N, int dim2N, int dim3N)
    {
        Dictionary<int, (int, T)>[][][] result = new Dictionary<int, (int, T)>[dim1N][][];

        for (int i = 0; i < dim1N; i++)
        {
            result[i] = Init2DDictList<T>(dim2N, dim3N);
        }

        return result;
    }

    private void InitExistingData()
    {
        (string, string, string) pathsWithConfig = Util.BuildPathsForBehavioralDataFileName(FileNameForBehavioralData, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings); ;
        (string, string, string) pathsWithoutConfigString = Util.BuildPathsForBehavioralDataFileNameWithoutConfigString(FileNameForBehavioralData, _supervisorSettings, _balancingTaskSettings); ;

        Debug.Log("fileNameForBehavioralData: " + FileNameForBehavioralData);

        try
        {
            InitExistingDataFallBack(pathsWithConfig, pathsWithoutConfigString, InitExitstingBehaviouralData);
        }
        catch (DirectoryNotFoundException)
        {
            Debug.Log(String.Format("Could neither load file nor directory {0}. Continue with new behavioural data in newly created directory.", pathsWithConfig.Item1));
            Directory.CreateDirectory(Path.GetDirectoryName(pathsWithConfig.Item1));
        }

        InitExistingDataFallBack(pathsWithConfig, pathsWithoutConfigString, InitExitstingReactionTimes);
    }

    private void InitExistingDataFallBack((string, string, string) pathsWithConfig, (string, string, string) pathsWithoutConfigString, Action<(string, string, string)> initExitingData)
    {
        try
        {
            initExitingData(pathsWithConfig);
        }
        catch (FileNotFoundException)
        {
            try
            {
                initExitingData(pathsWithoutConfigString);
            }
            catch (FileNotFoundException)
            {
                Debug.Log(String.Format("Could not load file {0}. Continue with new behavioural data.", pathsWithoutConfigString.Item1));
            }
        }
    }

    private void InitExitstingBehaviouralData((string, string, string) paths)
    {
        (Dictionary<int, (int, (Vector3, Vector3))>[][], int, int) resultBehavioural;

        CheckParameters<(Vector3, Vector3)>(paths.Item1, NumberOfAreaBins_BehavioralData * NumberOfAngleBins);

        resultBehavioural = LoadBehaviouralMetaDataFromJSON3D<(Vector3, Vector3)>(paths.Item1);
        _actionSetPerBinBehavioralData = resultBehavioural.Item1;
        _areaBinCount = resultBehavioural.Item2;
        ActionCount = resultBehavioural.Item3;
    }

    private void InitExitstingReactionTimes((string, string, string) paths)
    {
        (Dictionary<int, (int, (int, double, double))>[][][], int, int) resultReactionTimes;

        CheckParameters<(int, double, double)>(paths.Item2, NumberOfDistanceBins_ballPosition * NumberOfAngleBins * NumberOfTimeBins);

        resultReactionTimes = LoadBehaviouralMetaDataFromJSON4D<(int, double, double)>(paths.Item2);
        _reactionTimeInMsDistanceVelocityRelation = resultReactionTimes.Item1;
        _distanceBinCount = resultReactionTimes.Item2;
    }

    private void InitComparisonData()
    {
        (string, string, string) paths = Util.BuildPathsForBehavioralDataFileName(ComparisonFileName, _behavioralDataCollectionSettings, _supervisorSettings, _balancingTaskSettings);

        CheckParameters<(Vector3, Vector3)>(paths.Item1, NumberOfAreaBins_BehavioralData * NumberOfAngleBins);
        CheckParameters<(int, double, double)>(paths.Item2, NumberOfDistanceBins_ballPosition * NumberOfAngleBins * NumberOfTimeBins);

        (bool[][][], int) result3D = LoadComparisonFromJSON3D<(Vector3, Vector3)>(paths.Item1, NumberOfVelocityBins_BehavioralData, ref _areaBinComparisonCount, _actionSetPerBinBehavioralData);
        _actionSetPerBinBehavioralDataComparison = result3D.Item1;
        _totalNumberBehavioralDataComparison = result3D.Item2;

        (bool[][][][], int) result4D = LoadComparisonFromJSON4D<(int, double, double)>(paths.Item2, NumberOfDistanceBins_velocity, ref _distanceComparisonBinCount, _reactionTimeInMsDistanceVelocityRelation);
        _reactionTimeInMsDistanceVelocityRelationComparison = result4D.Item1;
        _totalNumberResponseTimeDataComparison = result4D.Item2;
    }

    private void InitText()
    {
        if (CollectDataForComparison)
        {
            UpdateComparisonTimesText();
            UpdateComparisonDataText();
        }
        else
        {
            UpdateReactionTimesText();
            UpdateBehaviouralDataText();
        }
    }

    private void CheckParameters<T>(string path, int numberOfElements)
    {
        int num = GetJSONNumberOfElements<T>(path);

        Assert.AreEqual(numberOfElements, num, String.Format("Shape of JSON file does not match parameters: #Bins (parameter): {0}, #Bins (JSON dim 1): {1}.", numberOfElements, num));
    }

    private int GetJSONNumberOfElements<T>(string path)
    {
        string json = File.ReadAllText(path);
        int num;

        try
        {
            Dictionary<int, (int, T)>[][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][]>(json);
            num = entry.Length * entry[0].Length;
        }
        catch (JsonSerializationException)
        {
            Dictionary<int, (int, T)>[][][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][][]>(json);
            num = entry.Length * entry[0].Length * entry[0][0].Length;
        }

        return num;
    }

    private (Dictionary<int, (int, T)>[][], int, int) LoadBehaviouralMetaDataFromJSON3D<T>(string path)
    {
        string json = File.ReadAllText(path);

        //arrray[ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][]>(json);

        return GetBehaviouralMetaData3D(entry);
    }

    private (Dictionary<int, (int, T)>[][][], int, int) LoadBehaviouralMetaDataFromJSON4D<T>(string path)
    {
        string json = File.ReadAllText(path);

        //arrray[timeBin][ballBin][angleBin]<velocityBin, entry>
        Dictionary<int, (int, T)>[][][] entry = JsonConvert.DeserializeObject<Dictionary<int, (int, T)>[][][]>(json);

        return GetBehaviouralMetaDatad4D(entry);
    }

    private (Dictionary<int, (int, T)>[][][], int, int) GetBehaviouralMetaDatad4D<T>(Dictionary<int, (int, T)>[][][] entry)
    {
        int uniqueCount = 0;
        int totalCount = 0;

        for (int i = 0; i < entry.Length; i++)
        {
            (Dictionary<int, (int, T)>[][], int, int) subEntry = GetBehaviouralMetaData3D(entry[i]);

            uniqueCount += subEntry.Item2;
            totalCount += subEntry.Item3;
        }

        return (entry, uniqueCount, totalCount);
    }

    private (Dictionary<int, (int, T)>[][], int, int) GetBehaviouralMetaData3D<T>(Dictionary<int, (int, T)>[][] entry)
    {
        int uniqueCount = 0;
        int totalCount = 0;

        for (int i = 0; i < entry.Length; i++)
        {
            for (int j = 0; j < entry[i].Length; j++)
            {
                int l = entry[i][j].Count;

                foreach (var item in entry[i][j])
                {
                    uniqueCount++;
                    totalCount += item.Value.Item1;
                }
            }
        }

        return (entry, uniqueCount, totalCount);
    }

    /// <summary>
    /// Loads the information if behavioral data is available from the specified comparisonFileName JSON file. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="numberOfBin"></param>
    /// <param name="numberOfVelocityBins"></param>
    /// <returns>(bool[][][][], int) where bool[][][][] is a 4d array describing if there is a value for the corresponding dimensions in 
    /// the comparison file. The int value describes the total number of values in the comparison file.</returns>
    private (bool[][][][], int) LoadComparisonFromJSON4D<T>(string path, int numberOfVelocityBins, ref int equalityRatio, Dictionary<int, (int, T)>[][][] loadedData)
    {
        Dictionary<int, (int, T)>[][][] behaviouralData = LoadBehaviouralMetaDataFromJSON4D<T>(path).Item1;

        return GetComparisonFromJSON4D<T>(behaviouralData, numberOfVelocityBins, ref equalityRatio, loadedData);
    }

    private (bool[][][], int) LoadComparisonFromJSON3D<T>(string path, int numberOfVelocityBins, ref int equalityRatio, Dictionary<int, (int, T)>[][] loadedData)
    {
        Dictionary<int, (int, T)>[][] behaviouralData = LoadBehaviouralMetaDataFromJSON3D<T>(path).Item1;

        return GetComparisonFromJSON3D<T>(behaviouralData, numberOfVelocityBins, ref equalityRatio, loadedData);
    }

    private (bool[][][][], int) GetComparisonFromJSON4D<T>(Dictionary<int, (int, T)>[][][] behaviouralData, int numberOfVelocityBins, ref int equalityRatio, Dictionary<int, (int, T)>[][][] loadedData)
    {
        int totalNumberDataComparison = 0;

        bool[][][][] result = new bool[behaviouralData.Length][][][];

        for (int i = 0; i < behaviouralData.Length; i++)
        {
            (bool[][][], int) subResult = GetComparisonFromJSON3D<T>(behaviouralData[i], numberOfVelocityBins, ref equalityRatio, loadedData[i]);
            result[i] = subResult.Item1;
            totalNumberDataComparison = totalNumberDataComparison + subResult.Item2;
        }

        return (result, totalNumberDataComparison);
    }

    private (bool[][][], int) GetComparisonFromJSON3D<T>(Dictionary<int, (int, T)>[][] behaviouralData, int numberOfVelocityBins, ref int equalityRatio, Dictionary<int, (int, T)>[][] loadedData)
    {
        int totalNumberDataComparison = 0;

        bool[][][] result = new bool[behaviouralData.Length][][];

        for (int i = 0; i < behaviouralData.Length; i++)
        {
            result[i] = new bool[behaviouralData[i].Length][];

            for (int j = 0; j < behaviouralData[i].Length; j++)
            {
                result[i][j] = new bool[numberOfVelocityBins];

                for (int k = 0; k < numberOfVelocityBins; k++)
                {
                    if (!behaviouralData[i][j].ContainsKey(k))
                    {
                        result[i][j][k] = false;
                    }
                    else
                    {
                        result[i][j][k] = true;
                        totalNumberDataComparison++;

                        if (loadedData[i][j].ContainsKey(k))
                        {
                            equalityRatio++;
                        }
                    }
                }
            }
        }

        return (result, totalNumberDataComparison);
    }

    private void CollectRawData(ActionBuffers actionBuffers, IBallAgent targetBallAgent, double timeSinceLastSwitch)
    {
        BehaviouralData behaviouralData;

        if (_switchCount != 0)
        {
            behaviouralData = new BehaviouralData
            {
                ActionZ = actionBuffers.ContinuousActions[0],
                ActionX = actionBuffers.ContinuousActions[1],
                TargetBallAgentHashCode = targetBallAgent.GetHashCode(),
                TargetBallLocalPositionX = targetBallAgent.GetBallLocalPosition().x,
                TargetBallLocalPositionY = targetBallAgent.GetBallLocalPosition().y,
                TargetBallLocalPositionZ = targetBallAgent.GetBallLocalPosition().z,
                TargetBallVelocityX = targetBallAgent.GetBallVelocity().x,
                TargetBallVelocityY = targetBallAgent.GetBallVelocity().y,
                TargetBallVelocityZ = targetBallAgent.GetBallVelocity().z,
                TargetPlatformAngleX = targetBallAgent.GetPlatformAngle().x,
                TargetPlatformAngleY = targetBallAgent.GetPlatformAngle().y,
                TargetPlatformAngleZ = targetBallAgent.GetPlatformAngle().z,
                SourceBallAgentHashCode = _previousActiveAgent.GetHashCode(),
                SourceBallLocalPositionX = _previousActiveAgent.GetBallLocalPosition().x,
                SourceBallLocalPositionY = _previousActiveAgent.GetBallLocalPosition().y,
                SourceBallLocalPositionZ = _previousActiveAgent.GetBallLocalPosition().z,
                SourceBallVelocityX = _previousActiveAgent.GetBallVelocity().x,
                SourceBallVelocityY = _previousActiveAgent.GetBallVelocity().y,
                SourceBallVelocityZ = _previousActiveAgent.GetBallVelocity().z,
                SourcePlatformAngleX = _previousActiveAgent.GetPlatformAngle().x,
                SourcePlatformAngleY = _previousActiveAgent.GetPlatformAngle().y,
                SourcePlatformAngleZ = _previousActiveAgent.GetPlatformAngle().z,
                TimeSinceLastSwitch = timeSinceLastSwitch,
                TimeBetweenSwitches = _timeBetweenSwitches
            };
        }
        else
        {
            behaviouralData = new BehaviouralData
            {
                ActionZ = actionBuffers.ContinuousActions[0],
                ActionX = actionBuffers.ContinuousActions[1],
                TargetBallAgentHashCode = targetBallAgent.GetHashCode(),
                TargetBallLocalPositionX = targetBallAgent.GetBallLocalPosition().x,
                TargetBallLocalPositionY = targetBallAgent.GetBallLocalPosition().y,
                TargetBallLocalPositionZ = targetBallAgent.GetBallLocalPosition().z,
                TargetBallVelocityX = targetBallAgent.GetBallVelocity().x,
                TargetBallVelocityY = targetBallAgent.GetBallVelocity().y,
                TargetBallVelocityZ = targetBallAgent.GetBallVelocity().z,
                TargetPlatformAngleX = targetBallAgent.GetPlatformAngle().x,
                TargetPlatformAngleY = targetBallAgent.GetPlatformAngle().y,
                TargetPlatformAngleZ = targetBallAgent.GetPlatformAngle().z,
                TimeSinceLastSwitch = timeSinceLastSwitch,
                TimeBetweenSwitches = _timeBetweenSwitches
            };
        }

        _behaviouralDataList.Add(behaviouralData);
    }

    /// <summary>
    ///Reaction time is set only until the next action set by the player is in the usual action bin. Therefore, if a player sets an action
    ///which is unusual due the task switch (maybe due to the stress of the new situation) the reaction time is not saved until the normal
    ///behavior can be seen. Example: task switch at t1, action set at t2 and t3 is not in the same range as usual, action set at t4 is in
    ///the usual range --> leads to a reaction time of t4-t1.
    ///Furthermore, reaction times are only measured if this chains of reaction times always have a corresponding entry in the behavioral
    ///data. Therefore, reaction times can only be measured if already enough behavioral data was collected. Example: task switch at t1,
    ///action set at t2 is not in the same range as usual, no action set data for t3 --> reaction time for task switch cannot be measured.
    /// </summary>
    private void CollectResponseTimeAtSwitch(ActionBuffers actionBuffers, IBallAgent targetBallAgent, double timeSinceLastSwitch)
    {
        if (_isReactionTimeMeasurementActive)
        {
            int ballBin = PositionConverter.CoordinatesToBin(targetBallAgent.GetBallLocalPosition(), _platformRadius, _numberOFBinsPerDirection);
            int velocityBin_BehavioralData = PositionConverter.RangeVectorToBin(targetBallAgent.GetBallVelocity(), VelocityRangeVector, NumberOfBallVelocityBinsPerAxis_BehavioralData, VelocityRangeMin);
            int targetAngleBin = PositionConverter.RangeVectorToBin(targetBallAgent.GetPlatformAngle(), AngleRangeVector, NumberOfAngleBinsPerAxis, AngleRangeMin);

            if (ballBin != -1)
            {
                //no behavioral data available --> discard measurement
                if (!_actionSetPerBinBehavioralData[ballBin][targetAngleBin].ContainsKey(velocityBin_BehavioralData))
                {
                    Debug.Log("Discard reaction time measurement (no behavioural data available)!");
                    _isReactionTimeMeasurementActive = false;
                    _suspendedReactionTimeCount = 0;
                }
                else
                {
                    Vector3 currentAverageActionBehavioralData = _actionSetPerBinBehavioralData[ballBin][targetAngleBin][velocityBin_BehavioralData].Item2.Item1 / _actionSetPerBinBehavioralData[ballBin][targetAngleBin][velocityBin_BehavioralData].Item1;

                    if (ActionIsInUsualRange(currentAverageActionBehavioralData, actionBuffers) && (actionBuffers.ContinuousActions[0] != _lastCallsContinuousActions[0] || actionBuffers.ContinuousActions[1] != _lastCallsContinuousActions[1]))
                    {
                        UpdateReactionTime(targetBallAgent, timeSinceLastSwitch);
                        _reactionTimeMeasurementCount += 1;

                        if (_reactionTimeMeasurementCount == SampleSize)
                        {
                            Debug.Log("Max number of samples collected. Quit Application...");
                            Core.Exit();
                        }

                        _isReactionTimeMeasurementActive = false;
                        _suspendedReactionTimeCount = 0;
                    }
                    else
                    {
                        Debug.Log("Suspend reaction time measurement (action in unusual range)!");
                        _suspendedReactionTimeCount += 1;
                    }
                }
            }
            _lastCallsContinuousActions[0] = actionBuffers.ContinuousActions[0];
            _lastCallsContinuousActions[1] = actionBuffers.ContinuousActions[1];
        }
    }

    private void UpdateReactionTime(IBallAgent targetBallAgent, double timeSinceLastSwitch)
    {
        Debug.Log(String.Format("Decision Time: {0}", timeSinceLastSwitch));

        int timeBin = PositionConverter.ContinuousValueToBin((float)_timeBetweenSwitches, _minTime, _maxTime, NumberOfTimeBins);
        int distanceBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(targetBallAgent.GetBallLocalPosition(), _previousActiveAgent.GetBallLocalPosition()),
                                                            _platformRadius * 2,  //scale of platform
                                                            NumberOfDistanceBins_ballPosition);
        int velocityBin = PositionConverter.ContinuousValueToBin(Vector3.Distance(targetBallAgent.GetBallVelocity(), _previousActiveAgent.GetBallVelocity()),
                                                            _platformRadius * 2,
                                                            NumberOfDistanceBins_velocity);
        int angleBinDistance = PositionConverter.ContinuousValueToBin(Vector3.Distance(targetBallAgent.GetPlatformAngle(), _previousActiveAgent.GetPlatformAngle()),
                                                            Vector3.Distance(AngleRangeMin, AngleRangeMax),
                                                            (int)Math.Pow(NumberOfAngleBinsPerAxis, 3));

        if (CollectDataForComparison && _reactionTimeInMsDistanceVelocityRelationComparison[timeBin][distanceBin][angleBinDistance][velocityBin] && !_reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance].ContainsKey(velocityBin))
        {
            CollectionTimerResponseTime = 0;
            _distanceComparisonBinCount++;

            if (!IsSimulation) UpdateComparisonTimesText();
        }

        if (_reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance].ContainsKey(velocityBin))
        {
            try
            {
                checked
                {
                    _reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin] = (_reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin].Item1 + 1,
                                                                                                                     (_reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin].Item2.Item1 + _suspendedReactionTimeCount,
                                                                                                                      _reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin].Item2.Item2 + timeSinceLastSwitch,
                                                                                                                      _reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin].Item2.Item3 + Math.Pow(timeSinceLastSwitch, 2)));
                }
            }
            catch (OverflowException e)
            {
                Debug.LogError(String.Format("DistanceBin {0}, VelocityBin {1}: {2}", distanceBin, velocityBin, e.Message));
            }
        }
        else
        {
            _reactionTimeInMsDistanceVelocityRelation[timeBin][distanceBin][angleBinDistance][velocityBin] = (1,
                                                                                                             (_suspendedReactionTimeCount,
                                                                                                              timeSinceLastSwitch,
                                                                                                              Math.Pow(timeSinceLastSwitch, 2)));
            _distanceBinCount += 1;

            if (!CollectDataForComparison)
            {
                if (!IsSimulation) UpdateReactionTimesText();
            }
        }

        if (IsAbcSimulation)
        {
            ReactionTime reactionTime = new ReactionTime
            {
                Time = timeSinceLastSwitch,
                SuspendedReactionTimeCount = _suspendedReactionTimeCount
            };
            _reactionTimeList.Add(reactionTime);
        }
    }

    private bool ActionIsInUsualRange(Vector3 currentAverageActionBehavioralData, ActionBuffers actionBuffers)
    {
        int averageActionBinBehavioralData = PositionConverter.RangeVectorToBin(currentAverageActionBehavioralData, ActionRangeVector, NumberOfActionBinsPerAxis, ActionRangeMin);
        int currentActionBinBehavioralData = PositionConverter.RangeVectorToBin(new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]), ActionRangeVector, NumberOfActionBinsPerAxis, ActionRangeMin);

        return averageActionBinBehavioralData == currentActionBinBehavioralData;
    }

    /// <summary>
    /// Collects behavioral data if _actionReceived == true and saves the data to _actionSetPerBinBehavioralData.
    /// </summary>
    /// <param name="actionBuffers"></param>
    /// <param name="ballAgent"></param>
    /// <param name="collectData"></param>
    private void CollectBehavioralData(ActionBuffers actionBuffers, IBallAgent ballAgent)
    {
        int ballBin = PositionConverter.CoordinatesToBin(ballAgent.GetBallLocalPosition(), _platformRadius, _numberOFBinsPerDirection);

        if (ballBin != -1 && !_isReactionTimeMeasurementActive)
        {
            int velocityBin = PositionConverter.RangeVectorToBin(ballAgent.GetBallVelocity(), VelocityRangeVector, NumberOfBallVelocityBinsPerAxis_BehavioralData, VelocityRangeMin);
            int angleBin = PositionConverter.RangeVectorToBin(ballAgent.GetPlatformAngle(), AngleRangeVector, NumberOfAngleBinsPerAxis, AngleRangeMin);

            if (CollectDataForComparison && _actionSetPerBinBehavioralDataComparison[ballBin][angleBin][velocityBin] && !_actionSetPerBinBehavioralData[ballBin][angleBin].ContainsKey(velocityBin))
            {
                CollectionTimerBehavioralData = 0;

                _areaBinComparisonCount++;

                if (!IsSimulation) UpdateComparisonDataText();
            }

            if (_actionSetPerBinBehavioralData[ballBin][angleBin].ContainsKey(velocityBin))
            {
                try
                {
                    checked
                    {
                        _actionSetPerBinBehavioralData[ballBin][angleBin][velocityBin] = (_actionSetPerBinBehavioralData[ballBin][angleBin][velocityBin].Item1 + 1,
                                                                                         (_actionSetPerBinBehavioralData[ballBin][angleBin][velocityBin].Item2.Item1 + new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]),
                                                                                          _actionSetPerBinBehavioralData[ballBin][angleBin][velocityBin].Item2.Item2 + new Vector3((float)Math.Pow(actionBuffers.ContinuousActions[1], 2), 0, (float)Math.Pow(actionBuffers.ContinuousActions[0], 2))));
                        ActionCount++;
                    }
                }
                catch (OverflowException e)
                {
                    Debug.LogError(String.Format("BallBin {0}, VelocityBin {1}: {2}", ballBin, velocityBin, e.Message));
                }
            }
            else
            {
                _actionSetPerBinBehavioralData[ballBin][angleBin][velocityBin] = (1,
                                                                                 (new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]),
                                                                                  new Vector3((float)Math.Pow(actionBuffers.ContinuousActions[1], 2), 0, (float)Math.Pow(actionBuffers.ContinuousActions[0], 2))));
                _areaBinCount += 1;
                ActionCount++;

                if (!CollectDataForComparison)
                {
                    if (!IsSimulation) UpdateBehaviouralDataText();
                }
            }
        }

        if (MaxNumberOfActions > 0 && MaxNumberOfActions <= ActionCount)
        {
            Debug.Log("Max number of actions reached. Quit Application...");
            Core.Exit();
        }
    }

    private void UpdateComparisonTimesText()
    {
        double ratio = _totalNumberResponseTimeDataComparison != 0 ? (_distanceComparisonBinCount / (double)(_totalNumberResponseTimeDataComparison)) * 100 : 100;

        string info = String.Format("{0:N4} % Comparison Times", ratio);
        if (_proportionCollectedReactionTimesText != null) _proportionCollectedReactionTimesText.text = info;
        Debug.Log(info);
    }

    private void UpdateReactionTimesText()
    {
        string info = String.Format("{0:N4} % Reaction Times", (_distanceBinCount / (double)(NumberOfDistanceBins_velocity * NumberOfDistanceBins_ballPosition * NumberOfAngleBins * NumberOfTimeBins)) * 100);
        if (_proportionCollectedReactionTimesText != null) _proportionCollectedReactionTimesText.text = info;
        Debug.Log(info);
    }

    private void UpdateComparisonDataText()
    {
        if (MaxNumberOfActions > 0)
        {
            double actioRatio = _totalNumberBehavioralDataComparison != 0 ? (ActionCount / (double)(MaxNumberOfActions)) * 100 : 100;
            string actionInfo = String.Format("{0:N4} % Actions Performed", actioRatio);
            if (_actionsText != null) _actionsText.text = actionInfo;
            Debug.Log(actionInfo);
        }

        double ratio = _totalNumberBehavioralDataComparison != 0 ? (_areaBinComparisonCount / (double)(_totalNumberBehavioralDataComparison)) * 100 : 100;

        string info = String.Format("{0:N4} % Comparison Data", ratio);
        if (_proportionCollectedBehavioralDataText != null) _proportionCollectedBehavioralDataText.text = info;
        Debug.Log(info);
    }

    private void UpdateBehaviouralDataText()
    {
        if (MaxNumberOfActions > 0)
        {
            double actioRatio = (ActionCount / (double)(MaxNumberOfActions)) * 100;
            string actionInfo = String.Format("{0:N4} % Actions Performed", actioRatio);
            if (_actionsText != null) _actionsText.text = actionInfo;
            Debug.Log(actionInfo);
        }
        else
        {
            string actionInfo = String.Format("{0} Actions Performed", ActionCount);
            if (_actionsText != null) _actionsText.text = actionInfo;
            Debug.Log(actionInfo);
        }

        string info = String.Format("{0:N4} % Behavioral Data", (_areaBinCount / (double)(NumberOfVelocityBins_BehavioralData * NumberOfAreaBins_BehavioralData * NumberOfAngleBins)) * 100);
        if (_proportionCollectedBehavioralDataText != null) _proportionCollectedBehavioralDataText.text = info;
        Debug.Log(info);
    }
}


public class ReactionTime
{
    public double Time { get; set; }

    public int SuspendedReactionTimeCount { get; set; }
}


public class BehaviouralData
{
    public BehaviouralData(float actionZ, float actionX, int targetBallAgentHashCode, float targetBallLocalPositionX, float targetBallLocalPositionY, float targetBallLocalPositionZ, float targetBallVelocityX, float targetBallVelocityY, float targetBallVelocityZ, float targetPlatformAngleX, float targetPlatformAngleY, float targetPlatformAngleZ, int sourceBallAgentHashCode, float sourceBallLocalPositionX, float sourceBallLocalPositionY, float sourceBallLocalPositionZ, float sourceBallVelocityX, float sourceBallVelocityY, float sourceBallVelocityZ, float sourcePlatformAngleX, float sourcePlatformAngleY, float sourcePlatformAngleZ, double timeSinceLastSwitch, double timeBetweenSwitches)
    {
        ActionZ = actionZ;
        ActionX = actionX;
        TargetBallAgentHashCode = targetBallAgentHashCode;
        TargetBallLocalPositionX = targetBallLocalPositionX;
        TargetBallLocalPositionY = targetBallLocalPositionY;
        TargetBallLocalPositionZ = targetBallLocalPositionZ;
        TargetBallVelocityX = targetBallVelocityX;
        TargetBallVelocityY = targetBallVelocityY;
        TargetBallVelocityZ = targetBallVelocityZ;
        TargetPlatformAngleX = targetPlatformAngleX;
        TargetPlatformAngleY = targetPlatformAngleY;
        TargetPlatformAngleZ = targetPlatformAngleZ;
        SourceBallAgentHashCode = sourceBallAgentHashCode;
        SourceBallLocalPositionX = sourceBallLocalPositionX;
        SourceBallLocalPositionY = sourceBallLocalPositionY;
        SourceBallLocalPositionZ = sourceBallLocalPositionZ;
        SourceBallVelocityX = sourceBallVelocityX;
        SourceBallVelocityY = sourceBallVelocityY;
        SourceBallVelocityZ = sourceBallVelocityZ;
        SourcePlatformAngleX = sourcePlatformAngleX;
        SourcePlatformAngleY = sourcePlatformAngleY;
        SourcePlatformAngleZ = sourcePlatformAngleZ;
        TimeSinceLastSwitch = timeSinceLastSwitch;
        TimeBetweenSwitches = timeBetweenSwitches;
    }

    public BehaviouralData() { }

    public float ActionZ { get; set; }

    public float ActionX { get; set; }

    public int TargetBallAgentHashCode { get; set; }

    public float TargetBallLocalPositionX { get; set; }

    public float TargetBallLocalPositionY { get; set; }

    public float TargetBallLocalPositionZ { get; set; }

    public float TargetBallVelocityX { get; set; }

    public float TargetBallVelocityY { get; set; }

    public float TargetBallVelocityZ { get; set; }

    public float TargetPlatformAngleX { get; set; }

    public float TargetPlatformAngleY { get; set; }

    public float TargetPlatformAngleZ { get; set; }

    public int SourceBallAgentHashCode { get; set; }

    public float SourceBallLocalPositionX { get; set; }

    public float SourceBallLocalPositionY { get; set; }

    public float SourceBallLocalPositionZ { get; set; }

    public float SourceBallVelocityX { get; set; }

    public float SourceBallVelocityY { get; set; }

    public float SourceBallVelocityZ { get; set; }

    public float SourcePlatformAngleX { get; set; }

    public float SourcePlatformAngleY { get; set; }

    public float SourcePlatformAngleZ { get; set; }

    public double TimeSinceLastSwitch { get; set; }

    public double TimeBetweenSwitches { get; set; }
}