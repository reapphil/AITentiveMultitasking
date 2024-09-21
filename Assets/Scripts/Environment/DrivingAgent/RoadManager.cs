using UnityEngine;
using Utils;

public class RoadManager : MonoBehaviour 
{
    public static RoadManager Instance { get; private set; }

    [field: SerializeField]
    public GameObject RoadPrefab { get; set; }
    
    public float RoadLength { get; set; } // The length of the road segment


    private Transform _spawnContainer;

    private GameObject[] _roadSegments;

    private GameManager _gameManager;

    private int _overheadSignCounter = 0; //counter to check if overhead sign should be placed

    private int _lastIndex; // Index of the last segment


    public void InitializeRoad(bool placeAgentOrPlayer) 
    {
        RoadPrefab.GetComponent<Road>().InitRoadLength(_gameManager);
        RoadLength = Road.RoadLength;
        
        _overheadSignCounter = -1;
        _lastIndex = 0;
        if (_roadSegments != null) {
            foreach (GameObject roadSegment in _roadSegments) {
                if (roadSegment) {
                    Destroy(roadSegment);
                }
            }
        }

        // Instantiate and position road segments
        _roadSegments = new GameObject[3];

        for (int i = 0; i < _roadSegments.Length; i++) {
            _roadSegments[i] = Instantiate(RoadPrefab, new Vector3(_spawnContainer.position.x, _spawnContainer.position.y, i * RoadLength - RoadLength),
                Quaternion.identity, _spawnContainer);
            _roadSegments[i].GetComponent<Road>().InitRoads(true,_overheadSignCounter <= 0);
            _overheadSignCounter++;
            if (_overheadSignCounter.Equals(_gameManager.SignEveryXRoad))
                _overheadSignCounter = 0;
        }

        _gameManager.SetCurrentRoadSegment(_roadSegments[0].GetComponent<Road>());
        _gameManager.SetCurrentRoadSegment(_roadSegments[1].GetComponent<Road>());

        if (placeAgentOrPlayer) {
            _gameManager.PlaceAgentOnCurrentLane();
        }
    }

    public void MoveLastRoadToFront() 
    {
        _overheadSignCounter++;
        if (_overheadSignCounter.Equals(_gameManager.SignEveryXRoad))
            _overheadSignCounter = 0;

        Road roadToMove = _roadSegments[_lastIndex].GetComponent<Road>();
        roadToMove.Reset();

        // Calculate the new position for the last segment to be moved to the front
        float newZPosition = _roadSegments[(_lastIndex + 1) % _roadSegments.Length].transform.localPosition.z + RoadLength * 2;
        roadToMove.gameObject.transform.localPosition = new Vector3(0, 0, newZPosition);

        roadToMove.PlaceRoadSign(false, _overheadSignCounter == 0);

        roadToMove.PlaceSpeedSign();

        // Update the index of the last segment
        _lastIndex = (_lastIndex + 1) % _roadSegments.Length;
    }

    public Lane GetPreviousLane(GameObject road) 
    {
        Lane previousFreeLaneIndex = Lane.Center;

        for (int currentIndex = 0; currentIndex < _roadSegments.Length; currentIndex++) {
            if (_roadSegments[currentIndex] == road) {
                int previousIndex = (currentIndex - 1 + _roadSegments.Length) % _roadSegments.Length;
                GameObject previousRoad = _roadSegments[previousIndex];
                if (previousRoad) {
                    previousFreeLaneIndex = previousRoad.GetComponent<Road>().GetActiveLine();
                }
            }
        }

        return previousFreeLaneIndex;
    }

    public int GetPreviousSpeed(GameObject road) 
    {
        int previousRoadSpeed = 100;

        for (int currentIndex = 0; currentIndex < _roadSegments.Length; currentIndex++) {
            if (_roadSegments[currentIndex] == road) 
            {    
                int previousIndex = (currentIndex - 1 + _roadSegments.Length) % _roadSegments.Length;
                GameObject previousRoad = _roadSegments[previousIndex];
                
                if (previousRoad) 
                {
                    previousRoadSpeed = previousRoad.GetComponent<Road>().SpeedSign.SignSpeed;
                }
            }
        }

        return previousRoadSpeed;
    }

    public int GetNextSpeed(GameObject road, GameObject agent)
    {
        int roadSpeed = -1;

        for (int i = 0; i < _roadSegments.Length; i++)
        {
            if (_roadSegments[i] == road)
            {
                SpeedSign speedSignCurrentRoadSegment = _roadSegments[i].GetComponent<Road>().SpeedSign;

                if (agent.transform.position.z < speedSignCurrentRoadSegment.transform.position.z)
                {
                    roadSpeed = speedSignCurrentRoadSegment.SignSpeed;
                }
                else
                {
                    int nextIndex = (i + 1 + _roadSegments.Length) % _roadSegments.Length;
                    roadSpeed = _roadSegments[nextIndex].GetComponent<Road>().SpeedSign.SignSpeed;
                }
            }
        }

        return roadSpeed;
    }

    public int GetDistanceToNextSpeedSign(GameObject road, GameObject agent)
    {
        int distance = -1;

        for (int i = 0; i < _roadSegments.Length; i++)
        {
            if (_roadSegments[i] == road)
            {
                SpeedSign speedSignCurrentRoadSegment = _roadSegments[i].GetComponent<Road>().SpeedSign;

                if (agent.transform.position.z < speedSignCurrentRoadSegment.transform.position.z)
                {
                    distance = (int)(speedSignCurrentRoadSegment.transform.position.z - agent.transform.position.z);
                }
                else
                {
                    int nextIndex = (i + 1 + _roadSegments.Length) % _roadSegments.Length;
                    distance = (int)(_roadSegments[nextIndex].GetComponent<Road>().SpeedSign.transform.position.z - agent.transform.position.z);
                }
            }
        }

        return distance;
    }

    public Vector3 GetCurrentEndOfRoad() 
    {
        Vector3 endPos = _roadSegments[2].transform.position;
        endPos.z += RoadLength;
        return endPos;
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
        _gameManager = GameManager.Instance;
    }
}