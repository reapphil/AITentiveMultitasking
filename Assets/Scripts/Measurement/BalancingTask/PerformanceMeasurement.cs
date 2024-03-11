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

    private BallAgent ballAgent;

    private Unity.MLAgents.Policies.BehaviorParameters _behaviorParameters;

    private string _dateTime;

    private List<Score> _scores;

    private List<SwitchingData> _switchingData;

    private (int, float) _countBallCenterDistance;

    private string _agentChoice;

    private int _episodeCount;

    private string _pathScores;

    private string _pathSwitchingData;

    private SwitchingData _switchingDataEntry;

    private IBallAgent _oldActiveBallAgent;

    private IBallAgent _newActiveBallAgent;

    private int _switchingId;

    private Vector3 _platformAngleSource;


    private void OnEnable()
    {
        if (!IsTrainingMode)
        {
            Debug.Log("Performance will be collected...");

            Supervisor.SupervisorAgent.EndEpisodeEvent += AddPerformanceOfEpisode;
            Supervisor.SupervisorAgent.OnTaskSwitchCompleted += CreateSwitchingData;
            BallAgent.OnCollectObservationsAction += CollectBallDistanceToCenter;
            BallAgent.OnAction += AddSwitchingData;
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
            BallAgent.OnCollectObservationsAction -= CollectBallDistanceToCenter;
            BallAgent.OnAction -= AddSwitchingData;

            if (IsAbcSimulation)
            {
                Util.SaveDataToCSV(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}", SimulationId, FileNameForScores)), _scores, true);
                Util.SaveDataToCSV(Path.Combine(Util.GetScoreDataPath(), string.Format("{0}{1}_{2}", SimulationId, "switching", FileNameForScores)), _switchingData, true);
            }
            else
            {
                Util.SaveDataToCSV(_pathScores, _scores);
                Util.SaveDataToCSV(_pathSwitchingData, _switchingData);
            }
        }
    }

    private void Awake()
    {
        _dateTime = DateTime.Now.ToString();
        _scores = new List<Score>();
        _switchingData = new List<SwitchingData>();

        _supervisorAgent = SupervisorAgent.GetSupervisor(gameObject);
        _behaviorParameters = this.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        ballAgent = _supervisorAgent.GetBallAgents()[0];

        _agentChoice = ballAgent.GetComponent<BallAgent>().GetType().ToString();

        string workingDirectory = Util.GetWorkingDirectory();

        SupervisorSettings supervisorSettings = new SupervisorSettings(
            _supervisorAgent is Supervisor.SupervisorAgentRandom,
            _supervisorAgent.SetConstantDecisionRequestInterval,
            _supervisorAgent.DecisionRequestIntervalInSeconds,
            _supervisorAgent is Supervisor.SupervisorAgentRandom ? ((Supervisor.SupervisorAgentRandom)_supervisorAgent).DecisionRequestIntervalRangeInSeconds : 0,
            _supervisorAgent.DifficultyIncrementInterval,
            _supervisorAgent.DecisionPeriod,
            _supervisorAgent.AdvanceNoticeInSeconds);

        BalancingTaskSettings balancingTaskSettings = new BalancingTaskSettings(
            -1,
            ballAgent.GlobalDrag,
            ballAgent.UseNegativeDragDifficulty,
            ballAgent.BallAgentDifficulty,
            ballAgent.BallAgentDifficultyDivisionFactor,
            ballAgent.BallStartingRadius,
            ballAgent.ResetSpeed,
            ballAgent.ResetPlatformToIdentity,
            ballAgent.DecisionPeriod);

        _pathScores = Path.Combine(workingDirectory, "Scores", Util.GetScoreString(supervisorSettings, balancingTaskSettings), FileNameForScores);
        _pathSwitchingData = Path.Combine(workingDirectory, "Scores", Util.GetScoreString(supervisorSettings, balancingTaskSettings), "switching_" + FileNameForScores);

        string modelName = GetModelName();

        _episodeCount = GetNumberOfNotAbortedEpisodesFromCSV(_pathScores, PlayerName, modelName);
        
        if (MaxNumberEpisodes > 0)
        {
            if (_episodeCount >= MaxNumberEpisodes)
            {
                Debug.Log("MaxNumberEpisodes already reached, quit application.");
                Core.Exit();
            }
        }

        LogToFile.LogPropertiesFieldsOfComponent(this);
    }

    private void Start()
    {
        _countBallCenterDistance = (0, 0f);
        _newActiveBallAgent = _oldActiveBallAgent = (BallAgent)_supervisorAgent.GetActiveTask();
        _switchingId = 1;
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
        _newActiveBallAgent = (BallAgent)supervisorAgent.GetActiveTask();

        if (_newActiveBallAgent is not null && !isNewEpisode)
        {
            _switchingDataEntry = new SwitchingData
            {
                DateTime = _dateTime,
                PlayerName = PlayerName,
                EpisodeId = supervisorAgent.EpisodeCount,
                SwitchingId = _switchingId,
                TimeOnPreviousTask = timeBetweenSwitches,
                DragValue = _newActiveBallAgent.GetBallDrag(),
                PlatformAngleSourceX = _oldActiveBallAgent.GetPlatformAngle().x,
                PlatformAngleSourceY = _oldActiveBallAgent.GetPlatformAngle().y,
                PlatformAngleSourceZ = _oldActiveBallAgent.GetPlatformAngle().z,
                BallVelocitySourceX = _oldActiveBallAgent.GetBallVelocity().x,
                BallVelocitySourceY = _oldActiveBallAgent.GetBallVelocity().y,
                BallVelocitySourceZ = _oldActiveBallAgent.GetBallVelocity().z,
                BallPositionSourceX = _oldActiveBallAgent.GetBallLocalPosition().x,
                BallPositionSourceY = _oldActiveBallAgent.GetBallLocalPosition().y,
                BallPositionSourceZ = _oldActiveBallAgent.GetBallLocalPosition().z,
                PlatformAngleTargetX = _newActiveBallAgent.GetPlatformAngle().x,
                PlatformAngleTargetY = _newActiveBallAgent.GetPlatformAngle().y,
                PlatformAngleTargetZ = _newActiveBallAgent.GetPlatformAngle().z,
                BallVelocityTargetX = _newActiveBallAgent.GetBallVelocity().x,
                BallVelocityTargetY = _newActiveBallAgent.GetBallVelocity().y,
                BallVelocityTargetZ = _newActiveBallAgent.GetBallVelocity().z,
                BallPositionTargetX = _newActiveBallAgent.GetBallLocalPosition().x,
                BallPositionTargetY = _newActiveBallAgent.GetBallLocalPosition().y,
                BallPositionTargetZ = _newActiveBallAgent.GetBallLocalPosition().z,
                Supervisor = !supervisorAgent.UseHeuristic,
                ModelName = GetModelName(),
                AgentChoice = _agentChoice
            };

            _switchingId += 1;
            _oldActiveBallAgent = _newActiveBallAgent;
        }
    }

    private void AddSwitchingData(ActionBuffers actionBuffers, BallAgent ballAgent, double timeSinceLastSwitch)
    {
        if (_switchingDataEntry is null)
        {
            _switchingDataEntry = new SwitchingData
            {
                DateTime = _dateTime,
                PlayerName = PlayerName,
                ModelName = GetModelName(),
                AgentChoice = _agentChoice
            };
        }

        _switchingDataEntry.EpisodeId = _supervisorAgent.EpisodeCount;
        _switchingDataEntry.JoystickAxisX = GetCurrentJoystickAxis().x;
        _switchingDataEntry.JoystickAxisY = GetCurrentJoystickAxis().y;
        _switchingDataEntry.ActionZ = actionBuffers.ContinuousActions[0];
        _switchingDataEntry.ActionX = actionBuffers.ContinuousActions[1];
        _switchingDataEntry.ReactionTime = timeSinceLastSwitch;

        _switchingData.Add(new SwitchingData(_switchingDataEntry));
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
            Drag = _supervisorAgent.GetBallAgents()[0].GetBallDrag(),
            AverageDistanceToCenter = _countBallCenterDistance.Item2 / _countBallCenterDistance.Item1,
            Supervisor = !_supervisorAgent.UseHeuristic,
            ModelName = GetModelName(),
            ResetPlatformToIdentity = _supervisorAgent.GetBallAgents()[0].ResetPlatformToIdentity,
            FocusActivePlatform = _supervisorAgent.FocusActiveTask,
            Autonomous = ballAgent.IsAutonomous,
            AgentChoice = _agentChoice,
            Aborted = aborted
        };

        _scores.Add(score);

        if (_supervisorAgent.FixedEpisodeDuration > MinimumScoreForMeasurement)
        {
            _episodeCount++;
            _countBallCenterDistance = (0, 0f);

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

    /// <summary>
    /// Writes (Counter, Distance to Center) to CountBallCenterDistance. This information is used to calculate the avverage distance of the Ball to the center of the platform.
    /// </summary>
    /// <param name="sensor"></param>
    /// <param name="ballAgent"></param>
    private void CollectBallDistanceToCenter(VectorSensor sensor, BallAgent ballAgent)
    {
        _countBallCenterDistance = (_countBallCenterDistance.Item1 + 1, _countBallCenterDistance.Item2 + Vector3.Distance(ballAgent.GetBallGlobalPosition(), ballAgent.GetPlatformGlobalPosition()));
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
    public float Drag { get; set; }
    public float AverageDistanceToCenter { get; set; }
    public bool Supervisor { get; set; }
    public string ModelName { get; set; }
    public bool ResetPlatformToIdentity { get; set; }
    public bool FocusActivePlatform { get; set; }
    public bool Autonomous { get; set; }
    public string AgentChoice { get; set; }
    public bool Aborted { get; set; }
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
        ReactionTime = switchingData.ReactionTime;
        TimeOnPreviousTask = switchingData.TimeOnPreviousTask;
        JoystickAxisX = switchingData.JoystickAxisX;
        JoystickAxisY = switchingData.JoystickAxisY;
        ActionZ = switchingData.ActionZ;
        ActionX = switchingData.ActionX;
        DragValue = switchingData.DragValue;
        PlatformAngleSourceX = switchingData.PlatformAngleSourceX;
        PlatformAngleSourceY = switchingData.PlatformAngleSourceY;
        PlatformAngleSourceZ = switchingData.PlatformAngleSourceZ;
        BallVelocitySourceX = switchingData.BallVelocitySourceX;
        BallVelocitySourceY = switchingData.BallVelocitySourceY;
        BallVelocitySourceZ = switchingData.BallVelocitySourceZ;
        BallPositionSourceX = switchingData.BallPositionSourceX;
        BallPositionSourceY = switchingData.BallPositionSourceY;
        BallPositionSourceZ = switchingData.BallPositionSourceZ;
        PlatformAngleTargetX = switchingData.PlatformAngleTargetX;
        PlatformAngleTargetY = switchingData.PlatformAngleTargetY;
        PlatformAngleTargetZ = switchingData.PlatformAngleTargetZ;
        BallVelocityTargetX = switchingData.BallVelocityTargetX;
        BallVelocityTargetY = switchingData.BallVelocityTargetY;
        BallVelocityTargetZ = switchingData.BallVelocityTargetZ;
        BallPositionTargetX = switchingData.BallPositionTargetX;
        BallPositionTargetY = switchingData.BallPositionTargetY;
        BallPositionTargetZ = switchingData.BallPositionTargetZ;
        Supervisor = switchingData.Supervisor;
        ModelName = switchingData.ModelName;
        AgentChoice = switchingData.AgentChoice;
}

    public string DateTime { get; set; }
    public string PlayerName { get; set; }
    public int EpisodeId { get; set; }
    public int SwitchingId { get; set; }
    public double ReactionTime { get; set; }
    public double TimeOnPreviousTask { get; set; }
    public float JoystickAxisX { get; set; }
    public float JoystickAxisY { get; set; }
    public float ActionZ { get; set; }
    public float ActionX { get; set; }
    public float DragValue { get; set; }
    public float PlatformAngleSourceX { get; set; }
    public float PlatformAngleSourceY { get; set; }
    public float PlatformAngleSourceZ { get; set; }
    public float BallVelocitySourceX { get; set; }
    public float BallVelocitySourceY { get; set; }
    public float BallVelocitySourceZ { get; set; }
    public float BallPositionSourceX { get; set; }
    public float BallPositionSourceY { get; set; }
    public float BallPositionSourceZ { get; set; }
    public float PlatformAngleTargetX { get; set; }
    public float PlatformAngleTargetY { get; set; }
    public float PlatformAngleTargetZ { get; set; }
    public float BallVelocityTargetX { get; set; }
    public float BallVelocityTargetY { get; set; }
    public float BallVelocityTargetZ { get; set; }
    public float BallPositionTargetX { get; set; }
    public float BallPositionTargetY { get; set; }
    public float BallPositionTargetZ { get; set; }
    public bool Supervisor { get; set; }
    public string ModelName { get; set; }
    public string AgentChoice { get; set; }
}