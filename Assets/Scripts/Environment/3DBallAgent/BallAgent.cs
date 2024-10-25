using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using Random = UnityEngine.Random;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public abstract class BallAgent : Agent, IComparable<BallAgent>, IBallAgent, ITask
{
    [field: SerializeField, Tooltip("If autonomous is True than there is no supervised control. The agent continuously control the platform and does " +
        "not give control to the supervisor."), ProjectAssign(Header = "Balancing Task")]
    virtual public bool IsAutonomous { get; set; } = false;

    [field: SerializeField, Tooltip("Determines if the supervisor should end its episode if the episode of the task ends"), ProjectAssign]
    public bool IsTerminatingTask { get; set; } = false;

    [field: SerializeField, Tooltip("Resets rotation of platform to its identity."), ProjectAssign]
    public bool ResetPlatformToIdentity { get; set; }

    [field: SerializeField, Tooltip("The speed how fast the platform reset to the horizontal position for the inactive platform."), ProjectAssign]
    public float ResetSpeed { get; set; } = 10;

    [field: SerializeField, Tooltip("ForceDifficulty is represented as the force related to the velocity of the Ball.")]
    public int ForceDifficulty { get; set; } = 150;

    [field: SerializeField, Tooltip("Radius of the random starting ball position."), ProjectAssign]
    public float BallStartingRadius { get; set; } = 1f;

    [field: SerializeField, Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request" +
        " a decision every 5 Academy steps."), ProjectAssign]
    public int DecisionPeriod { get; set; } = 5;

    [field: SerializeField, Header("Specific to Ball3D")]
    public GameObject Ball { get; set; }

    [field: SerializeField, Tooltip("If true the drag will get negative and therefore the velocity of the Ball is accelerated. Harder _dragDifficulty" +
        " compared to only positive drag value."), ProjectAssign]
    public bool UseNegativeDragDifficulty { get; set; }

    [field: SerializeField, Tooltip("Starting value of force added to ball if it fell below a certain threshold."), ProjectAssign]
    public int BallAgentDifficulty { get; set; } = 170;

    [field: SerializeField, Tooltip("Division factor of force during difficulty update. _dragDifficulty should be divided such that the " +
        "incrementation of the drag is considered."), ProjectAssign]
    public double BallAgentDifficultyDivisionFactor { get; set; } = 1.05;

    [field: SerializeField, Tooltip("Initial drag value of the Ball, a high value means a easy _dragDifficulty. Drag can be used to slow down an " +
        "object. The higher the drag the more the object slows down."), ProjectAssign]
    public float GlobalDrag { get; set; }

    public static Vector3 BallStartingPosition { get; protected set; }

    virtual public bool IsVisible { get; protected set; }

    virtual public bool IsActive { get; set; }

    public Queue<float> TaskRewardForSupervisorAgent { get; private set; }

    public Queue<float> TaskRewardForFocusAgent { get; private set; }

    public IStateInformation StateInformation { 
        get 
        {
            _ballStateInformation ??= new BallStateInformation();

            _ballStateInformation.ContinuousActionsX = _lastActionPerformed.x;
            _ballStateInformation.ContinuousActionsY = _lastActionPerformed.y;
            _ballStateInformation.DragValue = GetBallDrag();
            _ballStateInformation.PlatformAngleX = GetPlatformAngle().x;
            _ballStateInformation.PlatformAngleY = GetPlatformAngle().y;
            _ballStateInformation.PlatformAngleZ = GetPlatformAngle().z;
            _ballStateInformation.BallVelocityX = GetBallVelocity().x;
            _ballStateInformation.BallVelocityY = GetBallVelocity().y;
            _ballStateInformation.BallVelocityZ = GetBallVelocity().z;
            _ballStateInformation.BallPositionX = GetBallLocalPosition().x;
            _ballStateInformation.BallPositionY = GetBallLocalPosition().y;
            _ballStateInformation.BallPositionZ = GetBallLocalPosition().z;
                

            return _ballStateInformation;
        }
        set
        {
            _ballStateInformation = value as BallStateInformation;
        }
    }

    public Dictionary<string, double> Performance {
        get 
        {
            return new Dictionary<string, double>
            {
                { "AverageDistanceToCenter", _countBallCenterDistance.Item2 / _countBallCenterDistance.Item1 }
            };
        }
    
    }


    protected Supervisor.ISupervisorAgent _supervisorAgent;

    protected Rigidbody _ballRb;

    protected EnvironmentParameters _resetParams;


    private bool _isFocused;

    private int _stepCounterDecisionRequester;

    private float _dragDifficulty;

    private MeshRenderer _cubeMeshRenderer;

    private (int, float) _countBallCenterDistance;

    private BallStateInformation _ballStateInformation;

    private Vector2 _currentInput;

    private Vector2 _lastActionPerformed;

    public delegate void OnCollectObservations(VectorSensor sensor, BallAgent ballAgent);
    public static event OnCollectObservations OnCollectObservationsAction;


    public void OnMove(InputValue value)
    {
        _currentInput = value.Get<Vector2>();
    }

    //Initializes the Ball object based on the default values set in SetBall()
    public override void Initialize()
    {
        _cubeMeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _ballRb = Ball.GetComponent<Rigidbody>();

        _stepCounterDecisionRequester = 0;

        _resetParams = Academy.Instance.EnvironmentParameters;
        _supervisorAgent = gameObject.transform.root.GetComponent<Supervisor.SupervisorAgent>();

        BallStateInformation.PlatformRadius = GetScale() / 2f;

        TaskRewardForSupervisorAgent = new();
        TaskRewardForFocusAgent = new();

        SetResetParameters();
    }

    //Reset the environment if the agent acts autonomously, otherwise the supervisor resets the environment.
    public override void OnEpisodeBegin()
    {
        ResetEnvironmentParameters();
        InitDragDifficulty();

        if (IsAutonomous)
        {
            gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
            gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
            _ballRb.velocity = new Vector3(0f, 0f, 0f);
            Ball.transform.localPosition = BallStartingPosition;
        }
    }

    //must be called in inherited classes. Actual functionality must be implemented in inherited classes.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        List<dynamic> actions = new();
        var continuousActionsOut = actionBuffers.ContinuousActions;
        _lastActionPerformed = new Vector2(continuousActionsOut[0], continuousActionsOut[1]);

        actions.Add(_lastActionPerformed);

        ITask.InvokeOnAction(actions, this);
        //Debug.Log(string.Format("actionBuffers.ContinuousActions[0]: {0}\t actionBuffers.ContinuousActions[1]: {1}", actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]));
    }

    //must be called in inherited classes. Actual functionality must be implemented in inherited classes.
    public override void CollectObservations(VectorSensor sensor)
    {
        OnCollectObservationsAction?.Invoke(sensor, this);
        CollectBallDistanceToCenter();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = -_currentInput.x;
        continuousActionsOut[1] = _currentInput.y;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public int CompareTo(BallAgent other)
    {
        return this.IsAutonomous.CompareTo(other.IsAutonomous);
    }

    public GameObject GetBall()
    {
        return Ball;
    }

    public Vector3 GetBallLocalPosition()
    {
        return Ball.transform.localPosition;
    }

    public Vector3 GetBallGlobalPosition()
    {
        return Ball.transform.position;
    }

    public Vector3 GetPlatformGlobalPosition()
    {
        return gameObject.transform.position;
    }

    public Vector3 GetPlatformAngle()
    {
        return gameObject.transform.rotation.eulerAngles;
    }

    public float GetSlopeInDegrees()
    {
        return Vector3.Angle(gameObject.transform.up, Vector3.up);
    }

    public Vector3 GetBallVelocity()
    {
        return _ballRb != null ? _ballRb.velocity : default;
    }

    public Vector3 GetAngularVelocity()
    {
        return _ballRb != null ? _ballRb.angularVelocity : default;
    }

    public float GetBallDrag()
    {
        return _ballRb != null ? _ballRb.drag : default;
    }

    public Vector3 GetObservedBallPosition()
    {
        return GetBallGlobalPosition();
    }

    public Vector3 GetObservedBallVelocity()
    {
        return GetBallVelocity();
    }

    //platform has to be a circle
    public float GetScale()
    {
        Assert.AreEqual(gameObject.transform.localScale.x, gameObject.transform.localScale.z);

        return gameObject.transform.localScale.x;
    }

    public float GetMass()
    {
        return _ballRb != null ? _ballRb.mass : default;
    }

    public float GetBallDistanceToCenter()
    {
        return Vector3.Distance(GetBallGlobalPosition(), GetPlatformGlobalPosition());
    }

    public Vector3 GetBallDistanceToCenterVector3()
    {
        return gameObject.transform.position - Ball.transform.position;
    }

    public float GetAngularDrag()
    {
        return _ballRb != null ? _ballRb.angularDrag : default;
    }

    public void UpdateDifficultyLevel()
    {

        if (UseNegativeDragDifficulty)
        {
            _ballRb.drag -= _dragDifficulty;
        }
        else
        {
            _ballRb.drag /= _dragDifficulty;
        }

        if (ForceDifficulty > 50)
        {
            ForceDifficulty = (int)(ForceDifficulty / BallAgentDifficultyDivisionFactor);
        }

        Debug.Log(string.Format("Drag: {0}, ForceDifficulty: {1}", _ballRb.drag, ForceDifficulty));

        if (!UseNegativeDragDifficulty)
        {
            _dragDifficulty++;
        }
    }

    public void AddTrueObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(gameObject.transform.rotation.z);
        sensor.AddObservation(gameObject.transform.rotation.x);
        sensor.AddObservation(GetBallDistanceToCenterVector3());
        sensor.AddObservation(GetBallVelocity());
        //sensor.AddObservation(_ballRb.drag);
    }

    public virtual void AddBeliefObservationsToSensor(VectorSensor sensor)
    {
        AddTrueObservationsToSensor(sensor);
    }

    public void ResetPerformance()
    {
        _countBallCenterDistance = (0, 0f);
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += CatchEndEpisode;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= CatchEndEpisode;
    }

    protected void RotateZ(float actionZ)
    {
        if ((gameObject.transform.rotation.z < 0.25f && actionZ > 0f) ||
                    (gameObject.transform.rotation.z > -0.25f && actionZ < 0f))
        {
            gameObject.transform.Rotate(new Vector3(0, 0, 1), actionZ);
        }
    }

    protected void RotateX(float actionX)
    {
        if ((gameObject.transform.rotation.x < 0.25f && actionX > 0f) ||
                   (gameObject.transform.rotation.x > -0.25f && actionX < 0f))
        {
            gameObject.transform.Rotate(new Vector3(1, 0, 0), actionX);
        }
    }

    //Increases the force of the current forward direction of the Ball if there is a low velocity.
    protected void AddForceToBall()
    {
        if (Vector3.Magnitude(_ballRb.velocity) == 0)
        {
            _ballRb.AddRelativeForce(Vector3.forward * ForceDifficulty);
        }
        else if (Vector3.Magnitude(_ballRb.velocity) < 0.1f)
        {
            _ballRb.AddForce(_ballRb.velocity.normalized * ForceDifficulty);
        }
    }

    //Returns true in case the Ball is outside of the in the if-condition defined area
    protected bool HasLost()
    {
        float radius = GetScale() / 2;

        if ((Ball.transform.localPosition.y - gameObject.transform.localPosition.y) < -3f * radius / 5f ||
            Mathf.Abs(Ball.transform.localPosition.x - gameObject.transform.localPosition.x) > radius ||
            Mathf.Abs(Ball.transform.localPosition.z - gameObject.transform.localPosition.z) > radius)
        {
            Debug.Log("Ball is outside of the allowed area: episode ended...");

            return true;
        }

        return false;
    }

    protected void PropagateTaskCompletion()
    {
        if (HasLost())
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
    }


    private void ResetEnvironmentParameters()
    {
        BallStartingPosition = new Vector3(Random.Range(-BallStartingRadius, BallStartingRadius), 4f, Random.Range(-BallStartingRadius, BallStartingRadius));
        Ball.transform.localPosition = BallStartingPosition + gameObject.transform.localPosition;

        gameObject.transform.rotation = Quaternion.identity;

        _ballRb.velocity = new Vector3(0f, 0f, 0f);
        _ballRb.drag = GlobalDrag;

        ForceDifficulty = BallAgentDifficulty;
    }

    private void InitDragDifficulty()
    {
        if (UseNegativeDragDifficulty)
        {
            _dragDifficulty = 0.025f;
        }
        else
        {
            _dragDifficulty = 1;
        }
    }

    private void FixedUpdate()
    {
        AddForceToBall();
        PropagateTaskCompletion();

        if (IsAutonomous || IsActive)
        {
            RequestSimpleDecision();
        }

        _stepCounterDecisionRequester++;
    }

    private void Update()
    {
        if (ResetPlatformToIdentity)
        {
            if (!IsActive)
            {
                MoveTowardsIdentity(gameObject);
            }
        }
    }

    private void MoveTowardsIdentity(GameObject gameObject)
    {
        var step = ResetSpeed * Time.deltaTime;
        gameObject.transform.rotation = Quaternion.RotateTowards(gameObject.transform.rotation, Quaternion.identity, step);
        //gameObject.transform.rotation = Quaternion.identity;
    }

    //Set the attributes of the Ball by fetching the information from the academy. Currently the values are not set so that the default values
    //will be used (second argument).
    private void SetBall()
    {
        _ballRb.mass = _resetParams.GetWithDefault("mass", 1.0f);
        var scale = _resetParams.GetWithDefault("scale", 1.0f);
        Ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    //Will be only called once during the initialization.
    private void SetResetParameters()
    {
        SetBall();
    }

    private void RequestSimpleDecision()
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
    }

    private void CatchEndEpisode(object sender, bool aborted)
    {
        EndEpisode();
    }

    /// <summary>
    /// Writes (Counter, Distance to Center) to CountBallCenterDistance. This information is used to calculate the average distance of the Ball to the center of the platform.
    /// </summary>
    private void CollectBallDistanceToCenter()
    {
        _countBallCenterDistance = (_countBallCenterDistance.Item1 + 1, _countBallCenterDistance.Item2 + GetBallDistanceToCenter());
    }
}