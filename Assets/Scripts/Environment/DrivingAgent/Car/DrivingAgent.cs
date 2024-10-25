using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

public class DrivingAgent : Agent, ITask
{
    [field: SerializeField, Header("Settings"), ProjectAssign]
    public float MaxCarSpeed { get; set; } = 150;

    [field: SerializeField, Header("Realistic Car Controller")]
    // public GameObject carPrefab;
    public RCC_CarControllerV3 CarController { get; set; }

    [field: SerializeField]
    public GameManager GameManager { get; set; }

    [field: SerializeField]
    public RoadManager RoadManager { get; set; }

    [field: SerializeField, ProjectAssign]
    public bool IsAutonomous { get; set; }

    [field: SerializeField, Tooltip("Determines if the supervisor should end its episode if the episode of the task ends"), ProjectAssign]
    public bool IsTerminatingTask { get; set; } = false;

    [field: SerializeField, Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request " +
        "a decision every 5 Academy steps."), ProjectAssign]
    public int DecisionPeriod { get; set; } = 5;

    public float CarSpeed { get; set; } = 0f;

    public bool IsActive { get; set; }

    public Queue<float> TaskRewardForSupervisorAgent { get; private set; }

    public Queue<float> TaskRewardForFocusAgent { get; private set; }


    public IStateInformation StateInformation
    {
        get
        {
            _drivingStateInformation ??= new DrivingStateInformation();

            return _drivingStateInformation;
        }
        set
        {
            _drivingStateInformation = value as DrivingStateInformation;
        }
    }

    public Dictionary<string, double> Performance => new();

    protected Vector3 TargetPosition = Vector3.zero;

    protected float _steerInput = 0f;


    private DrivingStateInformation _drivingStateInformation;

    private float _accelerationInput = 0f;

    private float _brakeInput = 0f;

    private float _handbrakeInput = 0f;

    private int _currentStep = 0;

    private Vector2 _currentInput;


    public void OnMove(InputValue value) 
    {
        _currentInput = value.Get<Vector2>();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        Debug.Log("Begin Episode");

        GameManager.RestartSimulation();
        CarController.externalController = true;
        CarController.maxspeed = MaxCarSpeed;
        _currentStep = 0;
    }

    public void ResetPerformance() { }

    public override void CollectObservations(VectorSensor sensor)
    {
        AddTrueObservationsToSensor(sensor);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        List<dynamic> actions = new();
        var continuousActionsOut = actionBuffers.ContinuousActions;

        actions.Add(continuousActionsOut[0]);
        actions.Add(continuousActionsOut[1]);

        ITask.InvokeOnAction(actions, this);

        CarSpeed = CarController.speed;
        TargetPosition = GameManager.GetClosestPointOnLineSegment(transform.position);

        // Move the agent using the action.

        if (IsActive || IsAutonomous)
        {
            MoveAgent(actionBuffers.ContinuousActions);
        }

        float reward = GetReward();

        TaskRewardForFocusAgent.Enqueue(reward);
        TaskRewardForSupervisorAgent.Enqueue(reward);
        SetReward(reward);
    }

    public void AddTrueObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(TargetPosition);
        sensor.AddObservation(CarSpeed);
        sensor.AddObservation(GameManager.CurrentTargetSpeed);
    }

    public void UpdateDifficultyLevel() { }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = _currentInput.x;
        continuousActionsOut[1] = _currentInput.y;
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += CatchEndEpisode;
        GameManager.OnFinishReachedAction += CatchFinishReached;
        GetComponent<DecisionRequester>().DecisionPeriod = DecisionPeriod;
        TaskRewardForFocusAgent = new();
        TaskRewardForSupervisorAgent = new();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= CatchEndEpisode;
        GameManager.OnFinishReachedAction += CatchFinishReached;
    }

    protected void FixedUpdate()
    {
        if (transform.localPosition.x > 6.4 || transform.localPosition.x < -6.4 || (_currentStep > 45000 && CarController.speed < 10))
        {
            // "Apply negative rewards only to influencing agents. The driving agent won't learn from such a large negative reward, as episodes only
            // end when the goal is reached or the car crashes. Initially, only crashes end the episode, so progress would go unrecognized due to the
            // large penalty."
            TaskRewardForFocusAgent.Enqueue(-1000);
            TaskRewardForSupervisorAgent.Enqueue(-1000);

            if (IsTerminatingTask)
            {
                ITask.InvokeTermination();
            }
            else
            {
                EndEpisode();
            }

            Debug.Log("End Episode: car crashed or step count exceeded.");
        }

        _currentStep++;
    }


    private void CatchEndEpisode(object sender, bool aborted)
    {
        EndEpisode();
    }

    private void CatchFinishReached(object sender, bool aborted)
    {
        if (IsTerminatingTask)
        {
            ITask.InvokeTermination();
        }
        else
        {
            EndEpisode();
        }
    }

    private void ResetEnvironment()
    {
        RoadManager.InitializeRoad(false);

        RCC.Transport(CarController, new Vector3(GameManager.GetCurrentLaneLocation(gameObject).x, 0.75f, 0f), Quaternion.identity);
        CarController.Repair();
    }

    private float GetReward()
    {
        float distanceToTargetLaneCenter = Vector3.Distance(transform.position, TargetPosition);
        float distanceToTargetSpeed = Mathf.Abs(CarSpeed - GameManager.CurrentTargetSpeed);

        float maxDistanceToTargetSpeed = Math.Max(Math.Abs(GameManager.CurrentTargetSpeed), Math.Abs(GameManager.CurrentTargetSpeed - MaxCarSpeed));
        float maxDistanceToTargetLaneCenter = Math.Max(Math.Abs(TargetPosition.x - transform.parent.position.x - 6.5f), Math.Abs(TargetPosition.x - transform.parent.position.x + 6.5f)); //road with equals 13 with the center at position 0

        //MinMax scaling with a min of 0
        return (1 - distanceToTargetSpeed / maxDistanceToTargetSpeed) * (1 - distanceToTargetLaneCenter / maxDistanceToTargetLaneCenter);
    }

    private void MoveAgent(ActionSegment<float> actionBuffersContinuousActions)
    {
        //0 => steering
        //1 => speed
        //2 => break

        _steerInput = actionBuffersContinuousActions[0];
        _accelerationInput = actionBuffersContinuousActions[1];
        _brakeInput = actionBuffersContinuousActions[1];
        _handbrakeInput = 0f;

        //  Clamping inputs.
        _steerInput = Mathf.Clamp(_steerInput, -1f, 1f) * CarController.direction;
        _accelerationInput = Mathf.Clamp01(_accelerationInput);
        _brakeInput = Mathf.Abs(Mathf.Clamp(_brakeInput, -1, 0f));

        FeedRCC();
    }

    private void FeedRCC()
    {
        // Feeding throttleInput of the RCC.
        if (!CarController.changingGear && !CarController.cutGas)
            CarController.throttleInput =
                (CarController.direction == 1 ? Mathf.Clamp01(_accelerationInput) : Mathf.Clamp01(_brakeInput));
        else
            CarController.throttleInput = 0f;

        if (!CarController.changingGear && !CarController.cutGas)
            CarController.brakeInput =
                (CarController.direction == 1 ? Mathf.Clamp01(_brakeInput) : Mathf.Clamp01(_accelerationInput));
        else
            CarController.brakeInput = 0f;

        CarController.steerInput = _steerInput;

        CarController.handbrakeInput = _handbrakeInput;
    }
}