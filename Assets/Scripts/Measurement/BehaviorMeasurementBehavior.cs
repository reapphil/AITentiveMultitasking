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
using System.Linq;
using Newtonsoft.Json.Linq;
using System.ArrayExtensions;
using static UnityEngine.EventSystems.EventTrigger;

//Core functions are separated for testing purpose. MonoBehaviours cannot be created without a gameObject. Therefore, testing those classes is hard.
//The Humble Object pattern separates the core functionality into an own class which can be created with the help of the new keyword and therefore
//this class can be tested without a Game Object.
public class BehaviorMeasurementBehavior : MonoBehaviour
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

    [field: SerializeField, Tooltip("Updates the saved model behavioral data of the given file."), ProjectAssign]
    public bool UpdateExistingModelBehavior { get; set; }

    [field: SerializeField, Header("Relation Difference to Reaction Time"), Tooltip("Number of bins in which the active time of a platform should " +
    "be divided. The time bins are used to calculate the relation how the reaction time is related to the time a platform was active."), ProjectAssign(Header = "Relation Difference to Reaction Time")]
    public int NumberOfTimeBins { set; get; }

    [field: SerializeField, Tooltip("Collects behavioral data until the maximum number of actions is reached."), ProjectAssign]
    public int MaxNumberOfActions { get; set; }

    [field: SerializeField, Tooltip("Behavioral Data is saved to Scores/{File Name}"), ProjectAssign]
    public string FileNameForBehavioralData { get; set; } = "behavioralData.csv";

    [field: SerializeField, Tooltip("The raw data entries are saved additionally to the bin data."), ProjectAssign]
    public bool IsRawDataCollected { get; set; } = true;


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


    private BehaviorMeasurement _behaviourMeasurement;


    private void OnEnable()
    {
        if (SaveBehavioralData)
        {
            SupervisorAgent = SupervisorAgent.GetSupervisor(gameObject);
            _behaviourMeasurement = new BehaviorMeasurement(this);

            ITask.OnAction += _behaviourMeasurement.CollectData;
            SupervisorAgent.OnTaskSwitchCompleted += _behaviourMeasurement.UpdateActiveInstance;
            SupervisorAgent.EndEpisodeEvent += _behaviourMeasurement.ResetMeasurement;
        }

        LogToFile.LogPropertiesFieldsOfObject(this);
    }

    private void OnDisable()
    {
        if (SaveBehavioralData)
        {
            ITask.OnAction -= _behaviourMeasurement.CollectData;
            SupervisorAgent.OnTaskSwitchCompleted -= _behaviourMeasurement.UpdateActiveInstance;
            SupervisorAgent.EndEpisodeEvent -= _behaviourMeasurement.ResetMeasurement;

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
                _behaviourMeasurement.SaveBehavioralDataToJSON(Util.GetScoreDataPath(), IsAbcSimulation);
                _behaviourMeasurement.SaveReactionTimeToJSON(Util.GetScoreDataPath(), IsAbcSimulation);
            }

            Debug.Log(String.Format("Total number of actions collected: {0}", _behaviourMeasurement.ActionCount));
        }
    }
}

/// <summary>
/// TODO: The collection of raw data does only work if both tasks are equal (e.g. based on the file format of BehavioralData that currently cannot saved to a CSV file if the tasks differ).
/// </summary>
public class BehaviorMeasurement
{
    public Supervisor.ISupervisorAgent SupervisorAgent { private set; get; }

    public bool SaveBehavioralData { private set; get; }

    public bool UpdateExistingModelBehavior { private set; get; }

    public string FileNameForBehavioralData { private set; get; }

    public int NumberOfTimeBins { private set; get; }

    public int MaxNumberOfActions { private set; get; }

    public int ActionCount { private set; get; }

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

    private int _uniqueActionsCount;

    private int _distanceBinCount;

    private int _totalStateCount;

    private int _totalDistanceCount;

    private float[] _lastCallsContinuousActions;

    private ITask _previousActiveAgent;

    private ITask _activeAgent;

    private bool _isReactionTimeMeasurementActive;

    private double _timeBetweenSwitches;

    private SupervisorSettings _supervisorSettings;

    private Hyperparameters _hyperparameters;

    private List<BehavioralData> _behaviouralDataList;

    private List<ReactionTime> _reactionTimeList;

    private float _minTime;

    private float _maxTime;

    private Stopwatch _stopWatchLastSwitch = new();

    private int _suspendedReactionTimeCount;

    private int _switchCount = 0;

    private int _reactionTimeMeasurementCount = 0;


    public BehaviorMeasurement(BehaviorMeasurementBehavior behaviorMeasurementBehavior)
    {
        SupervisorAgent = behaviorMeasurementBehavior.SupervisorAgent;
        SaveBehavioralData = behaviorMeasurementBehavior.SaveBehavioralData;
        UpdateExistingModelBehavior = behaviorMeasurementBehavior.UpdateExistingModelBehavior;
        FileNameForBehavioralData = behaviorMeasurementBehavior.FileNameForBehavioralData;
        _proportionCollectedBehavioralDataText = behaviorMeasurementBehavior.ProportionCollectedBehavioralData;
        NumberOfTimeBins = behaviorMeasurementBehavior.NumberOfTimeBins;
        _proportionCollectedReactionTimesText = behaviorMeasurementBehavior.ProportionCollectedReactionTimes;
        _actionsText = behaviorMeasurementBehavior.Actions;
        IsRawDataCollected = behaviorMeasurementBehavior.IsRawDataCollected;
        IsSimulation = false;
        IsAbcSimulation = behaviorMeasurementBehavior.IsAbcSimulation;
        SampleSize = behaviorMeasurementBehavior.SampleSize;
        SimulationId = behaviorMeasurementBehavior.SimulationId;
        MaxNumberOfActions = behaviorMeasurementBehavior.MaxNumberOfActions;

        Initialization();
    }


    //Testing constructor
    public BehaviorMeasurement(Supervisor.ISupervisorAgent supervisorAgent, bool updateExistingModelBehavior, string fileNameForBehavioralData, int numberOfTimeBins, int maxNumberOfActions = 0, SupervisorSettings supervisorSettings = null, Hyperparameters hyperparameters = null, bool isRawDataCollected = true, bool isSimulation = false, bool isAbcSimulation = false, int sampleSize = -1, int simulationId = -1)
    {
        this.SupervisorAgent = supervisorAgent;
        this.SaveBehavioralData = true;
        this.UpdateExistingModelBehavior = updateExistingModelBehavior;
        this.FileNameForBehavioralData = fileNameForBehavioralData;
        this.NumberOfTimeBins = numberOfTimeBins;
        this.MaxNumberOfActions = maxNumberOfActions;
        this._supervisorSettings = supervisorSettings;
        this._hyperparameters = hyperparameters;
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
    /// See CollectResponseTimeAtSwitch and CollectBehavioralData for a description how data is collected. Is called every time an action is requested
    /// (fixed update circle e.g. 0.02s).
    /// </summary>
    /// <param name="actionBuffers"></param>
    /// <param name="ballAgent"></param>
    public void CollectData(ActionBuffers actionBuffers, ITask targetTask, double timeSinceLastSwitch = -1)
    {
        _activeAgent = targetTask;

        if (timeSinceLastSwitch == -1)
        {
            timeSinceLastSwitch = _stopWatchLastSwitch.ElapsedMilliseconds * Time.timeScale;
        }

        if (timeSinceLastSwitch == 0 && _switchCount != 0)
        {
            Debug.LogWarning("Warning: timeSinceLastSwitch is 0 although _switchCount is not 0");
        }

        if (IsRawDataCollected && !IsSimulation)
        {
            CollectRawData(actionBuffers, _activeAgent, timeSinceLastSwitch);
        }

        CollectResponseTimeAtSwitch(actionBuffers, _activeAgent, timeSinceLastSwitch);
        CollectBehavioralData(actionBuffers, _activeAgent);
    }

    public void SaveReactionTimeToJSON(string scorePath = null, bool isAbcSimulation=false)
    {
        List<Array> alreadySaved = new List<Array>();

        foreach (ITask task in SupervisorAgent.Tasks)
        {
            foreach(KeyValuePair<Type, Array> entry in task.StateInformation.ReactionTimes)
            {
                if (!alreadySaved.Contains(entry.Value))
                {
                    string path = Util.GetReactionTimeDataPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(entry.Key, task.StateInformation.GetType()), task.StateInformation.GetRelationalDimensions(entry.Key, NumberOfTimeBins));

                    if (scorePath != null)
                    {
                        path = scorePath;

                        if (isAbcSimulation)
                        {
                            path = Util.GetABCReactionTimeDataPath(scorePath, FileNameForBehavioralData, task.StateInformation.GetRelationalDimensions(entry.Key, NumberOfTimeBins), ".json", MeasurementUtil.GetMeasurementName(entry.Key, task.StateInformation.GetType()), SimulationId);
                        }
                    }

                    File.WriteAllText(path, JsonConvert.SerializeObject(entry.Value));
                    alreadySaved.Add(entry.Value);
                    Debug.Log(String.Format("Write reaction time data to new file {0}", path));
                }
            }
        }
    }

    public void SaveBehavioralDataToJSON(string scorePath = null, bool isAbcSimulation = false)
    {
        foreach (ITask task in SupervisorAgent.Tasks)
        {
            string path = Util.GetBehavioralDataPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters, Util.ShortenString(task.StateInformation.GetType().Name), task.StateInformation.BehaviorDimensions);

            if (scorePath != null)
            {
                path = scorePath;
                if (isAbcSimulation)
                {
                    path = Util.GetABCBehavioralDataPath(scorePath, FileNameForBehavioralData, task.StateInformation.BehaviorDimensions, ".json", task.StateInformation.GetType().Name, SimulationId);
                }
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(task.StateInformation.PerformedActions, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

            Debug.Log(String.Format("Write behavioral data to new file {0}", path));
        }
    }

    public void SaveRawBehavioralDataToCSV(int simulationId = -1)
    {
        string path = Util.GetRawBehavioralDataPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters);

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

        NumberOfTimeBins = NumberOfTimeBins > 0 ? NumberOfTimeBins : -1;

        InitMinMaxTime();
        InitSettings();
        InitCounting();

        if (UpdateExistingModelBehavior)
        {
            InitExistingData();
        }

        InitMeasurements();

        CheckParameters();

        _behaviouralDataList = new List<BehavioralData>();
        _reactionTimeList = new List<ReactionTime>();

        string pathWithConfig = Util.GetRawBehavioralDataPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters);
        Directory.CreateDirectory(Path.GetDirectoryName(pathWithConfig));

        InitText();

        _previousActiveAgent = _activeAgent = SupervisorAgent.Tasks[0];
    }

    private void InitSettings()
    {
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

        if (_hyperparameters is null)
        {
            _hyperparameters = new Hyperparameters
            {
                tasks = SupervisorAgent.TaskNames
            };
        }
    }

    private void InitMeasurements()
    {
        ITask[] tasks = SupervisorAgent.Tasks;

        InitPerformedActions(tasks);
        InitReactionTimes(tasks);
    }

    private void InitCounting()
    {
        List<(Type, Type)> combinations = new();

        foreach (ITask task in SupervisorAgent.Tasks.Distinct())
        {
            Type type = task.StateInformation.GetType();

            _totalStateCount += task.StateInformation.BehaviorDimensions.Aggregate(1, (acc, val) => acc * val);

            foreach (ITask taskb in SupervisorAgent.Tasks.Distinct())
            {
                if (!combinations.Contains((task.GetType(), taskb.GetType())))
                {
                    _totalDistanceCount += task.StateInformation.GetRelationalDimensions(taskb.StateInformation.GetType(), NumberOfTimeBins).Aggregate(1, (acc, val) => acc * val);
                }

                combinations.Add((task.GetType(), taskb.GetType()));
            }
        }

        ActionCount = 0;
        _uniqueActionsCount = 0;
        _distanceBinCount = 0;
        _lastCallsContinuousActions = new float[2] { 0f, 0f };
        _isReactionTimeMeasurementActive = false;
        _suspendedReactionTimeCount = 0;
    }

    private void InitPerformedActions(ITask[] tasks)
    {
        foreach (Type t in tasks.Distinct().Select(x => x.StateInformation.GetType()))
        {
            ITask[] tasksOfSameType = tasks.Where(x => x.StateInformation.GetType() == t).ToArray();
            IStateInformation state = tasksOfSameType[0].StateInformation;
            Array performedActions = Array.CreateInstance(typeof(Dictionary<int, (int, (Vector3, Vector3))>), state.BehaviorDimensions[..^1]);

            foreach (int[] binCombi in Util.GetIndicesForDimentions(state.BehaviorDimensions[..^1]))
            {
                performedActions.SetValue(new Dictionary<int, (int, (Vector3, Vector3))>(), binCombi);
            }

            foreach (ITask taskOfSameType in tasksOfSameType)
            {
                state = taskOfSameType.StateInformation;

                //same PerformedActions object for all tasks with the same type
                state.PerformedActions ??= performedActions;
            }
        }
    }

    private void InitReactionTimes(ITask[] tasks)
    {
        foreach (IStateInformation s1 in tasks.Select(x => x.StateInformation))
        {
            s1.ReactionTimes ??= new Dictionary<Type, Array>();

            foreach (IStateInformation s2 in tasks.Select(x => x.StateInformation))
            {
                if(s1 != s2)
                {
                    s2.ReactionTimes ??= new Dictionary<Type, Array>();

                    //Only if both tasks have the same type it does not matter which is the source and which is the target task when a switch is performed
                    if (s2.ReactionTimes.ContainsKey(s1.GetType()) && s1.GetType() == s2.GetType())
                    {
                        s1.ReactionTimes[s2.GetType()] = s2.ReactionTimes[s2.GetType()];
                    }
                    else
                    {
                        if (!s1.ReactionTimes.ContainsKey(s2.GetType()))
                        {
                            s1.ReactionTimes[s2.GetType()] = Array.CreateInstance(typeof(Dictionary<int, (int, (int, double, double))>), s1.GetRelationalDimensions(s2.GetType(), NumberOfTimeBins)[..^1]);

                            foreach (int[] binCombi in Util.GetIndicesForDimentions(s1.GetRelationalDimensions(s2.GetType(), NumberOfTimeBins)[..^1]))
                            {
                                s1.ReactionTimes[s2.GetType()].SetValue(new Dictionary<int, (int, (int, double, double))>(), binCombi);
                            }
                        }
                    }
                }
            }
        }
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
        foreach(ITask task in SupervisorAgent.Tasks)
        {
            foreach (int i in task.StateInformation.BehaviorDimensions)
            {
                Assert.AreNotEqual(0, i, "Behavioral data dimensions cannot be 0!");
            }

            foreach (Type type in SupervisorAgent.Tasks.Select(x => x.StateInformation.GetType()))
            {
                foreach (int i in task.StateInformation.GetRelationalDimensions(type,NumberOfTimeBins))
                {
                    Assert.AreNotEqual(0, i, "Reaction time dimensions cannot be 0!");
                }
            }
        }
    }

    private void InitExistingData()
    {
        foreach (ITask task in SupervisorAgent.Tasks)
        {
            string path = Util.GetBehavioralDataPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters, Util.ShortenString(task.StateInformation.GetType().Name), task.StateInformation.BehaviorDimensions);

            try
            {
                InitExistingDataFallBack((Util.GetBehavioralDataPath, Util.GetBehavioralDataWithoutConfigPath), task, InitExitstingBehaviouralData);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.Log(String.Format("Could neither load file nor directory {0}. Continue with new behavioral data in newly created directory.", path));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            InitExistingDataFallBack((Util.GetReactionTimeDataPath, Util.GetReactionTimeDataWithoutConfigPath), task, InitExitstingReactionTimes);
        }
    }

    private void InitExitstingBehaviouralData(Func<string, SupervisorSettings, Hyperparameters, string, int[], string> getPath, ITask task)
    {
        string path = getPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters, Util.ShortenString(task.StateInformation.GetType().Name), task.StateInformation.BehaviorDimensions);

        foreach (ITask t in SupervisorAgent.Tasks.Where(x => x.GetType() == task.GetType()))
        {
            if (t.StateInformation.PerformedActions != null)
            {
                task.StateInformation.PerformedActions = t.StateInformation.PerformedActions;

                return;
            }
        }

        (Array, int, int) resultBehavioural = LoadBehaviouralMetaDataFromJSON<(Vector3, Vector3)>(path, task.StateInformation.BehaviorDimensions);

        task.StateInformation.PerformedActions = resultBehavioural.Item1;
        _uniqueActionsCount = resultBehavioural.Item2;
        ActionCount = resultBehavioural.Item3;
    }

    private void InitExitstingReactionTimes(Func<string, SupervisorSettings, Hyperparameters, string, int[], string> getPath, ITask task)
    {
        foreach (ITask t in SupervisorAgent.Tasks)
        {
            task.StateInformation.ReactionTimes ??= new Dictionary<Type, Array>();

            if (t.StateInformation.ReactionTimes != null && t.StateInformation.ReactionTimes.ContainsKey(task.StateInformation.GetType()))
            {
                task.StateInformation.ReactionTimes[t.StateInformation.GetType()] = t.StateInformation.ReactionTimes[task.StateInformation.GetType()];
            }
            else
            {
                string path = getPath(FileNameForBehavioralData, _supervisorSettings, _hyperparameters, MeasurementUtil.GetMeasurementName(t.StateInformation.GetType(), task.StateInformation.GetType()), task.StateInformation.GetRelationalDimensions(t.StateInformation.GetType(), NumberOfTimeBins));

                (Array, int, int) resultReactionTimes = LoadBehaviouralMetaDataFromJSON<(int, double, double)>(path, task.StateInformation.GetRelationalDimensions(t.StateInformation.GetType(), NumberOfTimeBins));
                task.StateInformation.ReactionTimes[t.StateInformation.GetType()] = resultReactionTimes.Item1;
                _distanceBinCount = resultReactionTimes.Item2;
            }
        }
    }

    private void InitExistingDataFallBack((Func<string, SupervisorSettings, Hyperparameters, string, int[], string>, Func<string, SupervisorSettings, Hyperparameters, string, int[], string>) getPathFunctions, ITask task, Action<Func<string, SupervisorSettings, Hyperparameters, string, int[], string>, ITask> initExitingData)
    {
        try
        {
            initExitingData(getPathFunctions.Item1, task);
        }
        catch (FileNotFoundException)
        {
            try
            {
                initExitingData(getPathFunctions.Item2, task);
            }
            catch (FileNotFoundException)
            {
                Debug.Log(String.Format("Could not load file {0}. Continue with new behavioral data.", FileNameForBehavioralData));
            }
        }
    }

    private void InitText()
    {
        UpdateReactionTimesText();
        UpdateBehaviouralDataText();
    }

    private (Array, int, int) LoadBehaviouralMetaDataFromJSON<T>(string path, params int[] lengths)
    {
        string json = File.ReadAllText(path);

        JArray entry = JArray.Parse(json);

        return GetBehaviouralMetaData<T>(JArrayConverter.ConvertToSystemArray(entry, typeof(Dictionary<int, (int, T)>), lengths[..^1]));
    }

    private Array ConvertJArrayToArray(JArray jArray, Type elementType, params int[] lengths)
    {
        Array array = Array.CreateInstance(elementType, lengths);

        if (jArray.Count != lengths[0])
        {
            throw new ArgumentException("Mismatch in array dimensions.");
        }

        if (lengths.Length == 1)
        {
            for (int i = 0; i < lengths[0]; i++)
            {
                array.SetValue(((JValue)jArray[i]).Value, i);
            }
        }
        else
        {
            int[] innerLengths = lengths.Skip(1).ToArray();
            Type innerElementType = elementType.GetElementType();

            for (int i = 0; i < lengths[0]; i++)
            {
                JArray innerJArray = (JArray)jArray[i];
                array.SetValue(ConvertJArrayToArray(innerJArray, innerElementType, innerLengths), i);
            }
        }

        return array;
    }

    private (Array, int, int) GetBehaviouralMetaData<T>(Array array)
    {
        int uniqueCount = 0;
        int totalCount = 0;

        int[] dimension = new int[array.Rank];
        
        for(int i = 0; i < array.Rank; i++)
        {
            dimension[i] = array.GetLength(i);
        }

        foreach (int[] binCombi in Util.GetIndicesForDimentions(dimension))
        {
            Dictionary<int, (int, T)> dict = (Dictionary<int, (int, T)>)array.GetValue(binCombi);

            if(dict != null)
            {
                foreach (var item in dict)
                {
                    uniqueCount++;
                    totalCount += item.Value.Item1;
                }
            }
        }

        return (array, uniqueCount, totalCount);
    }

    private void CollectRawData(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch)
    {
        BehavioralData behaviouralData = new BehavioralData
        {
            ActionZ = actionBuffers.ContinuousActions[0],
            ActionX = actionBuffers.ContinuousActions[1],
            SourceTaskId = SupervisorAgent.GetPreviousActiveTaskNumber(),
            TargetTaskId = SupervisorAgent.GetActiveTaskNumber(),
            SourceState = _previousActiveAgent.StateInformation.GetCopyOfCurrentState(),
            TargetState = _activeAgent.StateInformation.GetCopyOfCurrentState(),
            TimeSinceLastSwitch = timeSinceLastSwitch,
            TimeBetweenSwitches = _timeBetweenSwitches
        };

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
    private void CollectResponseTimeAtSwitch(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch)
    {
        if (_isReactionTimeMeasurementActive)
        {
            int[] bins = task.StateInformation.GetDiscretizedStateInformation();

            Dictionary<int, (int, (Vector3, Vector3))> performedActions = (Dictionary<int, (int, (Vector3, Vector3))>)task.StateInformation.PerformedActions.GetValue(bins[..^1]);

            //no behavioral data available --> discard measurement
            if (!performedActions.ContainsKey(bins[^1]))
            {
                Debug.Log("Discard reaction time measurement (no behavioral data available)!");
                _isReactionTimeMeasurementActive = false;
                _suspendedReactionTimeCount = 0;
            }
            else
            {
                Vector3 currentAverageActionBehavioralData = performedActions[bins[^1]].Item2.Item1 / performedActions[bins[^1]].Item1;

                if (ActionIsInUsualRange(currentAverageActionBehavioralData, actionBuffers, task.StateInformation) && (actionBuffers.ContinuousActions[0] != _lastCallsContinuousActions[0] || actionBuffers.ContinuousActions[1] != _lastCallsContinuousActions[1]))
                {
                    UpdateReactionTime(task, timeSinceLastSwitch);
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
            
            _lastCallsContinuousActions[0] = actionBuffers.ContinuousActions[0];
            _lastCallsContinuousActions[1] = actionBuffers.ContinuousActions[1];
        }
    }

    private void UpdateReactionTime(ITask task, double timeSinceLastSwitch)
    {
        Debug.Log(String.Format("Decision Time: {0}", timeSinceLastSwitch));

        int timeBin = PositionConverter.ContinuousValueToBin((float)_timeBetweenSwitches, _minTime, _maxTime, NumberOfTimeBins);
        int[] bins = task.StateInformation.GetDiscretizedRelationalStateInformation(_previousActiveAgent.StateInformation, timeBin);

        Dictionary<int, (int, (int, double, double))> reactionTimes = (Dictionary<int, (int, (int, double, double))>)task.StateInformation.ReactionTimes[_previousActiveAgent.StateInformation.GetType()].GetValue(bins[..^1]);

        if (reactionTimes.ContainsKey(bins[^1]))
        {
            try
            {
                checked
                {
                    reactionTimes[bins[^1]] = (reactionTimes[bins[^1]].Item1 + 1,
                                              (reactionTimes[bins[^1]].Item2.Item1 + _suspendedReactionTimeCount,
                                               reactionTimes[bins[^1]].Item2.Item2 + timeSinceLastSwitch,
                                               reactionTimes[bins[^1]].Item2.Item3 + Math.Pow(timeSinceLastSwitch, 2)));
                }
            }
            catch (OverflowException e)
            {
                Debug.LogError(e.Message);
            }
        }
        else
        {
            reactionTimes[bins[^1]] = (1,
                                      (_suspendedReactionTimeCount,
                                       timeSinceLastSwitch,
                                       Math.Pow(timeSinceLastSwitch, 2)));
            _distanceBinCount += 1;

            if (!IsSimulation) UpdateReactionTimesText();
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

    private bool ActionIsInUsualRange(Vector3 currentAverageActionBehavioralData, ActionBuffers actionBuffers, IStateInformation stateInformation)
    {
        Vector3 actionRangeVector = new Vector3(Math.Abs(stateInformation.ActionRangeMax.x - stateInformation.ActionRangeMin.x),
                          Math.Abs(stateInformation.ActionRangeMax.y - stateInformation.ActionRangeMin.y),
                          Math.Abs(stateInformation.ActionRangeMax.z - stateInformation.ActionRangeMin.z));

        int averageActionBinBehavioralData = PositionConverter.RangeVectorToBin(currentAverageActionBehavioralData, actionRangeVector, stateInformation.NumberOfActionBinsPerAxis, stateInformation.ActionRangeMin);
        int currentActionBinBehavioralData = PositionConverter.RangeVectorToBin(new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]), actionRangeVector, stateInformation.NumberOfActionBinsPerAxis, stateInformation.ActionRangeMin);

        return averageActionBinBehavioralData == currentActionBinBehavioralData;
    }

    /// <summary>
    /// Collects behavioral data if _actionReceived == true and saves the data to _actionSetPerBinBehavioralData.
    /// </summary>
    /// <param name="actionBuffers"></param>
    /// <param name="task"></param>
    /// <param name="collectData"></param>
    private void CollectBehavioralData(ActionBuffers actionBuffers, ITask task)
    {
        int[] bins =  task.StateInformation.GetDiscretizedStateInformation();

        if (!_isReactionTimeMeasurementActive)
        {
            Dictionary<int, (int, (Vector3, Vector3))> performedActions = (Dictionary<int, (int, (Vector3, Vector3))>)task.StateInformation.PerformedActions.GetValue(bins[..^1]);

            if (performedActions.ContainsKey(bins[^1]))
            {
                try
                {
                    checked
                    {
                        performedActions[bins[^1]] = (performedActions[bins[^1]].Item1 + 1,
                                          (performedActions[bins[^1]].Item2.Item1 + new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]),
                                           performedActions[bins[^1]].Item2.Item2 + new Vector3((float)Math.Pow(actionBuffers.ContinuousActions[1], 2), 0, (float)Math.Pow(actionBuffers.ContinuousActions[0], 2))));
                        ActionCount++;
                    }
                }
                catch (OverflowException e)
                {
                    Debug.LogError(e.Message);
                }
            }
            else
            {
                performedActions[bins[^1]] = (1,
                                  (new Vector3(actionBuffers.ContinuousActions[1], 0, actionBuffers.ContinuousActions[0]),
                                   new Vector3((float)Math.Pow(actionBuffers.ContinuousActions[1], 2), 0, (float)Math.Pow(actionBuffers.ContinuousActions[0], 2))));
                _uniqueActionsCount += 1;
                ActionCount++;

                if (!IsSimulation) UpdateBehaviouralDataText();
            }
        }

        if (MaxNumberOfActions > 0 && MaxNumberOfActions <= ActionCount)
        {
            Debug.Log("Max number of actions reached. Quit Application...");
            Core.Exit();
        }
    }

    private void UpdateReactionTimesText()
    {
        string info = String.Format("{0:N4} % Reaction Times", (_distanceBinCount / (double)(_totalDistanceCount)) * 100);
        if (_proportionCollectedReactionTimesText != null) _proportionCollectedReactionTimesText.text = info;
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

        string info = String.Format("{0:N4} % Behavioral Data", (_uniqueActionsCount / (double)(_totalStateCount)) * 100);
        if (_proportionCollectedBehavioralDataText != null) _proportionCollectedBehavioralDataText.text = info;
        Debug.Log(info);
    }
}


public class ReactionTime
{
    public double Time { get; set; }

    public int SuspendedReactionTimeCount { get; set; }
}


public class BehavioralData
{
    public BehavioralData(float actionZ, float actionX, int sourceTaskId, int targetTaskId, IStateInformation sourceState, IStateInformation targetState, double timeSinceLastSwitch, double timeBetweenSwitches)
    {
        ActionZ = actionZ;
        ActionX = actionX;
        SourceTaskId = sourceTaskId;
        TargetTaskId = targetTaskId;
        SourceState = sourceState;
        TargetState = targetState;
        TimeSinceLastSwitch = timeSinceLastSwitch;
        TimeBetweenSwitches = timeBetweenSwitches;
    }

    public BehavioralData() { }

    public float ActionZ { get; set; }

    public float ActionX { get; set; }

    public int SourceTaskId { get; set; }
    
    public int TargetTaskId { get; set; }

    public double TimeSinceLastSwitch { get; set; }

    public double TimeBetweenSwitches { get; set; }

    public IStateInformation SourceState { get; set; }

    public IStateInformation TargetState { get; set; }
}