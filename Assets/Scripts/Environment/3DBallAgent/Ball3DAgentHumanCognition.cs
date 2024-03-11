using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Jobs;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

public class Ball3DAgentHumanCognition : BallAgent, ICrTask
{
    [field: SerializeField, Tooltip("Update period of the location probabilities of the Ball."), ProjectAssign(Header = "Human Cognition Model")]
    public float UpdatePeriod { get; set; } = 0.1f;

    [field: SerializeField, Tooltip("Defines how much samples should be taken to calculate the probability distributions."), ProjectAssign]
    public int NumberOfSamples { get; set; } = 100;

    [field: SerializeField, Tooltip("Sigma value used for the calculation of the normal distribution of the location probabilities."), ProjectAssign]
    public double Sigma { get; set; } = 0.1;

    [field: SerializeField, Tooltip("Sigma value used for the calculation of the mean value which is used to calculate the normal distribution of the location probabilities."), ProjectAssign]
    public double SigmaMean { get; set; } = 0.01;

    [field: SerializeField, Tooltip("Describes O(s,a,o) of the formula b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)}."), ProjectAssign]
    public double ObservationProbability { get; set; } = 0.9;

    [field: SerializeField, Tooltip("Seconds in which O(s,a,o)=1 still applies after a task switch."), ProjectAssign]
    public double ConstantReactionTime { get; set; } = 0;

    [field: SerializeField, Tooltip("Time in seconds in which the target agent acts based on the distribution of the source agent."), ProjectAssign]
    public float OldDistributionPersistenceTime { get; set; } = 0;

    [field: SerializeField, Tooltip("Observation is active for both agents."), ProjectAssign]
    public bool FullVision { get; set; } = false;

    [field: SerializeField, Tooltip("Specify the number of bins in which the platform should be divided."), ProjectAssign]
    public int NumberOfBins { get; set; } = 1000;

    [field: SerializeField, Tooltip("Visualizes the current belief position of the Ball (grey Ball), the Ball's average velocity based on the defined normal distribution (black line) and the direction of the bin with the highest prbability (red line)."), ProjectAssign]
    public bool ShowBeliefState { get; set; }

    [field: SerializeField, Tooltip("If active the observations of the ballagents are provided by the focus- instead of the supervisor agent."), ProjectAssign]
    public bool UseFocusAgent { get; set; }

    public override bool IsVisible
    {
        get
        {
            if (IsFocused || FullVision)
            {
                return true;
            }
            else
            {
                return _visibilityTimer > ConstantReactionTime && _visibilityTimer > OldDistributionPersistenceTime;
            }
        }
    }


    protected double[] _ballLocationProbabilities;

    protected float _platformRadius;

    protected int _numberOfBins;


    private static readonly ProfilerMarker s_calculateNormalDistributionForVelocityPerfMarker = new ProfilerMarker("UpdateBallLocationProbabilities.CalculateNormalDistributionForVelocity");
    private static readonly ProfilerMarker s_calculateTransitionProbabilitiesPerfMarker = new ProfilerMarker("UpdateBallLocationProbabilities.CalculateTransitionProbabilities");
    private static readonly ProfilerMarker s_updateBallLocationProbabilitiesPerfMarker = new ProfilerMarker("UpdateBallLocationProbabilities.UpdateBallLocationProbabilities");
    private static readonly ProfilerMarker s_normalizationPerfMarker = new ProfilerMarker("UpdateBallLocationProbabilities.Normalization");


    private FocusAgent _focusAgent;
    
    private GameObject _beliefBallPosition;

    private LineRenderer _averageLine;

    private LineRenderer _maxProbBinLine;

    private int _numberOFBinsPerDirection;

    private BallAgent _sourceBallAgent;

    private float _visibilityTimer = 0.0f;

    private float _updateTimer = 0.0f;

    private float _oldDistributionPersistenceTimer = 0.0f;

    private double _currentSigmaMean = 0;

    private Vector3 _estimatedVelocity;

    private Vector3 _averageVelocity;

    private System.Random _rand;

    private int _stepCountDecisionRequester;


    //Initializes the Ball object based on the default values set in SetBall()
    public override void Initialize()
    {
        base.Initialize();

        InitializeBallLocationProbabilities(_numberOfBins, _platformRadius);

        _focusAgent = gameObject.transform.parent.parent.GetComponent<FocusAgent>();

        if (ShowBeliefState)
        {
            _beliefBallPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _beliefBallPosition.transform.SetParent(transform.parent);
            _beliefBallPosition.GetComponent<SphereCollider>().enabled = false;
            _beliefBallPosition.transform.localPosition = GetBallBeliefPosition();

            _averageLine = gameObject.AddComponent<LineRenderer>();
            _maxProbBinLine = Ball.AddComponent<LineRenderer>();
            _averageLine.material = new Material(Shader.Find("Sprites/Default"));
            _maxProbBinLine.material = new Material(Shader.Find("Sprites/Default"));
            _maxProbBinLine.startWidth = _averageLine.startWidth = 0.1f;
            _maxProbBinLine.endWidth = _averageLine.endWidth = 0.1f;
            _maxProbBinLine.useWorldSpace = _averageLine.useWorldSpace = true;
            _averageLine.startColor = Color.black;
            _averageLine.endColor = Color.black;
            _maxProbBinLine.startColor = Color.red;
            _maxProbBinLine.endColor = Color.red;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(gameObject.transform.rotation.z);
        sensor.AddObservation(gameObject.transform.rotation.x);
        sensor.AddObservation(GetBallBeliefPosition() - gameObject.transform.position);
        sensor.AddObservation(_averageVelocity);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        _ballRb.WakeUp();

        RotateZ(2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f));
        RotateX(2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f));

        if (IsAutonomous)
        {
            if ((Ball.transform.position.y - gameObject.transform.position.y) < -2f ||
                Mathf.Abs(Ball.transform.position.x - gameObject.transform.position.x) > 5f ||
                Mathf.Abs(Ball.transform.position.z - gameObject.transform.position.z) > 5f)
            {
                EndEpisode();
            }
        }

        var curDist = Vector3.Distance(Ball.transform.localPosition, gameObject.transform.localPosition);
        //divided by the radius of the platform (which is 5)
        SetReward(1 - curDist / 5);
    }

    //returns bin for the position with the highest probability
    public Vector3 GetBallBeliefPosition()
    {
        double maxValue = _ballLocationProbabilities.Max();
        int maxIndex = _ballLocationProbabilities.ToList().IndexOf(maxValue);

        if (_oldDistributionPersistenceTimer < OldDistributionPersistenceTime && _sourceBallAgent is not null && _sourceBallAgent != this)
        {
            return ((Ball3DAgentHumanCognition)_sourceBallAgent).GetBallBeliefPosition();
        }

        return PositionConverter.BinToCoordinates(maxIndex, _platformRadius, _numberOFBinsPerDirection, Ball.transform.position.y);
    }

    public new Vector3 GetObservedBallPosition()
    {
        return GetBallBeliefPosition();
    }

    public new Vector3 GetObservedBallVelocity()
    {
        return _averageVelocity;
    }

    public override void AddPerceivedObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(GetObservedBallPosition() - gameObject.transform.position);
        sensor.AddObservation(GetObservedBallVelocity());
        sensor.AddObservation(GetPlatformAngle().z);
        sensor.AddObservation(GetPlatformAngle().x);
    }


    protected void Awake()
    {
        Assert.AreEqual(this.transform.localScale.z, this.transform.localScale.x);

        //+1 to allow Ball fall off the platform
        _platformRadius = this.transform.localScale.x / 2 + 1;
        _rand = new System.Random();
        _estimatedVelocity = new Vector3(0, 0, 0);
        _stepCountDecisionRequester = 0;

        _numberOfBins = Math.Sqrt(NumberOfBins) % 1 == 0 ? ((int)Math.Sqrt(NumberOfBins)) : ((int)Math.Sqrt(NumberOfBins)) + 1;
        _numberOfBins = _numberOfBins * _numberOfBins;
        _numberOFBinsPerDirection = (int)Math.Sqrt(_numberOfBins);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += ResetBallLocationProbabilities;
        Supervisor.SupervisorAgent.OnTaskSwitchTo += ResetUpdateTime;
        Supervisor.SupervisorAgent.OnTaskSwitchFrom += SetSourceBallAgent;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= ResetBallLocationProbabilities;
        Supervisor.SupervisorAgent.OnTaskSwitchTo -= ResetUpdateTime;
        Supervisor.SupervisorAgent.OnTaskSwitchFrom -= SetSourceBallAgent;
    }

    virtual protected void InitializeBallLocationProbabilities(int NumberOFBins, float platformRadius)
    {
        _ballLocationProbabilities = new double[NumberOFBins];
        _numberOFBinsPerDirection = (int)Math.Sqrt(NumberOFBins);
        _averageVelocity = new Vector3(0, 0, 0);

        for (int i = 0; i < NumberOFBins; i++)
        {
            if (PositionConverter.CoordinatesToBin(BallStartingPosition, platformRadius, _numberOFBinsPerDirection) == i)
            {
                _ballLocationProbabilities[i] = 1;
            }
            else
            {
                _ballLocationProbabilities[i] = 0;
            }
        }
    }


    private void FixedUpdate()
    {
        base.AddForceToBall();
        base.UpdateTimeSinceLastSwitch();
        base.PropagateTaskCompletion();

        if (IsAutonomous)
        {
            RequestAutonomousDecision();
        }
        else
        {
            RequestSupervisedDecision();
        }

        UpdateBeliefState();

        _stepCountDecisionRequester++;

        _visibilityTimer += Time.fixedDeltaTime;
        _oldDistributionPersistenceTimer += Time.fixedDeltaTime;
    }

    private void ResetUpdateTime(ITask task)
    {
        if (this.Equals(task))
        {
            _updateTimer = 0;
        }
    }

    private void SetSourceBallAgent(ITask task)
    {
        if (task is BallAgent)
        {
            _sourceBallAgent = (BallAgent)task;
        }
    }

    private void ResetBallLocationProbabilities(object sender, bool aborted)
    {
        InitializeBallLocationProbabilities(_numberOfBins, _platformRadius);
    }

    private void RequestAutonomousDecision()
    {
        if (_stepCountDecisionRequester >= DecisionPeriod)
        {
            _stepCountDecisionRequester = 0;
            RequestDecision();
        }
        else
        {
            RequestAction();
        }
    }

    private void RequestSupervisedDecision()
    {
        if (IsActive)
        {
            if (_stepCountDecisionRequester >= DecisionPeriod)
            {
                _stepCountDecisionRequester = 0;
                RequestDecision();
            }
            else
            {
                RequestAction();
            }
        }
        else
        {
            _oldDistributionPersistenceTimer = 0;
            _visibilityTimer = 0;
        }
    }

    private void UpdateBeliefState()
    {
        _updateTimer += Time.fixedDeltaTime;

        if (UpdatePeriod < _updateTimer)
        {
            _updateTimer = 0;

            UpdateBallLocationProbabilities();

            if (ShowBeliefState)
            {
                _beliefBallPosition.transform.localPosition = GetBallBeliefPosition();
            }
        }
    }

    //Updates the s_ballLocationProbabilities based on the following formula: b`(s_) = O(s,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)}
    private void UpdateBallLocationProbabilities()
    {
        //_estimatedVelocity is only updated based on the true velocity if the current instance is active
        if (IsVisible)
        {
            //_estimatedVelocity = _ballRb.velocity * (UpdatePeriod + UpdatePeriod * UpdatePeriod);
            _estimatedVelocity = _ballRb.velocity * UpdatePeriod;
            _currentSigmaMean = SigmaMean;
        }
        else
        {
            //Vector3 currentVelocity = _ballRb.velocity * (UpdatePeriod + UpdatePeriod * UpdatePeriod);
            Vector3 currentVelocity = _ballRb.velocity * UpdatePeriod;

            //adding inaccuracy every step the focus is not on the platform
            NormalDistribution normalDistributionMeanX = new NormalDistribution(_estimatedVelocity.x, _currentSigmaMean);
            NormalDistribution normalDistributionMeanZ = new NormalDistribution(_estimatedVelocity.z, _currentSigmaMean);

            _currentSigmaMean = _currentSigmaMean/2;

            //taking into account the decreasing speed
            double distanceRatio = _estimatedVelocity.magnitude == 0 ? 0 : currentVelocity.magnitude / _estimatedVelocity.magnitude;

            float meanX = (float)normalDistributionMeanX.Sample(_rand);
            float meanZ = (float)normalDistributionMeanZ.Sample(_rand);

            _estimatedVelocity = new Vector3(meanX, currentVelocity.y, meanZ) * (float)distanceRatio;
        }

        s_calculateNormalDistributionForVelocityPerfMarker.Begin();
        Vector3[] normal = GetNormalDistributionForVelocity(NumberOfSamples, _estimatedVelocity);
        s_calculateNormalDistributionForVelocityPerfMarker.End();

        double[] currentBallLocationProbabilities = (double[])_ballLocationProbabilities.Clone();

        NativeArray<Vector3> normalNative = new NativeArray<Vector3>(normal, Allocator.TempJob);
        NativeArray<double> currentBallLocationProbabilitiesNative = new NativeArray<double>(currentBallLocationProbabilities, Allocator.TempJob);
        NativeArray<double> ballLocationProbabilitiesNative = new NativeArray<double>(_numberOfBins, Allocator.TempJob);

        Assert.AreEqual(_numberOFBinsPerDirection*_numberOFBinsPerDirection, _numberOfBins);

        BallLocationProbabilitiesUpdateJob ballLocationProbabilitiesUpdateJob = new BallLocationProbabilitiesUpdateJob
        {
            NormalDistributionForVelocity = normalNative,
            CurrentBallLocationProbabilities = currentBallLocationProbabilitiesNative,
            BallLocationProbabilities = ballLocationProbabilitiesNative,
            PlatformRadius = _platformRadius,
            NumberOFBinsPerDirection = _numberOFBinsPerDirection,
            NumberOFBins = _numberOfBins,
            BallPosition = Ball.transform.localPosition,
            LocalScaleZ = this.transform.localScale.z,
            LocalScaleX = this.transform.localScale.x,
            IsVisibleInstance = IsVisible,
            ObservationProbability = ObservationProbability
        };

        s_updateBallLocationProbabilitiesPerfMarker.Begin();
        JobHandle jobHandle = ballLocationProbabilitiesUpdateJob.Schedule(_numberOfBins, 16);
        jobHandle.Complete();

        //direct assignment of the array would overwrite the reference to the original array (e.g. reference of subclass)
        Array.Copy(ballLocationProbabilitiesNative.ToArray(), _ballLocationProbabilities, _numberOfBins);
        s_updateBallLocationProbabilitiesPerfMarker.End();

        _averageVelocity = GetAverageVelocity(normal);

        if (ShowBeliefState)
        {
            _averageLine.SetPosition(0, _beliefBallPosition.transform.position);
            //_averageLine.SetPosition(1, _beliefBallPosition.transform.position + _averageVelocity * 10);
            _averageLine.SetPosition(1, _beliefBallPosition.transform.position + _estimatedVelocity * 10);

            Dictionary<int, double> t = GetTransitionProbabilities(_beliefBallPosition.transform.localPosition, normal);
            var keyOfMaxValue = t.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            _maxProbBinLine.SetPosition(0, _beliefBallPosition.transform.position);
            Vector3 maxProbBinDirection = new Vector3(
                PositionConverter.BinToCoordinates(keyOfMaxValue, _platformRadius, _numberOFBinsPerDirection, Ball.transform.position.y).x * 2,
                _beliefBallPosition.transform.position.y,
                PositionConverter.BinToCoordinates(keyOfMaxValue, _platformRadius, _numberOFBinsPerDirection, Ball.transform.position.y).z * 2);
            _maxProbBinLine.SetPosition(1, maxProbBinDirection + gameObject.transform.position);
        }

        //Normalise the updated belief b`(s_)
        s_normalizationPerfMarker.Begin();
        double sum = _ballLocationProbabilities.Sum();
        for (int p = 0; p < _numberOfBins; p++)
        {
            _ballLocationProbabilities[p] = _ballLocationProbabilities[p] / sum;
        }
        s_normalizationPerfMarker.End();

        normalNative.Dispose();
        currentBallLocationProbabilitiesNative.Dispose();
        ballLocationProbabilitiesNative.Dispose();
    }

    private Vector3[] GetNormalDistributionForVelocity(int numberOfSamples, Vector3 velocity)
    {
        Vector3[] normal = new Vector3[numberOfSamples];

        NormalDistribution normalDistributionX = new NormalDistribution(velocity.x, Sigma);
        NormalDistribution normalDistributionZ = new NormalDistribution(velocity.z, Sigma);

        for (int i = 0; i < numberOfSamples; i++)
        {
            normal[i] = new Vector3((float)normalDistributionX.Sample(_rand), velocity.y, (float)normalDistributionZ.Sample(_rand)); //TODO: change velocity.y to the actual position of y in respect of the sample of x and z
        }

        return normal;
    }

    private Vector3 GetAverageVelocity(Vector3[] normalDistributionForVelocity)
    {
        float x, y, z;
        x = y = z = 0;

        foreach (Vector3 vector in normalDistributionForVelocity)
        {
            x = x + vector.x;
            y = y + vector.y;
            z = z + vector.z;
        }

        int numberOfSamples = normalDistributionForVelocity.Length;

        return new Vector3(x / numberOfSamples, y / numberOfSamples, z / numberOfSamples);
    }

    //Calculates the transition probabilities for the given position based on the NormalDistributionForVelocity. Attention: if the UpdatePeriode, the
    //NumberOfBins and the actual velocity of the Ball are too small then there are no transitions because the velocity per UpdatePeriode is too
    //small to point to another bin. Therefore, in this case the bin stays the same. 
    private Dictionary<int, double> GetTransitionProbabilities(Vector3 position, Vector3[] normalDistributionForVelocity)
    {
        Dictionary<int, int> numberOfTransitionsPerBin = new Dictionary<int, int>();
        Dictionary<int, double> transitionProbabilities = new Dictionary<int, double>();

        foreach (Vector3 velocity in normalDistributionForVelocity)
        {
            Vector3 target = position + velocity;
            int targetBin = PositionConverter.CoordinatesToBin(target, _platformRadius, _numberOFBinsPerDirection);

            if (numberOfTransitionsPerBin.ContainsKey(targetBin)){
                numberOfTransitionsPerBin[targetBin] = numberOfTransitionsPerBin[targetBin] + 1;
            }
            else
            {
                numberOfTransitionsPerBin[targetBin] = 1;
            }       
        }

        foreach (KeyValuePair<int, int> entry in numberOfTransitionsPerBin)
        {
            transitionProbabilities[entry.Key] = entry.Value / (double)normalDistributionForVelocity.Length;
        }

        return transitionProbabilities;
    }

    private Vector3 CalculateRateOfChange(Vector3 oldPosition, Vector3 newPosition)  
    {
        return new Vector3(newPosition.x - oldPosition.x, newPosition.y - oldPosition.y, newPosition.z - oldPosition.z);
    }
}
