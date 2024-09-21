using NSubstitute.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class DrivingAgentHumanCognition : DrivingAgent, ICrTask
{
    [field: SerializeField, Tooltip("Update period of the location probabilities of the car over the x.axis."), ProjectAssign(Header = "Human Cognition Model")]
    public float UpdatePeriod { get; set; } = 0.1f;

    [field: SerializeField, Tooltip("Defines how much samples should be taken to calculate the probability distributions."), ProjectAssign]
    public int NumberOfSamples { get; set; } = 100;

    [field: SerializeField, Tooltip("Sigma value used for the calculation of the normal distribution of the location probabilities."), ProjectAssign]
    public double Sigma { get; set; } = 0.1;

    [field: SerializeField, Tooltip("Sigma value used for the calculation of the mean value which is used to calculate the normal distribution of the location probabilities."), ProjectAssign]
    public double SigmaMean { get; set; } = 0.01;

    [field: SerializeField, Tooltip("Describes O(s,a,o) of the formula b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)}."), ProjectAssign]
    public double ObservationProbability { get; set; } = 0.9;

    [field: SerializeField, Tooltip("Specify the number of bins in which the lanes should be divided."), ProjectAssign]
    public int NumberOfBins { get; set; } = 1000;

    [field: SerializeField, Tooltip("If active the observations of the driving agents are provided by the focus- instead of the supervisor agent."), ProjectAssign]
    public bool UseFocusAgent { get; set; }

    [field: SerializeField, Tooltip("Observation is active for this agent independent of the focus/supervisor agent."), ProjectAssign]
    public bool FullVision { get; set; } = false;

    [field: SerializeField, Tooltip("Visualizes the current belief position of the car (gray car) and the observed speed limit."), ProjectAssign]
    public bool ShowBeliefState { get; set; }

    [field: SerializeField]
    public Text SpeedText { get; set; }


    private const float RANGEMIN = -6.5f;

    private const float RANGEMAX = 6.5f;

    private float _estimatedVelocity;

    private double _currentSigmaMean = 0;

    protected double[] _carLocationProbabilities;

    private float _updateTimer = 0.0f;

    private System.Random _rand;

    private GameObject _beliefCarPosition;

    [field: SerializeField]
    public VisualStateSpace FocusStateSpace { get; set; }


    public bool IsVisible
    {
        get
        {
            if (FullVision)
            {
                return true;
            }
            else
            {
                if (UseFocusAgent)
                {
                    return FocusStateSpace.Encoding[0] == 1;
                }
                else
                {
                    return IsActive;
                }
            }
        }
    }

    //returns bin for the position with the highest probability
    public float GetCarBeliefPositionOnXAxis()
    {
        if (_carLocationProbabilities == null)
        {
            return 0;
        }

        double maxValue = _carLocationProbabilities.Max();
        int maxIndex = _carLocationProbabilities.ToList().IndexOf(maxValue);

        return PositionConverter.BinToContinuousValue(maxIndex, RANGEMIN, RANGEMAX, NumberOfBins);
    }

    public void AddBeliefObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation((float)_carLocationProbabilities.Max());
    }

    public new void AddTrueObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(new Vector3(GetCarBeliefPositionOnXAxis(), transform.position.y, transform.position.z));
        sensor.AddObservation(TargetPosition);
        sensor.AddObservation(CarSpeed);
        sensor.AddObservation(GameManager.CurrentObservedTargetSpeed);
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        InitializeCarLocationProbabilities(NumberOfBins, RANGEMIN, RANGEMAX);

        if (ShowBeliefState)
        {
            _beliefCarPosition.transform.localPosition = new Vector3(GetCarBeliefPositionOnXAxis(), transform.localPosition.y, transform.localPosition.z);

            ConfigurableJoint hoodCameraJoint = transform.GetChildByName("HoodCamera").gameObject.GetComponent<ConfigurableJoint>();
            hoodCameraJoint.autoConfigureConnectedAnchor = false;
            hoodCameraJoint.connectedAnchor = new Vector3(0, 2, -7);
        }
    }


    protected void Awake()
    {
        _rand = new System.Random();
        _estimatedVelocity = 0;

        if (ShowBeliefState)
        {
            _beliefCarPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _beliefCarPosition.transform.SetParent(transform.parent);
            _beliefCarPosition.GetComponent<SphereCollider>().enabled = false;
            SpeedText.transform.parent.parent.gameObject.SetActive(true);
        }
    }


    private void UpdateCurrentObservedTargetSpeed()
    {
        if (IsVisible)
        {
            GameManager.UpdateCurrentObservedTargetSpeed(this.gameObject);
        }   
    }

    private void InitializeCarLocationProbabilities(int numberOFBins, float rangeMin, float rangeMax)
    {
        _carLocationProbabilities = new double[numberOFBins];

        for (int i = 0; i < numberOFBins; i++)
        {
            if (PositionConverter.ContinuousValueToBin(transform.localPosition.x, rangeMin, rangeMax, numberOFBins) == i)
            {
                _carLocationProbabilities[i] = 1;
            }
            else
            {
                _carLocationProbabilities[i] = 0;
            }
        }
    }

    private new void FixedUpdate()
    {
        base.FixedUpdate();

        UpdateBeliefState();
        UpdateCurrentObservedTargetSpeed();
    }

    private void UpdateBeliefState()
    {
        _updateTimer += Time.fixedDeltaTime;

        if (UpdatePeriod < _updateTimer)
        {
            _updateTimer = 0;

            UpdateCarLocationProbabilities();
        }

        if (ShowBeliefState)
        {
            _beliefCarPosition.transform.localPosition = new Vector3(GetCarBeliefPositionOnXAxis(), transform.localPosition.y, transform.localPosition.z);
            SpeedText.text = GameManager.CurrentObservedTargetSpeed.ToString();
        }
    }

    //Updates the s_carLocationProbabilities based on the following formula: b`(s_) = O(s,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)}
    private void UpdateCarLocationProbabilities()
    {
        //_estimatedVelocity is only updated based on the true velocity if the current instance is active
        if (IsVisible)
        {
            _estimatedVelocity = GetComponent<Rigidbody>().velocity.x * UpdatePeriod;
            _currentSigmaMean = SigmaMean;
        }
        else
        {
            //Usage of current velocity instead of last observed velocity since the agent performs a conscious steering action and therefore observes
            //at least to a certain extent the introduced velocity of the car related to the performed action.
            float currentVelocity = GetComponent<Rigidbody>().velocity.x * UpdatePeriod;

            //adding inaccuracy every step the focus is not on the platform
            NormalDistribution normalDistributionMeanX = new NormalDistribution(currentVelocity, _currentSigmaMean);

            float meanX = (float)normalDistributionMeanX.Sample(_rand);

            _estimatedVelocity = meanX;
        }

        float[] normal = CRUtil.GetNormalDistributionForVelocity(NumberOfSamples, _estimatedVelocity, Sigma, _rand);

        double[] currentBallLocationProbabilities = (double[])_carLocationProbabilities.Clone();

        NativeArray<float> normalNative = new NativeArray<float>(normal, Allocator.TempJob);
        NativeArray<double> currentCarLocationProbabilitiesNative = new NativeArray<double>(currentBallLocationProbabilities, Allocator.TempJob);
        NativeArray<double> carLocationProbabilitiesNative = new NativeArray<double>(NumberOfBins, Allocator.TempJob);

        ObjectIn1DLocationProbabilitiesUpdateJob carLocationProbabilitiesUpdateJob = new ObjectIn1DLocationProbabilitiesUpdateJob
        {
            NormalDistributionForVelocity = normalNative,
            CurrentObjectLocationProbabilities = currentCarLocationProbabilitiesNative,
            ObjectLocationProbabilities = carLocationProbabilitiesNative,
            RangeMin = RANGEMIN,
            RangeMax = RANGEMAX,
            NumberOFBins = NumberOfBins,
            ObjectPosition = transform.localPosition.x,
            IsVisibleInstance = IsVisible,
            ObservationProbability = ObservationProbability
        };

        JobHandle jobHandle = carLocationProbabilitiesUpdateJob.Schedule(NumberOfBins, 16);
        jobHandle.Complete();

        //direct assignment of the array would overwrite the reference to the original array (e.g. reference of subclass)
        Array.Copy(carLocationProbabilitiesNative.ToArray(), _carLocationProbabilities, NumberOfBins);

        //Normalize the updated belief b`(s_)
        double sum = _carLocationProbabilities.Sum();
        for (int p = 0; p < NumberOfBins; p++)
        {
            _carLocationProbabilities[p] = _carLocationProbabilities[p] / sum;

            if (double.IsNaN(_carLocationProbabilities[p]))
            {
                Debug.LogError("Car location probabilities contain NaN values. Usually this can happen when the a velocity value of the normal distribution is to big resulting in an index error in the edge bin correction logic. Try to reduce the sigma value.");
            }
        }

        normalNative.Dispose();
        currentCarLocationProbabilitiesNative.Dispose();
        carLocationProbabilitiesNative.Dispose();
    }
}
