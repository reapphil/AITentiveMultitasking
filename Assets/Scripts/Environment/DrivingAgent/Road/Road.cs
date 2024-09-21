using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using Random = UnityEngine.Random;

public class Road : MonoBehaviour {
    
    [field: SerializeField, Header("Overhead Sign")]
    public OverheadSign OverheadSign { get; set; }

    [field: SerializeField, Header("Speed Sign")] 
    public SpeedSign SpeedSign { get; set; }


    [field: SerializeField, Header("Road Settings")]
    public GameObject RoadTilesParent { get; set; }

    [field: SerializeField]
    public GameObject RoadTilePrefab { get; set; }

    [field: SerializeField]
    public int RoadTilesAmount { get; set; } = 50; //has to be at least the min road length 1f * (gameManager.maxSpeed / 3.6f) + 4.6f * (gameManager.maxSpeed / 3.6f)

    public static int RoadLength { get; set; }

    public List<Line> LinesToDraw = new List<Line>();

    public bool LoggedOptimalLaneChangeEnd = true;


    private Lane _activeLane = Lane.Center;
    
    //Line Positions
    private Vector3 _roadStartLinePosition;

    private Vector3 _overheadSignLinePosition;
    
    private Vector3 _afterOverheadSignLinePosition;
    
    private Vector3 _roadEndLinePosition;

    private RoadManager _roadManager;
    
    private GameManager _gameManager;


    public void InitRoadLength(GameManager gameManagerRef) 
    {
        RoadLength = RoadTilesAmount * 10;
        
        //gets min needed road length
        float minRoadLength = 1f * (gameManagerRef.MaxSpeed / 3.6f) + 4.6f * (gameManagerRef.MaxSpeed / 3.6f) + 20f;

        if (RoadLength < minRoadLength) {
            //calculates and sets the min needed road length if the set one is to short
            RoadTilesAmount = (int) Mathf.Ceil(minRoadLength  / 10);
            RoadLength = RoadTilesAmount * 10;
        }
    }

    public void InitRoads(bool isFirstSpawn,bool overheadSign) {
        Reset();
        PlaceRoadTiles();
        PlaceSpeedSign();
        PlaceRoadSign(isFirstSpawn,overheadSign);
    }

    public void PlaceSpeedSign() {
        //Get Random Position for Speed Sign
        int newZPosition = (int) Random.Range( 10f, RoadLength - (RoadLength * 0.1f));

        SpeedSign.gameObject.transform.localPosition = new Vector3(0, 0, newZPosition);
        
        //Get Random Speed Value
        int steps = ((_gameManager.MaxSpeed - _gameManager.MinSpeed) / _gameManager.Step) + 1;
        int randomIndex = Random.Range(0, steps);
        int randomNumber = _gameManager.MinSpeed + (randomIndex * _gameManager.Step);
        
        SpeedSign.SetSpeedText(randomNumber);        
    }

    public Lane GetActiveLine() {
        return _activeLane;
    }

    // Function to calculate the distance from point P to line segment AB
    public float DistancePointToLineSegment(Vector3 P) {
        Vector3 A = Vector3.zero;
        Vector3 B = Vector3.zero;

        P.y = 1f;

        if (P.z > _roadEndLinePosition.z) {
            Debug.LogWarning("Failed to calculate correct distance to center of lane");
            return -1f;
        } 
        
        if (P.z >= _afterOverheadSignLinePosition.z) {
            A = _afterOverheadSignLinePosition;
            B = _roadEndLinePosition;
        } else if (P.z >= _overheadSignLinePosition.z) {
            A = _overheadSignLinePosition;
            B = _afterOverheadSignLinePosition;
        } else if (P.z >= _roadStartLinePosition.z) {
            A = _roadStartLinePosition;
            B = _overheadSignLinePosition;
        }

        // Direction vector from A to B
        Vector3 AB = B - A;
        // Direction vector from A to P
        Vector3 AP = P - A;
        // Calculate the magnitude of AB vector (the length of the line segment)
        float magnitudeAB = AB.magnitude;
        // The normalized "direction" vector from A to B
        Vector3 directionAB = AB.normalized;
        // Project point P onto the line defined by A and B
        float dotProduct = Vector3.Dot(AP, directionAB);
        // Ensure the projected point lies within the segment
        dotProduct = Mathf.Clamp(dotProduct, 0f, magnitudeAB);
        // Find the projected point along AB
        Vector3 projectedPoint = A + directionAB * dotProduct;
        // Return the distance from P to the projected point
        
        LinesToDraw.Add(new Line(P,projectedPoint,Color.red));

        return Vector3.Distance(projectedPoint, P);
    }
    
    public Vector3 ClosestPointOnLineSegment(Vector3 P) {
        Vector3 A = Vector3.zero;
        Vector3 B = Vector3.zero;

        P.y = 1f;

        if (P.z > _roadEndLinePosition.z) {
            Debug.LogWarning("Failed to calculate correct distance to center of lane");
            return Vector3.zero;
        } 
        
        if (P.z >= _afterOverheadSignLinePosition.z) {
            A = _afterOverheadSignLinePosition;
            B = _roadEndLinePosition;
        } else if (P.z >= _overheadSignLinePosition.z) {
            A = _overheadSignLinePosition;
            B = _afterOverheadSignLinePosition;
        } else if (P.z >= _roadStartLinePosition.z) {
            A = _roadStartLinePosition;
            B = _overheadSignLinePosition;
        }

        // Direction vector from A to B
        Vector3 AB = B - A;
        // Direction vector from A to P
        Vector3 AP = P - A;
        // Calculate the magnitude of AB vector (the length of the line segment)
        float magnitudeAB = AB.magnitude;
        // The normalized "direction" vector from A to B
        Vector3 directionAB = AB.normalized;
        // Project point P onto the line defined by A and B
        float dotProduct = Vector3.Dot(AP, directionAB);
        // Ensure the projected point lies within the segment
        dotProduct = Mathf.Clamp(dotProduct, 0f, magnitudeAB);
        // Find the projected point along AB
        Vector3 projectedPoint = A + directionAB * dotProduct;
        // Return the distance from P to the projected point


        return projectedPoint;
    }
    
    public void PlaceRoadSign(bool isFirstSpawn, bool signActiveOnRoad) {
        
        OverheadSign.gameObject.SetActive(signActiveOnRoad);
        
        Lane previousLane = _roadManager.GetPreviousLane(gameObject);

        if (signActiveOnRoad) {
            
            //Max Possible Length for Lane Switch
            float laneSwitchLengthMaxSpeed = 1f * (_gameManager.MaxSpeed / 3.6f) + 4.6f * (_gameManager.MaxSpeed / 3.6f);
        
            //Find Random Position in the Available Space
            int newZPosition = (int) Random.Range(isFirstSpawn ? 40f : 10f, RoadLength - laneSwitchLengthMaxSpeed);

            OverheadSign.gameObject.transform.localPosition = new Vector3(0, 5, newZPosition);
            _activeLane = OverheadSign.RandomSigns(previousLane);
        } else {
            _activeLane = previousLane;
        }
      
        DrawDrivingLanes(previousLane);
    }

    public bool IsInFrontOfOverheadSign(Vector3 position) {
        return OverheadSign.gameObject.transform.position.z > position.z;
    }

    public float GetDistanceToNextSpeedSign(Vector3 position)
    {
        return SpeedSign.gameObject.transform.position.z - position.z;
    }

    public void ResetLines() {
        LinesToDraw.Clear();
    }

    public void Reset() {
        LoggedOptimalLaneChangeEnd = true;
    }


    private void Awake()
    {
        _gameManager = GameManager.Instance;
        _roadManager = RoadManager.Instance;
    }

    private void Update()
    {
        foreach (Line line in LinesToDraw)
        {
            Debug.DrawLine(line.startPosition, line.endPosition, line.color, 1f);
        }

        if (!LoggedOptimalLaneChangeEnd && _gameManager.CarController.transform.position.z >= _afterOverheadSignLinePosition.z)
        {
            LoggedOptimalLaneChangeEnd = true;
            _gameManager.PerfLogWriter.WriteInfoLine(InfoLineType.OptimalLaneChangeEnd, (float)_activeLane);
        }
    }

    private void PlaceRoadTiles()
    {
        for (int i = 0; i < RoadTilesAmount; i++)
        {
            GameObject childRoadTile = Instantiate(RoadTilePrefab, RoadTilesParent.transform.position,
                Quaternion.identity, RoadTilesParent.transform);
            childRoadTile.transform.localPosition = new Vector3(0, 0, i * 10);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerCar"))
        {
            _roadManager.MoveLastRoadToFront();
            _gameManager.SetCurrentRoadSegment(this);
        }
    }

    private void DrawDrivingLanes(Lane previousLane)
    {
        Vector3 roadStartPosition = gameObject.transform.position;
        Vector3 signPosition = OverheadSign.gameObject.transform.position;
        roadStartPosition.z -= 5;
        roadStartPosition.y = 1f;
        signPosition.y = 1f;

        roadStartPosition.x = _gameManager.GetXLocationForLane(previousLane);
        signPosition.x = _gameManager.GetXLocationForLane(previousLane);

        Vector3 startPosAfterSign = OverheadSign.gameObject.transform.position;
        Vector3 roadEndPosition = gameObject.transform.position;
        startPosAfterSign.y = 1f;
        roadEndPosition.z += RoadLength - 5;
        roadEndPosition.y = 1f;

        startPosAfterSign.x = _gameManager.GetXLocationForLane(_activeLane);
        roadEndPosition.x = _gameManager.GetXLocationForLane(_activeLane);

        int speedAtSign = (signPosition.z > SpeedSign.gameObject.transform.position.z
            ? SpeedSign.SignSpeed
            : _roadManager.GetPreviousSpeed(gameObject));
        signPosition.z += 1f * (speedAtSign / 3.6f);
        startPosAfterSign.z += 4.6f * (speedAtSign / 3.6f);

        //draw line up to sign
        LinesToDraw.Add(new Line(roadStartPosition, signPosition, Color.green));
        _roadStartLinePosition = roadStartPosition;
        _overheadSignLinePosition = signPosition;

        //draw line after sign to end of road segment
        LinesToDraw.Add(new Line(startPosAfterSign, roadEndPosition, Color.green));

        _afterOverheadSignLinePosition = startPosAfterSign;
        _roadEndLinePosition = roadEndPosition;

        //draw line change line
        LinesToDraw.Add(new Line(signPosition, startPosAfterSign, Color.green));
    }
}