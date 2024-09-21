using System;
using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using CsvHelper.Configuration;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using Unity.MLAgents.Actuators;
using Supervisor;
using UnityEngine.InputSystem.XR;
using System.Linq;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;
using System.Reflection;
using static UnityEngine.EventSystems.EventTrigger;

public class PerformanceMeasurement : MonoBehaviour
{
    [field: SerializeField, Tooltip("Scores are saved to Scores/Data/{File Name}"), ProjectAssign]
    public string FileNameForScores { get; set; } = "_scores.csv";

    [field: SerializeField, Tooltip("Name of the Player."), ProjectAssign]
    public string PlayerName { get; set; } = "Alexander Lingler";

    [field: SerializeField, Tooltip("Max number of Episodes for performance measurement. If value <= 0 no max number is used."), ProjectAssign]
    public int MaxNumberEpisodes { get; set; } = 0;

    [field: SerializeField, Tooltip("Lower boundary for the measurement of the _scores. Episodes with a lower score are not recorded."), ProjectAssign]
    public int MinimumScoreForMeasurement { get; set; } = 20;

    [field: SerializeField]
    public bool IsTrainingMode { private get; set; }

    [field: SerializeField]
    public bool IsSupervised { private get; set; }

    [field: ProjectAssign(Hide = true)]
    public bool IsAbcSimulation { get; set; }

    [field: ProjectAssign(Hide = true)]
    public int SimulationId { get; set; }

    [field: SerializeField, ProjectAssign]
    public bool MeasurePerformance { get; set; }


    private Supervisor.SupervisorAgent _supervisorAgent;

    private ITask[] _tasks;

    private BallAgent _ballAgent;

    private Unity.MLAgents.Policies.BehaviorParameters _behaviorParameters;

    private string _dateTime;

    private List<Score> _scores;

    private Dictionary<Tuple<int, int>, List<SwitchingData>> _switchingData;

    private List<ITask> _tasksActionReceivedFrom;

    private int _episodeCount;

    private string _pathScores;

    private string _pathSwitchingData;

    private SwitchingData _switchingDataEntry;

    private int _targetTask;

    private int _sourceTask;

    private int _switchingId;


    private void OnEnable()
    {
        if (!IsTrainingMode)
        {
            Debug.Log("Performance will be collected...");

            Supervisor.SupervisorAgent.EndEpisodeEvent += AddPerformanceOfEpisode;
            Supervisor.SupervisorAgent.OnTaskSwitchCompleted += CreateSwitchingData;
            ITask.OnAction += AddSwitchingData;
            ITask.OnAction += ValidateSwitchingData;
        }

        if (!MeasurePerformance && !IsAbcSimulation)
        {
            MaxNumberEpisodes = 0;
            MinimumScoreForMeasurement = 0;
            FileNameForScores = "scores.csv";
        }
    }

    private void OnDisable()
    {
        if (!IsTrainingMode)
        {
            Supervisor.SupervisorAgent.EndEpisodeEvent -= AddPerformanceOfEpisode;
            Supervisor.SupervisorAgent.OnTaskSwitchCompleted -= CreateSwitchingData;
            ITask.OnAction -= AddSwitchingData;
            ITask.OnAction -= ValidateSwitchingData;

            if (IsAbcSimulation)
            {
                Util.SaveDataToCSV(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}", SimulationId, FileNameForScores)), _scores, true);

                if (SwitchingDataIsValid())
                {
                    foreach (KeyValuePair<Tuple<int, int>, List<SwitchingData>> entry in _switchingData)
                    {
                        Util.SaveDataToCSV(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}{2}_{3}", SimulationId, "switching", MeasurementUtil.GetTupleName(entry.Key, _tasks), FileNameForScores)), entry.Value, true);
                    }
                }
            }
            else
            {
                Util.SaveDataToCSV(Path.Combine(_pathScores, FileNameForScores), _scores);

                if (SwitchingDataIsValid())
                {
                    foreach (KeyValuePair<Tuple<int, int>, List<SwitchingData>> entry in _switchingData)
                    {
                        Util.SaveDataToCSV(Path.Combine(_pathSwitchingData, String.Format("{0}{1}_{2}", "switching", MeasurementUtil.GetTupleName(entry.Key, _tasks), FileNameForScores)), entry.Value);
                    }
                }
            }
        }
    }

    private void Awake()
    {
        _dateTime = DateTime.Now.ToString();
        _scores = new List<Score>();
        _supervisorAgent = SupervisorAgent.GetSupervisor(gameObject);
        _behaviorParameters = this.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        _switchingData = new Dictionary<Tuple<int, int>, List<SwitchingData>>();
        _tasksActionReceivedFrom = new();

        LogToFile.LogPropertiesFieldsOfObject(this);
    }

    private void Start()
    {
        _switchingId = 1;
        _tasks = _supervisorAgent.Tasks;
        InitPaths();

        _sourceTask = _targetTask = _supervisorAgent.GetActiveTaskNumber();

        string modelName = GetModelName();
        _episodeCount = GetNumberOfNotAbortedEpisodesFromCSV(Path.Combine(_pathScores, FileNameForScores), PlayerName, modelName);

        if (MaxNumberEpisodes > 0)
        {
            if (_episodeCount >= MaxNumberEpisodes)
            {
                Debug.Log("MaxNumberEpisodes already reached, quit application.");
                Core.Exit();
            }
        }
    }

    private void InitPaths()
    {
        string workingDirectory = Util.GetWorkingDirectory();

        SupervisorSettings supervisorSettings = new SupervisorSettings(
            _supervisorAgent is Supervisor.SupervisorAgentRandom,
            _supervisorAgent.SetConstantDecisionRequestInterval,
            _supervisorAgent.DecisionRequestIntervalInSeconds,
            _supervisorAgent is Supervisor.SupervisorAgentRandom ? ((Supervisor.SupervisorAgentRandom)_supervisorAgent).DecisionRequestIntervalRangeInSeconds : 0,
            _supervisorAgent.DifficultyIncrementInterval,
            _supervisorAgent.DecisionPeriod,
            _supervisorAgent.AdvanceNoticeInSeconds);

        Hyperparameters hyperparameters = new Hyperparameters
        {
            tasks = _supervisorAgent.TaskNames,
        };

        _pathScores = Path.Combine(workingDirectory, "Scores", Util.GetScoreString(supervisorSettings, hyperparameters));
        _pathSwitchingData = Path.Combine(workingDirectory, "Scores", Util.GetScoreString(supervisorSettings, hyperparameters));
    }

    private Vector2 GetCurrentJoystickAxis()
    {
        Vector2 axis = new Vector2();

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            axis = gamepad.leftStick.ReadValue();
            //stickL.x will be -1.0..1.0 (for full left to full right)
            //stickL.y will be -1.0..1.0 (for full down to full up)
        }

        return axis;
    }

    private void CreateSwitchingData(double timeBetweenSwitches, Supervisor.ISupervisorAgent supervisorAgent, bool isNewEpisode)
    {
        _sourceTask = _targetTask;
        _targetTask = supervisorAgent.GetActiveTaskNumber();

        if(_sourceTask == _targetTask)
        {
            return;
        }

        Tuple<int, int> tuple = MeasurementUtil.GetTuple(_sourceTask, _targetTask);

        IStateInformation stateInformationA = _tasks[tuple.Item1].StateInformation;
        IStateInformation stateInformationB = _tasks[tuple.Item2].StateInformation;

        _switchingDataEntry = new SwitchingData
        {
            DateTime = _dateTime,
            PlayerName = PlayerName,
            EpisodeId = supervisorAgent.EpisodeCount,
            SwitchingId = _switchingId,
            SourceTaskId = _sourceTask,
            TargetTaskId = _targetTask,
            TimeOnPreviousTask = timeBetweenSwitches,
            StateA = stateInformationA,
            StateB = stateInformationB,
            Supervisor = !supervisorAgent.UseHeuristic,
            ModelName = GetModelName(),
        };

        if (!_switchingData.ContainsKey(tuple))
        {
            _switchingData[tuple] = new List<SwitchingData>();
        }
        _switchingData[tuple].Add(_switchingDataEntry);

        _switchingId += 1;
    }

    private void ValidateSwitchingData(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch = -1)
    {
        if(!_tasksActionReceivedFrom.Contains(task))
        {
            _tasksActionReceivedFrom.Add(task);
        }
    }

    private bool SwitchingDataIsValid()
    {
        foreach (ITask task in _supervisorAgent.Tasks)
        {
            if (!_tasksActionReceivedFrom.Contains(task))
            {
                Debug.LogWarning(string.Format("SwitchingData is not valid. No received actions from Task {0}.", task.GetType().Name));
                return false;
            }
        }

        return true;
    }

    private void AddSwitchingData(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch = -1)
    {
        if (_sourceTask == _targetTask)
        {
            return;
        }

        _switchingDataEntry.EpisodeId = _supervisorAgent.EpisodeCount;
        _switchingDataEntry.JoystickAxisX = GetCurrentJoystickAxis().x;
        _switchingDataEntry.JoystickAxisY = GetCurrentJoystickAxis().y;
        _switchingDataEntry.ContinuousActionZ = actionBuffers.ContinuousActions.Length > 1 ? actionBuffers.ContinuousActions[0] : 0;
        _switchingDataEntry.ContinuousActionX = actionBuffers.ContinuousActions.Length > 2 ? actionBuffers.ContinuousActions[1] : 0;
        _switchingDataEntry.DiscreteActionX = actionBuffers.DiscreteActions.Length > 1 ? actionBuffers.DiscreteActions[0] : 0;
        _switchingDataEntry.DiscreteActionX = actionBuffers.DiscreteActions.Length > 2 ? actionBuffers.DiscreteActions[1] : 0;
        _switchingDataEntry.ReactionTime = _supervisorAgent.TimeSinceLastSwitch;

        Tuple<int, int> tuple = MeasurementUtil.GetTuple(_sourceTask, _targetTask);

        _switchingData[tuple].Add(new SwitchingData(_switchingDataEntry));
    }

    /// <summary>
    /// Adds reached performance (see Score class) to _scores.
    /// </summary>
    /// <param name="aborted"></param>
    private void AddPerformanceOfEpisode(object sender, bool aborted)
    {
        Score score = new Score
        {
            DateTime = _dateTime,
            PlayerName = PlayerName,
            EpisodeId = _supervisorAgent.EpisodeCount,
            Duration = _supervisorAgent.FixedEpisodeDuration,
            Supervisor = !_supervisorAgent.UseHeuristic,
            ModelName = GetModelName(),
            FocusActivePlatform = _supervisorAgent.FocusActiveTask,
            Aborted = aborted,
            TaskPerformance = GetAggregatedTaskPerformance()
        };

        _scores.Add(score);

        if (_supervisorAgent.FixedEpisodeDuration > MinimumScoreForMeasurement)
        {
            _episodeCount++;

            if (MaxNumberEpisodes > 0 && !aborted)
            {
                Debug.Log(string.Format("Performance Measurement active: {0}/{1} episodes completed!", _episodeCount, MaxNumberEpisodes));
                

                if (_episodeCount == MaxNumberEpisodes)
                {
                    Debug.Log("MaxNumberEpisodes reached, quit application.");
                    Core.Exit();
                }
            }
        }
    }

    private Dictionary<string, double> GetAggregatedTaskPerformance()
    {
        Dictionary<string, double> aggregatedTaskPerformance = new();

        List<Type> types = _supervisorAgent.Tasks
                    .Select(obj => obj.GetType())
                    .Distinct()
                    .ToList();

        foreach (Type type in types)
        {
            List<ITask> tasksOfType = _supervisorAgent.Tasks
                                        .Where(obj => obj.GetType() == type)
                                        .ToList();

            for(int i = 0; i < tasksOfType.Count; i++)
            {
                foreach (KeyValuePair<string, double> entry in tasksOfType[i].Performance)
                {
                    if (aggregatedTaskPerformance.ContainsKey(entry.Key))
                    {
                        aggregatedTaskPerformance[entry.Key] += entry.Value;
                    }
                    else
                    {
                        aggregatedTaskPerformance[entry.Key] = entry.Value;
                    }

                    if (i == tasksOfType.Count - 1)
                    {
                        aggregatedTaskPerformance[entry.Key] /= tasksOfType.Count;
                    }
                }
                
                tasksOfType[i].ResetPerformance();
            }
        }

        return aggregatedTaskPerformance;
    }

    private string GetModelName()
    {
        if (!IsSupervised)
        {
            return "NoSupervisor";
        }
        else if (_supervisorAgent.GetType() == typeof(Supervisor.SupervisorAgentRandom))
        {
            return "RandomSupervisor";
        }
        else
        {
            return _behaviorParameters.Model != null ? _behaviorParameters.Model.name : null;
        }
    }

    private int GetNumberOfNotAbortedEpisodesFromCSV(string path, string playerName, string modelName)
    {
        int episodeCount = 0;

        if (File.Exists(path))
        {
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<Score>();

                foreach(Score score in records)
                {
                    if ((!score.Aborted && score.ModelName == modelName && score.PlayerName == playerName && score.Duration > MinimumScoreForMeasurement))
                    {
                        episodeCount++;
                    }
                }
            }
        }

        Debug.Log(string.Format("Performance Measurement active: {0}/{1} episodes completed for model {2} and player {3}!", episodeCount, MaxNumberEpisodes, modelName, playerName));

        return episodeCount;
    }
}


public class Score
{
    public string DateTime { get; set; }
    public string PlayerName { get; set; }
    public int EpisodeId { get; set; }
    public float Duration { get; set; }
    public bool Supervisor { get; set; }
    public string ModelName { get; set; }
    public bool FocusActivePlatform { get; set; }
    public bool Aborted { get; set; }
    public Dictionary<string, double> TaskPerformance { get; set; }
}


public class SwitchingData
{
    public SwitchingData(){}

    public SwitchingData(SwitchingData switchingData)
    {
        DateTime = switchingData.DateTime;
        PlayerName = switchingData.PlayerName;
        EpisodeId = switchingData.EpisodeId;
        SwitchingId = switchingData.SwitchingId;
        SourceTaskId = switchingData.SourceTaskId;
        TargetTaskId = switchingData.TargetTaskId;
        ReactionTime = switchingData.ReactionTime;
        TimeOnPreviousTask = switchingData.TimeOnPreviousTask;
        JoystickAxisX = switchingData.JoystickAxisX;
        JoystickAxisY = switchingData.JoystickAxisY;
        ContinuousActionZ = switchingData.ContinuousActionZ;
        ContinuousActionX = switchingData.ContinuousActionX;
        DiscreteActionX = switchingData.DiscreteActionX;
        DiscreteActionY = switchingData.DiscreteActionY;
        StateA = switchingData.StateA;
        StateB = switchingData.StateB;
        Supervisor = switchingData.Supervisor;
        ModelName = switchingData.ModelName;
    }

    public string DateTime { get; set; }
    public string PlayerName { get; set; }
    public int EpisodeId { get; set; }
    public int SwitchingId { get; set; }
    public int SourceTaskId { get; set; }
    public int TargetTaskId { get; set; }
    public double ReactionTime { get; set; }
    public double TimeOnPreviousTask { get; set; }
    public float JoystickAxisX { get; set; }
    public float JoystickAxisY { get; set; }
    public float ContinuousActionZ { get; set; }
    public float ContinuousActionX { get; set; }
    public float DiscreteActionX { get; set; }
    public float DiscreteActionY { get; set; }
    public bool Supervisor { get; set; }
    public string ModelName { get; set; }
    public IStateInformation StateA { get; set; }
    public IStateInformation StateB { get; set; }
}
