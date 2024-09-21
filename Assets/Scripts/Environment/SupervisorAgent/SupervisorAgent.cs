using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using TMPro;
using Unity.MLAgents.Policies;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;


namespace Supervisor
{
    public enum Mode
    {
        Force,
        Notification,
        Suggestion
    }


    public class SupervisorAgent : Agent, ISupervisorAgent
    {
        [field: SerializeField, Tooltip("Score is shown in the top right corner if true."), ProjectAssign]
        public bool ShowReward { get; set; }

        [field: SerializeField, Tooltip("Focus active platform in heuristic mode."), ProjectAssign]
        public bool FocusActiveTask { get; set; }

        [field: SerializeField, Tooltip("Hide inactive platform."), ProjectAssign]
        public bool HideInactiveTasks { get; set; }

        [field: SerializeField, Tooltip("In case a task switch happens, the next requested decision is after SetConstantDecisionRequestInterval + " +
            "AdvanceNoticeInSeconds."), ProjectAssign]
        public float AdvanceNoticeInSeconds { get; set; }

        [field: SerializeField, Tooltip("Mode of the supervisor: " +
            "Force -> automatic switch, the user cannot decide if the switch should be performed;   " +
            "Notification -> the user will be notified about upcoming switch and can perform the switch during 1 second " +
            "If the switch was not performed by the user, the switch is performed after expiry of this 1 second;   " +
            "Suggestion: the supervisor only suggestion a switch, the decision remains by the user")]
        public Mode Mode { get; set; }

        [field: SerializeField, Tooltip("Defines if the interval in which the agent should perform an action is constant (true) or a minimum value " +
            "(false)."), ProjectAssign]
        public bool SetConstantDecisionRequestInterval { get; set; }

        [field: SerializeField, Tooltip("Defines the interval in which the agent should perform an action."), ProjectAssign]
        public float DecisionRequestIntervalInSeconds { get; set; }

        [field: SerializeField, Tooltip("The Interval in which the _dragDifficulty level should be increased."), ProjectAssign]
        public int DifficultyIncrementInterval { get; set; } = 15;

        [field: SerializeField, Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will " +
            "request a decision every 5 Academy steps. The DecisionPeriod is ignored if SetConstantDecisionRequestInterval is true.")]
        public int DecisionPeriod { get; set; } = 5;

        [field: SerializeField, Tooltip("Defines the reward function of the supervisor agent. \"r_{number}\" is interpreted as the values returned by " +
            "the \"TaskRewardForSupervisorAgent\" queue of the specific tasks. {number} start with 0 and enumerates the tasks displayed from left to " +
            "right. All function of the Math library can be used (without Math. prefix). Furthermore, the following variables can be used: t_s: time " +
            "since last switch; t_d: decision request interval in seconds"), ProjectAssign]
        public string RewardFunction { get; set; } = "r_0";

        [field: SerializeField, ProjectAssign]
        public float TimeScale { get; set; } = 1;

        [field: SerializeField, ProjectAssign]
        public int StartCountdownAt { get; set; } = 5;

        [field: SerializeField, Tooltip("Must be defined for the training. For all other modes, the size is determined by the provided model.")]
        public int VectorObservationSize { get; set; }

        [field: SerializeField]
        public Text CumulativeRewardText { get; set; }

        [field: SerializeField]
        public Text CurrentRewardText { get; set; }

        [field: SerializeField]
        public TextMeshProUGUI TextMeshProUGUI { get; set; }

        public int EpisodeCount { get; protected set; }

        public float FixedEpisodeDuration { get; protected set; }

        public bool UseHeuristic { get; protected set; }

        [field: SerializeField]
        public GameObject[] TaskGameObjects { get; set; }

        [field: SerializeField]
        public GameObject[] TaskGameObjectsProjectSettingsOrdering { get; set; }

        public float TimeSinceLastSwitch { get; set; }

        public ITask[] Tasks 
        {
            get
            {
                return ITask.GetTasksFromGameObjects(TaskGameObjects);
            }
        }

        public ITask[] TasksProjectSettingsOrdering
        {
            get
            {
                return ITask.GetTasksFromGameObjects(TaskGameObjectsProjectSettingsOrdering);
            }
        }

        public string[] TaskNames
        {
            get
            {
                string[] taskName = new string[Tasks.Length];

                for (int i = 0; i < Tasks.Length; i++)
                {
                    taskName[i] = Tasks[i].GetType().Name;
                }

                return taskName;
            }
        }


        protected int _activeInstanceActionLevel;

        protected int _previousActiveActionLevel;

        private int _activeInstanceSwitchingLevel;

        protected int _previousActiveSwitchingLevel;

        protected int _pendingInstance;

        protected bool _isDifficultyUpdatedInCurrentInterval;

        protected float _fixedUpdateTimer = 0.0f;

        protected float _advanceNoticeTimer = 0.0f;

        protected Dictionary<InputAction, bool> _wasReleased;

        protected int _switchCount;

        protected AudioSource _audioSource;

        protected bool _isUserInput;

        protected float _fixedNotificationExecutionTimer = 0.0f;


        private Camera _mainCamera;

        private SupervisorControls _controls;

        private int _stepCounterDecisionRequester;

        private bool _taskSwitched;

        private bool _notificationActive;

        private float _collectedReward;

        private float _lastCollectedReward;


        public static event EventHandler<bool> EndEpisodeEvent;

        public delegate void StartEpisodeAction();
        public static event StartEpisodeAction OnStartEpisode;

        public delegate void TaskSwitchAction(double timeBetweenSwitches, ISupervisorAgent supervisorAgent, bool isNewEpisode);
        public static event TaskSwitchAction OnTaskSwitchCompleted;

        public delegate void TaskSwitchToAction(ITask task);
        public static event TaskSwitchToAction OnTaskSwitchTo;

        public delegate void TaskSwitchFromAction(ITask task);
        public static event TaskSwitchToAction OnTaskSwitchFrom;

        public delegate void SetRewardAction(float reward);
        public static event SetRewardAction OnSetReward;


        public static SupervisorAgent GetSupervisor(GameObject gameObject)
        {
            return gameObject.GetComponents<SupervisorAgent>().Where(x => x.enabled).First();
        }

        public ITask GetActiveTask()
        {
            foreach (ITask task in Tasks)
            {
                if (task.IsActive)
                {
                    return task;
                }
            }

            return null;
        }

        public List<T> GetTask<T>()
        {
            List<T> result = new();

            foreach(ITask task in Tasks)
            {
                if (task.GetType() == typeof(T))
                {
                    result.Add((T)task);
                }
            }

            return result;
        }

        public int GetActiveTaskNumber()
        {
            return _activeInstanceActionLevel;
        }

        public int GetPreviousActiveTaskNumber()
        {
            return _previousActiveSwitchingLevel;
        }

        public int GetTaskNumber(ITask task)
        {
            return Array.IndexOf(Tasks, task);
        }

        public override void OnEpisodeBegin()
        {
            OnStartEpisode?.Invoke();

            EpisodeCount++;

            Debug.Log(string.Format("Start Episode {0}!", EpisodeCount));
            FixedEpisodeDuration = 0;
            _isDifficultyUpdatedInCurrentInterval = false;

            _activeInstanceActionLevel = 0;
            _previousActiveActionLevel = 0;

            if (Mode == Mode.Notification)
            {
                GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                _isUserInput = false;
            }

            Act(_activeInstanceActionLevel);

            _switchCount = 1;
            TimeSinceLastSwitch = 0;

            RunCountDown();
        }

        public override void Initialize()
        {
            _stepCounterDecisionRequester = 0;
            _mainCamera = Camera.main;
            UseHeuristic = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType.Equals(Unity.MLAgents.Policies.BehaviorType.HeuristicOnly);
            _wasReleased = new Dictionary<InputAction, bool>();

            foreach (ITask task in Tasks)
            {
                task.IsActive = false;
            }

            _activeInstanceActionLevel = 0;
            _previousActiveActionLevel = 1;

            Tasks[_activeInstanceActionLevel].IsActive = true;

            InitMode();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            foreach (ITask task in Tasks)
            {
                task.AddTrueObservationsToSensor(sensor);
            }

            sensor.AddObservation(_switchCount);
            sensor.AddObservation(TimeSinceLastSwitch);
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            //SumOfDiscreteBranchSizes only restricts the actions to be performed for the training. If the model was trained with more actions,
            //actionBuffers length still can be greater than SumOfDiscreteBranchSizes. The if condition is added to prevent an error in this case,
            //for instance if the task runs autonomously anyways and the input of the supervisor should be ignored.
            if (actionBuffers.DiscreteActions[0] < GetComponent<BehaviorParameters>().BrainParameters.ActionSpec.SumOfDiscreteBranchSizes)
            {
                Act(actionBuffers.DiscreteActions[0]);
                ResolveInteraction(actionBuffers.DiscreteActions[0]);
            }
        }

        public void OnEndEpisode(bool aborted)
        {
            EndEpisodeEvent?.Invoke(this, aborted);
        }

        public IEnumerator DelayedAgentSwitchTo(float t, int activeInstance)
        {
            StartCoroutine(Notification(Tasks[activeInstance]));
            yield return StartCoroutine(Delay(t, activeInstance));
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;

            //If discreteActionsOut[0] wont be set, then discreteActionsOut[0] = 0. Therefore the following line sets discreteActionsOut[0] to the
            //PreviousAction to prevent this default behavior.
            discreteActionsOut[0] = _activeInstanceActionLevel;

            ControlGamepad(discreteActionsOut);
            ControlKeyboard(discreteActionsOut);
        }


        protected virtual float PeekLastRewards()
        {
            Dictionary<string, object> parameters = new();

            for (int i = 0; i < Tasks.Length; i++)
            {
                parameters.Add($"r_{i}", TasksProjectSettingsOrdering[i].TaskRewardForSupervisorAgent.IsNullOrEmpty() ? 0 : TasksProjectSettingsOrdering[i].TaskRewardForSupervisorAgent.ElementAt(TasksProjectSettingsOrdering[i].TaskRewardForSupervisorAgent.Count-1));
            }

            parameters.Add("t_s", TimeSinceLastSwitch);
            parameters.Add("t_d", DecisionRequestIntervalInSeconds);

            float reward = (float)FunctionInterpreter.Interpret(RewardFunction, parameters);

            return reward;
        }

        protected virtual float DequeueReward()
        {
            Dictionary<string, object> parameters = new();

            for (int i = 0; i < Tasks.Length; i++)
            {
                parameters.Add($"r_{i}", TasksProjectSettingsOrdering[i].TaskRewardForSupervisorAgent.IsNullOrEmpty() ? 0 : TasksProjectSettingsOrdering[i].TaskRewardForSupervisorAgent.DequeueAll().Sum());
            }

            parameters.Add("t_s", TimeSinceLastSwitch);
            parameters.Add("t_d", DecisionRequestIntervalInSeconds);

            float reward = (float)FunctionInterpreter.Interpret(RewardFunction, parameters);

            return reward;
        }

        protected virtual void SwitchAgentTo(int activeInstance)
        {
            UpdateAgentsActiveStatus(activeInstance);
            
            if(_switchCount != 0)
            {
                PropagateTimeBetweenSwitches();
            }

            _switchCount += 1;

            if (FocusActiveTask)
            {
                FocusActiveInstance();
            }

            if (HideInactiveTasks) 
            {
                HideInactiveInstance();    
            }
        }

        protected virtual void RequestInteractionAfterInterval()
        {
            bool hasIntervalExpired = _fixedUpdateTimer > DecisionRequestIntervalInSeconds + AdvanceNoticeInSeconds;

            if (SetConstantDecisionRequestInterval)
            {
                RequestInteractionAfterConstantInterval(hasIntervalExpired);
            }
            else
            {
                RequestInteractionAfterVariableInterval(hasIntervalExpired);
            }
        }

        protected virtual void RequestInteractionAfterConstantInterval(bool hasIntervalExpired)
        {
            if (hasIntervalExpired)
            {
                _advanceNoticeTimer = 0;
                RequestDecision();
                _fixedUpdateTimer = 0;
            }
        }

        protected virtual void RequestInteractionAfterVariableInterval(bool hasIntervalExpired)
        {
            //New decision is requested only after the number of seconds defined in DecisionRequestIntervalInSeconds after a task switch. 
            if (_taskSwitched)
            {
                _fixedUpdateTimer = 0;
            }

            if (hasIntervalExpired && !_taskSwitched && !_notificationActive)
            {
                _advanceNoticeTimer = 0;
                RequestSimpleDecision();
            }

            _taskSwitched = false;
        }

        protected virtual void PropagateTimeBetweenSwitches()
        {
            InvokeOnTaskSwitchCompleted();
            TimeSinceLastSwitch = 0;
        }

        protected void InvokeOnTaskSwitchCompleted()
        {
            OnTaskSwitchCompleted?.Invoke(TimeSinceLastSwitch, this, false);
        }

        protected void Awake()
        {
            _controls = new SupervisorControls();
        }

        protected override void OnEnable()
        {
            ITask.OnTermination += EndEpisodes;
            base.OnEnable();
            _controls.Heuristic.Enable();

            if (!ShowReward)
            {
                CumulativeRewardText.text = "";
                CurrentRewardText.text = "";
            }

            //-1 => Managed by ml-agents
            if (TimeScale != -1)
            {
                Time.timeScale = TimeScale;
            }
        }

        protected override void OnDisable()
        {
            ITask.OnTermination -= EndEpisodes;
            base.OnDisable();
            _controls.Heuristic.Disable();
            OnEndEpisode(true);
        }

        protected void EndEpisodes()
        {
            OnEndEpisode(false);

            foreach (ITask task in Tasks)
            {
                while (task.TaskRewardForSupervisorAgent.Count > 0)
                {
                    SetReward(task.TaskRewardForSupervisorAgent.DequeueAll().Sum());
                }
            }

            EndEpisode();
        }

        protected void Act(int activeInstance)
        {
            switch (Mode)
            {
                case Mode.Force:
                    SwitchingAction(activeInstance);
                    break;
                case Mode.Notification:
                    NotificationAction(activeInstance);
                    break;
                case Mode.Suggestion:
                    SuggestionAction(activeInstance);
                    break;
            }
        }

        protected void ResolveInteraction(int activeInstance)
        {
            _lastCollectedReward = PeekLastRewards();
            _collectedReward = DequeueReward();
            SetReward(_collectedReward);
            OnSetReward?.Invoke(_collectedReward);
        }

        protected void RequestInteraction()
        {
            if (GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType.Equals(Unity.MLAgents.Policies.BehaviorType.HeuristicOnly))
            {
                RequestDecision();
            }
            else
            {
                RequestInteractionAfterInterval();
            }
        }

        protected void RequestSimpleDecision()
        {
            if (_stepCounterDecisionRequester >= DecisionPeriod)
            {
                _stepCounterDecisionRequester = 0;
                RequestDecision();
            }
            else
            {
                RequestAction();
            }
            _stepCounterDecisionRequester++;
        }

        protected void UpdateDifficultyLevel()
        {
            if ((Convert.ToInt32(FixedEpisodeDuration) % DifficultyIncrementInterval) == 0)
            {
                if (!_isDifficultyUpdatedInCurrentInterval)
                {
                    foreach (ITask task in Tasks)
                    {
                        task.UpdateDifficultyLevel();
                    }

                    _isDifficultyUpdatedInCurrentInterval = true;
                }
            }
            else _isDifficultyUpdatedInCurrentInterval = false;
        }

        protected void UpdateAgentsActiveStatus(int activeInstance)
        {
            OnTaskSwitchTo?.Invoke(Tasks[activeInstance]);
            OnTaskSwitchFrom?.Invoke(Tasks[_activeInstanceSwitchingLevel]);
            _previousActiveSwitchingLevel = _activeInstanceSwitchingLevel;
            _activeInstanceSwitchingLevel = activeInstance;
            _taskSwitched = true;

            foreach (ITask task in Tasks)
            {
                task.IsActive = false;
                VisualizeActiveStatusOfTasks(task);
            }

            Tasks[activeInstance].IsActive = true;
            VisualizeActiveStatusOfTasks(Tasks[activeInstance]);
        }

        protected void UpdateAgentsFocusStatus(int activeInstance)
        {
            OnTaskSwitchTo?.Invoke(Tasks[activeInstance]);
            OnTaskSwitchFrom?.Invoke(Tasks[_activeInstanceSwitchingLevel]);
            _previousActiveSwitchingLevel = _activeInstanceSwitchingLevel;
            _activeInstanceSwitchingLevel = activeInstance;
            _taskSwitched = true;

            foreach (ITask task in Tasks)
            {
                if (task.GetType() == typeof(ICrTask))
                {
                    ICrTask crTask = (ICrTask)task;
                    crTask.FocusStateSpace.DeactivateAllElements();
                }

                task.IsActive = false;
                VisualizeSuggestionStatusOfTasks(task);
            }

            if (Tasks[activeInstance].GetType() == typeof(ICrTask))
            {
                ICrTask crTask = (ICrTask)Tasks[activeInstance];
                crTask.FocusStateSpace.ActivateAllElements();
            }

            Tasks[activeInstance].IsActive = true;
            VisualizeSuggestionStatusOfTasks(Tasks[activeInstance]);
        }

        protected void FocusActiveInstance()
        {
            foreach(ITask task in Tasks)
            {
                Camera camera = task.GetGameObject().transform.parent.GetChildByName("Camera").GetComponent<Camera>();

                if (task.IsActive)
                {
                    camera.enabled = true;
                    camera.rect = new Rect(0, 0, 1, 1);
                }
                else
                {
                    camera.enabled = false;
                }
            }
        }

        protected void HideInactiveInstance()
        {
            foreach (ITask task in Tasks)
            {
                foreach (Renderer renderer in task.GetGameObject().transform.parent.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = task.IsActive;
                }
            }
        }

        protected IEnumerator Notification(ITask task)
        {
            Camera camera = task.GetGameObject().transform.parent.GetChildByName("Camera").GetComponent<Camera>();

            camera.clearFlags = CameraClearFlags.SolidColor;

            _notificationActive = true;

            yield return new WaitForSeconds(AdvanceNoticeInSeconds);

            _notificationActive = false;

            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private void InitMode()
        {
            switch (Mode) 
            {
                case Mode.Suggestion:
                    InitSuggestionMode();
                    break;
                default:
                    InitForceMode();
                    break;
            }
        }

        private void InitSuggestionMode()
        {
            foreach (ITask task in Tasks)
            {
                if (Mode == Mode.Suggestion)
                {
                    task.IsAutonomous = true;
                }

                task.GetGameObject().transform.parent.GetChildByName("Camera").GetChildByName("Frame").gameObject.SetActive(true);
            }
        }

        private void InitForceMode()
        {
            foreach(ITask task in Tasks)
            {
                PostProcessLayer postProcessLayer = task.GetGameObject().transform.parent.GetChildByName("Camera").GetComponent<PostProcessLayer>();
                postProcessLayer.volumeLayer = task.IsActive ? (1 << 0) : (1 << 3);
            }
        }


        //_dragDifficulty is updated every 30 seconds. RequestDecision is handled manually here since DecisionRequestIntervalInSeconds is used (agent 
        //does not use Decision Requester script).
        private void FixedUpdate()
        {
            _fixedUpdateTimer += Time.fixedDeltaTime;
            TimeSinceLastSwitch += Time.fixedDeltaTime;
            _fixedNotificationExecutionTimer += Time.fixedDeltaTime;
            FixedEpisodeDuration += Time.fixedDeltaTime;

            UpdateDifficultyLevel();
            RequestInteraction();
            ExecutePendingSwitch(_pendingInstance);
        }

        private void VisualizeActiveStatusOfTasks(ITask task)
        {
            PostProcessLayer postProcessLayer = task.GetGameObject().transform.parent.GetChildByName("Camera").GetComponent<PostProcessLayer>();
            postProcessLayer.volumeLayer = task.IsActive ? (1 << 0) : (1 << 3);
        }

        private void VisualizeSuggestionStatusOfTasks(ITask task)
        {
            GameObject imageGameObject = task.GetGameObject().transform.parent.GetChildByName("Camera").GetChildByName("Frame").GetChildByName("Image").gameObject;
            Image image = imageGameObject.GetComponent<Image>();
            BlinkingImageAnimation blinkingImageAnimation = imageGameObject.GetComponent<BlinkingImageAnimation>();

            if (task.IsActive)
            {
                image.color = new Color(0, 255, 0, 1);
                blinkingImageAnimation.FadeSpeed = 3;
            }
            else
            {
                image.color = new Color(255, 0, 0, 1);
                blinkingImageAnimation.FadeSpeed = 1;
            }
        }

        private void LogPropertiesOfComponentsToFile()
        {
            //supervisorAgent
            LogToFile.LogPropertiesFieldsOfObject(this.GetComponent<BehaviorParameters>());

            //focusAgent
            LogToFile.LogPropertiesFieldsOfObject(this.transform.GetChild(0).GetComponent<BehaviorParameters>());

            TaskGameObjects.DistinctBy(x => x.transform.GetChildByName("Agent").GetComponent<ITask>().GetType()).ToList().ForEach(taskGameObject =>
            {
                GameObject agent = taskGameObject.transform.GetChildByName("Agent").gameObject;
                ITask task = agent.GetComponent<ITask>();

                LogToFile.LogPropertiesFieldsOfObject(task);
                LogToFile.LogPropertiesFieldsOfObject(task.StateInformation);
                LogToFile.LogPropertiesFieldsOfObject(agent.GetComponent<BehaviorParameters>());
            });


            LogToFile.LogPropertiesFieldsOfObject(this);
            LogToFile.LogPropertiesFieldsOfObject(transform.GetChildByName("FocusAgent").GetComponent<FocusAgent>());
        }

        private void RunCountDown()
        {
            if (TimeScale == 1)
            {
                if (StartCountdownAt > 0)
                {
                    if (EpisodeCount == 1)
                    {
                        StartCoroutine(Countdown(StartCountdownAt * 4));
                    }
                    else
                    {
                        StartCoroutine(Countdown(StartCountdownAt));
                    }
                }
            }
        }

        private void Start()
        {
            EpisodeCount = 0;
            _isUserInput = false;
            _audioSource = GetComponent<AudioSource>();
            LogPropertiesOfComponentsToFile();
        }

        //Prints the current reward to Canvas
        private void Update()
        {
            if (ShowReward)
            {
                CumulativeRewardText.text = string.Format("Cumulative Reward:\t{0}", Math.Round(GetCumulativeReward(), 2).ToString());
                CurrentRewardText.text = string.Format("Current Reward:\t{0}", Math.Round(_lastCollectedReward, 2).ToString());
            }
        }

        private IEnumerator Countdown(float seconds)
        {
            CountdownTimer countdownTimer = TextMeshProUGUI.GetComponent<CountdownTimer>();
            countdownTimer.StartCountDown(seconds);

            Time.timeScale = 1/(seconds*2);

            yield return new WaitUntil(() => countdownTimer.CurrentTime == 0);

            Time.timeScale = TimeScale;
        }

        protected virtual void SwitchingAction(int targetInstance)
        {
            if (targetInstance != _activeInstanceActionLevel)
            {
                if (!(GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly))
                {
                    if (_audioSource != null)
                    {
                        _audioSource.Play();
                    }
                }

                if (AdvanceNoticeInSeconds > 0)
                {
                    _advanceNoticeTimer = AdvanceNoticeInSeconds;
                    StartCoroutine(DelayedAgentSwitchTo(AdvanceNoticeInSeconds, targetInstance));
                }
                else
                {
                    SwitchAgentTo(targetInstance);
                }
            }

            _activeInstanceActionLevel = targetInstance;
        }

        protected virtual void NotificationAction(int targetInstance)
        {
            if (_activeInstanceActionLevel != targetInstance)
            {
                if (!_isUserInput)
                {
                    GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;

                    StartCoroutine(Notification(Tasks[targetInstance]));

                    if (_audioSource != null)
                    {
                        _audioSource.Play();
                    }

                    _isUserInput = true;
                    _fixedNotificationExecutionTimer = 0;
                    _pendingInstance = targetInstance;
                }
                else
                {
                    _isUserInput = false;
                    GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                    _previousActiveActionLevel = _activeInstanceActionLevel;
                    _activeInstanceActionLevel = targetInstance;
                    SwitchAgentTo(_activeInstanceActionLevel);
                }
            }
        }

        protected virtual void SuggestionAction(int targetInstance)
        {
            if (targetInstance != _activeInstanceActionLevel)
            {
                if (!(GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly))
                {
                    if (_audioSource != null)
                    {
                        _audioSource.Play();
                    }
                }

                if (_switchCount != 0)
                {
                    PropagateTimeBetweenSwitches();
                }

                UpdateAgentsFocusStatus(targetInstance);
            }

            _activeInstanceActionLevel = targetInstance;
        }


        private void ExecutePendingSwitch(int pendingInstance)
        {
            if (_isUserInput && _fixedNotificationExecutionTimer > 1 && Mode == Mode.Notification)
            {
                _isUserInput = false;
                GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                SwitchAgentTo(pendingInstance);
                _previousActiveActionLevel = _activeInstanceActionLevel;
                _activeInstanceActionLevel = pendingInstance;
            }
        }

        private IEnumerator Delay(float t, int activeInstance)
        {
            yield return new WaitForSeconds(t);

            SwitchAgentTo(activeInstance);
        }

        private void ControlGamepad(ActionSegment<int> discreteActionsOut)
        {
            PerformInput(_controls.Heuristic.SwitchLeft, discreteActionsOut, false);
            PerformInput(_controls.Heuristic.SwitchRight, discreteActionsOut, true);
        }

        private void ControlKeyboard(ActionSegment<int> discreteActionsOut)
        {
            PerformInput(_controls.Heuristic.Switch, discreteActionsOut);
        }

        private void PerformInput(InputAction inputAction, ActionSegment<int> discreteActionsOut, bool isSwitchedToRight = true)
        {
            if (inputAction.IsPressed() && _wasReleased[inputAction])
            {
                _wasReleased[inputAction] = false;

                if (isSwitchedToRight)
                {
                    discreteActionsOut[0] = _activeInstanceActionLevel < Tasks.Length - 1 ? _activeInstanceActionLevel + 1 : 0;
                }
                else
                {
                    discreteActionsOut[0] = _activeInstanceActionLevel > 0 ? _activeInstanceActionLevel - 1 : Tasks.Length - 1;
                }
            }
            else if (!inputAction.IsPressed())
            {
                _wasReleased[inputAction] = true;
            }
        }
    }
}