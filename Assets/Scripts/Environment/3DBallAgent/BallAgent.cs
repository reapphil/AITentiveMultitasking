using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using Random = UnityEngine.Random;
using UnityEngine.Assertions;
using System.Collections;

public abstract class BallAgent : Agent, IComparable<BallAgent>, IBallAgent, ITask
{
    [field: SerializeField, Tooltip("If autonomus is True than there is no supervised control. The agent continuously control the platform and does not give control to the " +
    "supervisor."), ProjectAssign(Header = "Balancing Task")]
    virtual public bool IsAutonomous { get; set; } = false;

    [field: SerializeField, Tooltip("Resets rotation of platform to its identity."), ProjectAssign]
    public bool ResetPlatformToIdentity { get; set; }

    [field: SerializeField, Tooltip("The speed how fast the platform reset to the horizontal position for the inactive platform."), ProjectAssign]
    public float ResetSpeed { get; set; } = 10;

    [field: SerializeField, Tooltip("ForceDifficulty is represented as the force releated to the velocity of the Ball.")]
    public int ForceDifficulty { get; set; } = 150;

    [field: SerializeField, Tooltip("Radius of the random starting ball position."), ProjectAssign]
    public float BallStartingRadius { get; set; } = 1f;

    [field: SerializeField, Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request a decision every 5 Academy steps."), ProjectAssign]
    public int DecisionPeriod { get; set; } = 5;

    [field: SerializeField, Header("Specific to Ball3D")]
    public GameObject Ball { get; set; }

    [field: SerializeField, Tooltip("If true the drag will get negative and therefore the velocity of the Ball is accelerated. Harder _dragDifficulty compared to only " +
    "positive drag value."), ProjectAssign]
    public bool UseNegativeDragDifficulty { get; set; }

    [field: SerializeField, Tooltip("Starting value of force added to ball if it fell below a certain threshold."), ProjectAssign]
    public int BallAgentDifficulty { get; set; } = 170;

    [field: SerializeField, Tooltip("Division factor of force during difficulty update. _dragDifficulty should be divided such that the incrementation of the drag is considered."), ProjectAssign]
    public double BallAgentDifficultyDivisionFactor { get; set; } = 1.05;

    [field: SerializeField, Tooltip("Initial drag value of the Ball, a high value means a easy _dragDifficulty. Drag can be used to slow down an object. The higher the drag " +
    "the more the object slows down."), ProjectAssign]
    public float GlobalDrag { get; set; }

    public static Vector3 BallStartingPosition { get; protected set; }

    virtual public bool IsVisible { get; protected set; }

    public bool IsFocused 
    {
        get => _isFocused;
        set
        {
            _isFocused = value;

            if (IsFocused)
            {
                GetGameObject().transform.parent.transform.GetChildByName("Eye_Canvas").gameObject.SetActive(true);
            }
            else
            {
                GetGameObject().transform.parent.transform.GetChildByName("Eye_Canvas").gameObject.SetActive(false);
            }
        }
    }

    virtual public bool IsActive { get; set; }


    protected Supervisor.ISupervisorAgent _supervisorAgent;

    protected Rigidbody _ballRb;

    protected EnvironmentParameters _resetParams;

    protected double _timeSinceLastSwitch;


    private bool _isFocused;

    private int _stepCounterDecisionRequester;

    private float _dragDifficulty;

    private MeshRenderer _cubeMeshRenderer;


    public delegate void OnActionReceivedAction(ActionBuffers actionBuffers, BallAgent ballAgent, double timeSinceLastSwitch=-1);
    public static event OnActionReceivedAction OnAction;

    public delegate void OnCollectObservations(VectorSensor sensor, BallAgent ballAgent);
    public static event OnCollectObservations OnCollectObservationsAction;


    //Initializes the Ball object based on the default values set in SetBall()
    public override void Initialize()
    {
        _cubeMeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _ballRb = Ball.GetComponent<Rigidbody>();

        _stepCounterDecisionRequester = 0;

        _resetParams = Academy.Instance.EnvironmentParameters;
        _supervisorAgent = gameObject.transform.root.GetComponent<Supervisor.SupervisorAgent>();

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
            Ball.transform.position = BallStartingPosition;
        }
    }

    //must be called in inheritaded classes. Actual functionality must be implemented in inheritaded classes.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        OnAction?.Invoke(actionBuffers, this, _timeSinceLastSwitch);
        //Debug.Log(string.Format("actionBuffers.ContinuousActions[0]: {0}\t actionBuffers.ContinuousActions[1]: {1}", actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]));
    }

    //must be called in inheritaded classes. Actual functionality must be implemented in inheritaded classes.
    public override void CollectObservations(VectorSensor sensor)
    {
        OnCollectObservationsAction?.Invoke(sensor, this);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
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
        return _ballRb.velocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return _ballRb.angularVelocity;
    }

    public float GetBallDrag()
    {
        return _ballRb.drag;
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
        return _ballRb.mass;
    }

    public float GetBallDistanceToCenter()
    {
        var curDist = Vector3.Distance(Ball.transform.localPosition, gameObject.transform.localPosition);
        //divided by the radius of the platform (which is 5)
        return curDist / (GetScale()/2);
    }

    public Vector3 GetBallDistanceToCenterVector3()
    {
        return Ball.transform.position - gameObject.transform.position;
    }

    public float GetAngularDrag()
    {
        return _ballRb.angularDrag;
    }

    public float GetDrag()
    {
        return _ballRb.drag;
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

    public void AddObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(GetPlatformAngle().z);
        sensor.AddObservation(GetPlatformAngle().x);
        sensor.AddObservation(GetBallDistanceToCenterVector3());
        sensor.AddObservation(GetBallVelocity());
    }

    public virtual void AddPerceivedObservationsToSensor(VectorSensor sensor)
    {
        AddObservationsToSensor(sensor);
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

    protected void UpdateTimeSinceLastSwitch()
    {
        if (IsActive)
        {
            _timeSinceLastSwitch += Time.fixedDeltaTime;
        }
        else
        {
            _timeSinceLastSwitch = 0;
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
            ITask.InvokeTaskCompletion();
        }
    }


    private void ResetEnvironmentParameters()
    {
        BallStartingPosition = new Vector3(Random.Range(-BallStartingRadius, BallStartingRadius), 4f, Random.Range(-BallStartingRadius, BallStartingRadius));
        Ball.transform.localPosition = BallStartingPosition + gameObject.transform.localPosition;

        gameObject.transform.rotation = Quaternion.identity;

        _ballRb.velocity = new Vector3(0f, 0f, 0f);
        _ballRb.drag = GlobalDrag;

        _timeSinceLastSwitch = 0;

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
        UpdateTimeSinceLastSwitch();
        PropagateTaskCompletion();

        if (IsAutonomous)
        {
            RequestSimpleDecision();
        }
        else if (IsActive)
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

    //Will be only called once during the initalization.
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
}