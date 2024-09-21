using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Utils;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {
    
    [field: SerializeField, ProjectAssign, Header("Scenario")]
    public ScenarioSettings ActiveScenarioSettings { get; set; }

    [field: SerializeField]
    public List<ScenarioSettings> Scenarios { get; set; }

    [field: SerializeField, Header("Player")]
    public GameObject Player { get; set; }

    public float CarSpeed { get; set; }

    [field: SerializeField, Header("Goal")]
    public float ScenarioDistance { get; set; } = 5000;

    [field: SerializeField]
    public GameObject FinishPrefab { get; set; }

    [field: SerializeField]
    public GameObject WrongDirectionPrefab { get; set; }

    [field: SerializeField, Header("Speed Sign Settings"), Tooltip("Min Speed on Sign")]
    public int MinSpeed { get; set; } = 100;
    
    [field: SerializeField, Tooltip("Max Speed on Sign")]
    public int MaxSpeed { get; set; } = 150;
    
    [field: SerializeField, Tooltip("Step size on sign")]
    public int Step { get; set; } = 10;

    [field: SerializeField, Header("Overhead Sign Settings")]
    public int SignEveryXRoad { get; set; } = 1;

    [field: SerializeField, Header("Self driving Cars")]
    public bool SpawnCars { get; set; } = true;

    [field: SerializeField]
    public float TimeBetweenSpawns { get; set; } = 5f;

    [field: SerializeField]
    public float MaxCars { get; set; } = 10;
    
    [field: SerializeField, Tooltip("Speed in correlation to the current target speed of the player")]
    public float FasterCarsSpeed { get; set; } = 10f;
    
    [field: SerializeField, Tooltip("Speed in correlation to the current target speed of the player")]
    public float SlowerCarsSpeed { get; set; } = -10f;

    [field: SerializeField, Header("Performance Log Settings")]
    public bool WritePerformanceLog { get; set; } = true;

    [field: SerializeField]
    public PerformanceLogWriter PerfLogWriter { get; set; } = new();
    
    [field: SerializeField, Header("Logitech G29 Steering Wheel Settings")]
    public float SteeringSensitivity { get; set; } = 0.5f;

    [field: SerializeField]
    public int SaturationPercentage { get; set; } = 35;

    [field: SerializeField]
    public int Coefficient { get; set; } = 95;

    [field: SerializeField]
    public int SpringGain { get; set; } = 20;

    [field: SerializeField]
    public int DefaultSpringGain { get; set; } = 20;

    [field: SerializeField]
    public int OffsetPercentage { get; set; } = 0;

    [field: SerializeField]
    public GameObject[] SelfDrivingCars { get; set; }

    public float CurrentTargetSpeed { get; set; } = 100;

    public float CurrentObservedTargetSpeed { get; set; } = 100;

    public static GameManager Instance { get; private set; }

    public bool LaneChangeInProgress { get; set; } = false;
    
    public Lane TargetLaneOfLaneChange { get; set; } = Lane.Center;
    
    public RCC_CarControllerV3 CarController { get; set; }

    public delegate void OnFinishReached(object sender, bool aborted);
    public static event OnFinishReached OnFinishReachedAction;


    private RCC_InputManager _rcc_InputManager;

    private List<SelfDrivingCar> _fasterCars = new List<SelfDrivingCar>();

    private List<SelfDrivingCar> _slowerCars = new List<SelfDrivingCar>();

    private LogitechGSDK.LogiControllerPropertiesData _logitechProperties;
    
    private GameObject _finish;

    private GameObject _wrongDirection;

    private Coroutine _trafficSpawnCoroutine;
    
    private Road _previousRoadSegment;
    
    private Road _currentRoadSegment;

    private RoadManager _roadManager;

    //private TerrainManager _terrainManager;

    private Transform _spawnContainer;


    public void StartSimulation() 
    {
        PerfLogWriter.Init("PerformanceLog_" + this.GetHashCode() + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + (ActiveScenarioSettings ? "_" + ActiveScenarioSettings.name : "") + ".txt", this);
        CarController = Player.GetComponent<RCC_CarControllerV3>();

        if (_trafficSpawnCoroutine != null) {
            StopCoroutine(_trafficSpawnCoroutine);
        }
        _trafficSpawnCoroutine = StartCoroutine(SpawnSelfDrivers());

        _roadManager.InitializeRoad(true);
        
        PlaceStartFinish();
    }
    
    public void RestartSimulation() 
    {
        //_terrainManager.Reset();
        
        PerfLogWriter.Init("PerformanceLog_" + this.GetHashCode() + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + (ActiveScenarioSettings ? "_" + ActiveScenarioSettings.name : "") + ".txt", this);

        if (_trafficSpawnCoroutine != null) {
            StopCoroutine(_trafficSpawnCoroutine);

            foreach (SelfDrivingCar car in _fasterCars) {
                if(car != null)
                {
                    Destroy(car.gameObject);
                }
            }

            foreach (SelfDrivingCar car in _slowerCars) {
                if (car != null)
                {
                    Destroy(car.gameObject);
                }
            }
            
        }
        _trafficSpawnCoroutine = StartCoroutine(SpawnSelfDrivers());

        _roadManager.InitializeRoad(true);
        
        PlaceStartFinish();
    }

    public Vector3 GetClosestPointOnLineSegment(Vector3 positionOfPlayerOrAgent) 
    {
        if (positionOfPlayerOrAgent.z >= (_currentRoadSegment.transform.position.z - 5)) {
            return _currentRoadSegment.ClosestPointOnLineSegment(positionOfPlayerOrAgent);
        }

        if (_previousRoadSegment) {
            return _previousRoadSegment.ClosestPointOnLineSegment(positionOfPlayerOrAgent);
        }

        Debug.LogWarning("Failed to get Closest Point on Line Segment");
        return Vector3.zero;
    }

    public void SetCurrentRoadSegment(Road road) 
    {
        _previousRoadSegment = _currentRoadSegment;
        _currentRoadSegment = road;
    }

    public void PlaceAgentOnCurrentLane() {
        var lane = _currentRoadSegment.IsInFrontOfOverheadSign(Player.transform.position)
            ? _previousRoadSegment.GetActiveLine()
            : _currentRoadSegment.GetActiveLine();

        RCC.Transport(CarController, new Vector3(GetXLocationForLane(lane), 0.75f, 10), Quaternion.identity);
    }

    public Vector3 GetCurrentLaneLocation(GameObject agent) 
    {
        Vector3 playerPos = agent.transform.position;
        var lane = _currentRoadSegment.IsInFrontOfOverheadSign(playerPos)
            ? _previousRoadSegment.GetActiveLine()
            : _currentRoadSegment.GetActiveLine();

        playerPos.x = GetXLocationForLane(lane);

        return playerPos;
    }

    public void RemoveSelfDrivingCar(SelfDrivingCar car) 
    {
        bool isRemoved = false;
        if (_slowerCars.Contains(car)) {
            isRemoved = _slowerCars.Remove(car);
        } else if (_fasterCars.Contains(car)) {
            isRemoved = _fasterCars.Remove(car);
        } else {
            Debug.LogWarning("Failed to remove self driving car");
        }

        if (!isRemoved) {
            Debug.LogWarning("Failed to remove self driving car");
        }
    }

    public float GetXLocationForLane(Lane lane) 
    {
        return lane switch {
            Lane.Left => _spawnContainer.position.x - 4.25f,
            Lane.Center => _spawnContainer.position.x,
            Lane.Right => _spawnContainer.position.x + 4.25f,
            _ => _spawnContainer.position.x
        };
    }

    public void UpdateCurrentTargetSpeed(int signSpeed) 
    {
        CurrentTargetSpeed = signSpeed;

        PerfLogWriter.WriteInfoLine(InfoLineType.TargetSpeedChange, signSpeed);

        foreach (SelfDrivingCar fasterCar in _fasterCars) {
            fasterCar.maximumSpeed = CurrentTargetSpeed + FasterCarsSpeed;
        }

        foreach (SelfDrivingCar slowerCar in _slowerCars) {
            slowerCar.maximumSpeed = CurrentTargetSpeed + SlowerCarsSpeed;
        }
    }

    public void UpdateCurrentObservedTargetSpeed(GameObject agent)
    {
        if (GetDistanceToNextSpeedSign(agent) < 50)
        {
            CurrentObservedTargetSpeed = _roadManager.GetNextSpeed(_currentRoadSegment.gameObject, agent);
        }
    }

    public void LoadScenarioSettings() 
    {
        ScenarioDistance = ActiveScenarioSettings.scenarioDistance;
        MinSpeed = ActiveScenarioSettings.minSpeed;
        MaxSpeed = ActiveScenarioSettings.maxSpeed;
        Step = ActiveScenarioSettings.step;
        SignEveryXRoad = ActiveScenarioSettings.signEveryXRoad;
        SpawnCars = ActiveScenarioSettings.spawnCars;
        TimeBetweenSpawns = ActiveScenarioSettings.timeBetweenSpawns;
        MaxCars = ActiveScenarioSettings.maxCars;
        FasterCarsSpeed = ActiveScenarioSettings.fasterCarsSpeed;
        SlowerCarsSpeed = ActiveScenarioSettings.slowerCarsSpeed;
        WritePerformanceLog = ActiveScenarioSettings.writePerformanceLog;
    }


    private void Awake()
    {
        _spawnContainer = gameObject.GetSpawnContainer().transform;

        // Check if instance already exists and if it's not this, then destroy this.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (Scenarios.Count > 0)
        {
            if (!ActiveScenarioSettings)
            {
                ActiveScenarioSettings = Scenarios[0];
            }
            LoadScenarioSettings();
        }
    }

    private void FixedUpdate()
    {
        if (CarController)
        {
            CarSpeed = CarController.speed;
            float distanceToCurrentLane = -1;

            if (CarController.transform.position.z >= (_currentRoadSegment.transform.position.z - 5))
            {
                distanceToCurrentLane = _currentRoadSegment.DistancePointToLineSegment(CarController.transform.position);
            }
            else
            {
                if (_previousRoadSegment)
                {
                    distanceToCurrentLane =
                        _previousRoadSegment.DistancePointToLineSegment(CarController.transform.position);
                }
            }

            PerfLogWriter.WriteValueLine(CarController.transform.position.z, distanceToCurrentLane, CarSpeed,
                CarController.steerAngle * CarController.steerInput);

            if (ScenarioDistance < CarController.transform.position.z)
            {
                RestartSimulation();
                OnFinishReachedAction?.Invoke(this, false);
            }
        }
    }

    private void Update()
    {
        if (CarController)
        {
            if (LaneChangeInProgress && Mathf.Abs(CarController.transform.position.x - GetXLocationForLane(TargetLaneOfLaneChange)) <= 1)
            {
                LaneChangeInProgress = false;
                PerfLogWriter.WriteInfoLine(InfoLineType.LaneChangeEnd, (float)TargetLaneOfLaneChange);
            }
        }
    }

    private void Start()
    {
        _roadManager = RoadManager.Instance;
        //_terrainManager = TerrainManager.Instance;
        _rcc_InputManager = RCC_InputManager.Instance;

        //rcc_InputManager.logitechSteeringSensitivity = steeringSensitivity;

        //TODO: fix LogitechGSDK dll import issue:
        //-----------------------
        //LogitechGSDK.LogiSteeringInitialize(false);

        //InitializeLogitechSteeringWheel();
        //-----------------------

        StartSimulation();
    }

    private void PlaceStartFinish()
    {
        if (_finish != null)
        {
            _finish.transform.position = new Vector3(_spawnContainer.position.x, _spawnContainer.position.y, _spawnContainer.position.z + ScenarioDistance);
            _wrongDirection.transform.position = new Vector3(_spawnContainer.position.x, _spawnContainer.position.y, _spawnContainer.position.z - 5);
        }
        else
        {
            _finish = Instantiate(FinishPrefab, new Vector3(_spawnContainer.position.x, _spawnContainer.position.y, _spawnContainer.position.z + ScenarioDistance), Quaternion.identity, _spawnContainer);
            _wrongDirection = Instantiate(WrongDirectionPrefab, new Vector3(_spawnContainer.position.x, _spawnContainer.position.y, _spawnContainer.position.z - 5), Quaternion.identity, _spawnContainer);
        }
    }

    private float GetDistanceToNextSpeedSign(GameObject agent)
    {
        return _roadManager.GetDistanceToNextSpeedSign(_currentRoadSegment.gameObject, agent);
    }

    private IEnumerator SpawnSelfDrivers()
    {
        bool skippWait = false;
        while (SpawnCars)
        {
            if (!skippWait)
                yield return new WaitForSeconds(TimeBetweenSpawns);

            if (MaxCars > _fasterCars.Count + _slowerCars.Count)
            {
                int decider = Random.Range(0, 2);
                Lane deciderLane = EnumHelper.GetRandomEnumValue<Lane>();

                Vector3 spawnLocation = GetSpawnLocation(decider, deciderLane);

                // Size of the box to check for colliders
                Vector3 checkBoxSize = new Vector3(2, 1, 2);

                Collider[] hitColliders = Physics.OverlapBox(spawnLocation, checkBoxSize * 0.5f);
                if (hitColliders.Length > 0)
                {
                    skippWait = true;
                    continue;
                }

                float checkDistance = 50f; // Distance to check behind the spawn location
                // Check for cars driving towards the spawn location using a raycast
                RaycastHit hit;
                if (Physics.Raycast(spawnLocation, -Vector3.forward, out hit, checkDistance))
                {
                    if (hit.collider.CompareTag("SelfDriving") || hit.collider.CompareTag("PlayerCar") || hit.collider.CompareTag("AgentCar"))
                    { // Assuming cars have a tag "Car"
                        // Debug.Log("Couldn't Spawn Car - Car detected within 50 units behind the spawn point");
                        skippWait = true;
                        continue;
                    }
                }

                SpawnSelfDriver(decider, deciderLane, spawnLocation);

                skippWait = false;
            }
        }
    }

    private Vector3 GetSpawnLocation(int decider, Lane deciderLane)
    {
        float zLocation = decider switch
        {
            0 => Player.transform.position.z - 50f,
            1 => Player.transform.position.z + 150f,
            _ => 0
        };

        return new Vector3(GetXLocationForLane(deciderLane), 0.75f, zLocation);
    }

    private void SpawnSelfDriver(int decider, Lane deciderLane, Vector3 spawnLocation)
    {
        GameObject car = Instantiate(SelfDrivingCars[Random.Range(0, SelfDrivingCars.Length)],
                    spawnLocation, Quaternion.identity, _spawnContainer);

        // Debug.Log("Spawn: " + car.name);

        SelfDrivingCar selfDrivingCar = car.GetComponent<SelfDrivingCar>();
        selfDrivingCar.targetLane = deciderLane;

        switch (decider)
        {
            case 0:
                _fasterCars.Add(selfDrivingCar);
                break;
            case 1:
                _slowerCars.Add(selfDrivingCar);
                break;
        }

        foreach (Collider collider in car.GetComponentsInHierarchy<Collider>())
        {
            Physics.IgnoreCollision(collider, _wrongDirection.GetComponentInChildren<Collider>());
        }
    }

    private void InitializeLogitechSteeringWheel() 
    {
        Debug.Log("Logitech Steering Wheel -> " + LogitechGSDK.LogiUpdate() + " | " + LogitechGSDK.LogiIsConnected(0));

        if (!LogitechGSDK.LogiIsConnected(0))
            return;

        LogitechGSDK.LogiStopSpringForce(0);
        LogitechGSDK.LogiStopConstantForce(0);
        LogitechGSDK.LogiStopDamperForce(0);

        _logitechProperties.wheelRange = 90;
        _logitechProperties.forceEnable = true;
        _logitechProperties.overallGain = 80;
        _logitechProperties.springGain = SpringGain;
        _logitechProperties.damperGain = 80;
        _logitechProperties.allowGameSettings = false;
        _logitechProperties.combinePedals = false;
        _logitechProperties.defaultSpringEnabled = true;
        _logitechProperties.defaultSpringGain = DefaultSpringGain;
        LogitechGSDK.LogiSetPreferredControllerProperties(_logitechProperties);

        Debug.Log("Logitech Steering Wheel Initialized");
        LogitechGSDK.LogiPlaySpringForce(0, OffsetPercentage, SaturationPercentage, Coefficient);
        // LogitechGSDK.LogiPlayConstantForce(0, 50);
        // LogitechGSDK.LogiPlayDamperForce(0, 100);
    }
}