using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using TMPro;
using System.Diagnostics;
using Unity.MLAgents.Policies;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;


namespace Supervisor
{
    public class SupervisorAgent : Agent, ISupervisorAgent
    {
        [field: SerializeField, Tooltip("Score is shown in the top right corner if true."), ProjectAssign]
        public bool ShowScore { get; set; }

        [field: SerializeField, Tooltip("Focus active platfrom in heuristic mode."), ProjectAssign]
        public bool FocusActiveTask { get; set; }

        [field: SerializeField, Tooltip("Hide inactive platfrom."), ProjectAssign]
        public bool HideInactiveTasks { get; set; }

        [field: SerializeField, Tooltip("In case a task switch happens, the next requested decision is after SetConstantDecisionRequestInterval + AdvanceNoticeInSeconds."), ProjectAssign]
        public float AdvanceNoticeInSeconds { get; set; }

        [field: SerializeField, Tooltip("Notification to switch to a certain platform is given instead to a direct switch to the platform.")]
        public bool NotificationMode { get; set; }

        [field: SerializeField, Tooltip("Defines if the interval in which the agent should perform an action is constant (true) or a minimum value (false)."), ProjectAssign]
        public bool SetConstantDecisionRequestInterval { get; set; }

        [field: SerializeField, Tooltip("Defines the interval in which the agent should perform an action."), ProjectAssign]
        public float DecisionRequestIntervalInSeconds { get; set; }

        [field: SerializeField, Tooltip("The Interval in which the _dragDifficulty level should be increased."), ProjectAssign]
        public int DifficultyIncrementInterval { get; set; } = 15;

        [field: SerializeField, Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request a decision every 5 " +
            "Academy steps. The DecisionPeriod is ignored if SetConstantDecisionRequestInterval is true.")]
        public int DecisionPeriod { get; set; } = 5;

        [field: SerializeField, ProjectAssign]
        public float TimeScale { get; set; } = 1;

        [field: SerializeField, ProjectAssign]
        public int StartCountdownAt { get; set; } = 5;

        [field: SerializeField]
        public Text StopwatchText { get; set; }

        [field: SerializeField]
        public TextMeshProUGUI TextMeshProUGUI { get; set; }

        public int EpisodeCount { get; protected set; }

        public float FixedEpisodeDuration { get; protected set; }

        public bool UseHeuristic { get; protected set; }

        [field: SerializeField]
        public GameObject[] TaskGameObjects { get; set; }

        public ITask[] Tasks { get; set; }


        protected int _activeInstance;

        protected int _previousActive;

        protected int _pendingInstance;

        protected bool _isDifficultyUpdatedInCurrentInterval;

        protected float _fixedUpdateTimer = 0.0f;

        protected float _advanceNoticeTimer = 0.0f;

        protected Dictionary<InputAction, bool> _wasReleased;

        protected int _switchCount;

        protected float _timeSinceLastSwitch;

        protected AudioSource _audioSource;

        protected bool _isUserInput;

        protected float _fixedNotificationExecutionTimer = 0.0f;


        private Camera _mainCamera;

        private SupervisorControls _controls;

        private int _stepCounterDecisionRequester;

        private int _previousActiveInstance;

        private bool _taskSwitched;


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

        public int GetActiveTaskNumber()
        {
            return _activeInstance;
        }

        public override void OnEpisodeBegin()
        {
            OnStartEpisode?.Invoke();

            EpisodeCount++;

            Debug.Log(string.Format("Start Episode {0}!", EpisodeCount));
            FixedEpisodeDuration = 0;
            _isDifficultyUpdatedInCurrentInterval = false;

            _activeInstance = 0;
            _previousActive = 0;

            if (NotificationMode)
            {
                GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                _isUserInput = false;
            }

            SwitchAgentTo(_activeInstance);

            _switchCount = 1;
            _timeSinceLastSwitch = 0;

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

            _activeInstance = 0;
            _previousActive = 1;

            Tasks[_activeInstance].IsActive = true;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            foreach(ITask task in Tasks)
            {
                task.AddObservationsToSensor(sensor);
            }

            sensor.AddObservation(_switchCount);
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            Act(actionBuffers.DiscreteActions[0]);
            ResolveInteraction(actionBuffers.DiscreteActions[0]);
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
            //PreviousAction to prevent this default behaviour.
            discreteActionsOut[0] = _previousActive;

            ControlGamepad(discreteActionsOut);
            ControlKeyboard(discreteActionsOut);
        }


        protected virtual float GetReward()
        {
            float reward = (float)((DecisionRequestIntervalInSeconds / (1 + Math.Exp(-(Math.Exp(2) * (_timeSinceLastSwitch - 0.5))))));
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
            bool hasIntervalExpired = _fixedUpdateTimer > DecisionRequestIntervalInSeconds + _advanceNoticeTimer;

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

            if (hasIntervalExpired && !_taskSwitched)
            {
                _advanceNoticeTimer = 0;
                RequestSimpleDecision();
            }

            _taskSwitched = false;
        }

        protected virtual void SwitchingAction(int activeInstance)
        {
            _activeInstance = activeInstance;

            if (_previousActive != _activeInstance)
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
                    StartCoroutine(DelayedAgentSwitchTo(AdvanceNoticeInSeconds, _activeInstance));
                }
                else
                {
                    SwitchAgentTo(_activeInstance);
                }
            }

            _previousActive = activeInstance;
        }

        protected virtual void PropagateTimeBetweenSwitches()
        {
            InvokeOnTaskSwitchCompleted();
            _timeSinceLastSwitch = 0;
        }

        protected void InvokeOnTaskSwitchCompleted()
        {
            OnTaskSwitchCompleted?.Invoke(_timeSinceLastSwitch, this, false);
        }

        protected void Awake()
        {
            _controls = new SupervisorControls();
            Tasks = ITask.GetTasksFromGameObjects(TaskGameObjects);
        }

        protected override void OnEnable()
        {
            ITask.TaskCompleted += EndEpisodes;
            base.OnEnable();
            _controls.Heuristic.Enable();

            if (!ShowScore)
            {
                StopwatchText.text = "";
            }

            //-1 => Managed by ml-agents
            if (TimeScale != -1)
            {
                Time.timeScale = TimeScale;
            }
        }

        protected override void OnDisable()
        {
            ITask.TaskCompleted -= EndEpisodes;
            base.OnDisable();
            _controls.Heuristic.Disable();
            OnEndEpisode(true);
        }

        protected void EndEpisodes()
        {
            OnEndEpisode(false);
            base.EndEpisode();
        }

        protected void Act(int activeInstance)
        {
            if (NotificationMode)
            {
                NotificationAction(activeInstance);
            }
            else
            {
                SwitchingAction(activeInstance);
            }
        }

        protected void ResolveInteraction(int activeInstance)
        {
            float reward = GetReward();
            SetReward(reward);
            OnSetReward?.Invoke(reward);
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
            OnTaskSwitchFrom?.Invoke(Tasks[_previousActiveInstance]);
            _previousActiveInstance = activeInstance;
            _taskSwitched = true;

            foreach (ITask task in Tasks)
            {
                task.IsActive = false;
                VisualizeActiveStatusOfTasks(task);
            }

            Tasks[activeInstance].IsActive = true;
            VisualizeActiveStatusOfTasks(Tasks[activeInstance]);
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

            yield return new WaitForSeconds(AdvanceNoticeInSeconds);

            camera.clearFlags = CameraClearFlags.Skybox;
        }


        //_dragDifficulty is updated every 30 seconds. RequestDecision is handled manually here since DecisionRequestIntervalInSeconds is used (agent 
        //does not use Decision Requester script).
        private void FixedUpdate()
        {
            _fixedUpdateTimer += Time.fixedDeltaTime;
            _timeSinceLastSwitch += Time.fixedDeltaTime;
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

        private void LogPropertiesOfComponentsToFile()
        {
            GameObject agentGameObject = TaskGameObjects[0].transform.GetChildByName("Agent").gameObject;

            Type t = agentGameObject.GetComponent<BehaviorParameters>().GetType();
            LogToFile.LogPropertiesFieldsOfComponent(agentGameObject.GetComponent(t));

            t = this.GetComponent<BehaviorParameters>().GetType();
            LogToFile.LogPropertiesFieldsOfComponent(this.GetComponent(t));

            t = this.transform.GetChild(0).GetComponent<BehaviorParameters>().GetType();
            LogToFile.LogPropertiesFieldsOfComponent(this.transform.GetChild(0).GetComponent(t));

            LogToFile.LogPropertiesFieldsOfComponent(agentGameObject.GetComponent(typeof(ITask)));

            LogToFile.LogPropertiesFieldsOfComponent(this);
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
            if (ShowScore)
            {
                StopwatchText.text = Math.Round(GetCumulativeReward()).ToString() + " Points";
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

        protected virtual void NotificationAction(int targetInstance)
        {
            if (_previousActive != targetInstance)
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
                    _activeInstance = _previousActive = targetInstance;
                    SwitchAgentTo(_activeInstance);
                }
            }
        }

        private void ExecutePendingSwitch(int pendingInstance)
        {
            if (_isUserInput && _fixedNotificationExecutionTimer > 1 && NotificationMode)
            {
                _isUserInput = false;
                GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                SwitchAgentTo(pendingInstance);
                _previousActive = _activeInstance = pendingInstance;
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
                    discreteActionsOut[0] = _previousActive < Tasks.Length - 1 ? _previousActive + 1 : 0;
                }
                else
                {
                    discreteActionsOut[0] = _previousActive > 0 ? _previousActive - 1 : Tasks.Length - 1;
                }
            }
            else if (!inputAction.IsPressed())
            {
                _wasReleased[inputAction] = true;
            }
        }
    }
}