using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour 
{
    [field: SerializeField]
    public GameObject TerrainPrefab { get; set; }

    [field: SerializeField]
    public float LeftSideRoadX { get; set; } = -4.75f;

    [field: SerializeField]
    public float RightSideRoadX { get; set; } = -4.75f;
    
    public static TerrainManager Instance { get; private set; }


    private Transform _spawnContainer;

    private Vector2 _terrainDimensions = new Vector2(500, 500);

    private float _nextTerrainZ = -250f;

    private float _nextTriggerZ = 0f;

    private List<GameObject> _activeTerrains = new();

    private GameManager _gameManager;


    public void Reset() 
    {
        foreach (GameObject activeTerrain in _activeTerrains) {
            Destroy(activeTerrain);
        }
        
        _activeTerrains.Clear();
        _nextTerrainZ = -250f;

        StartPlacing();
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
    }

    private void Start()
    {
        _gameManager = GameManager.Instance;

        StartPlacing();

    }

    private void StartPlacing() 
    {
        _nextTriggerZ = _nextTerrainZ + _terrainDimensions.y + _terrainDimensions.y;
        PlaceTerrains();
        PlaceTerrains();
        PlaceTerrains();
        PlaceTerrains();
    }

    private void Update() 
    {
        if (_gameManager.CarController.transform.position.z >= _nextTriggerZ) {
            _nextTriggerZ += _terrainDimensions.y;
            PlaceTerrains();
            RemoveOldestTerrain();
            RemoveOldestTerrain();
        }
    }

    private void PlaceTerrains() 
    {
        GameObject newTerrainRightSide = Instantiate(TerrainPrefab, new Vector3(_spawnContainer.position.x + RightSideRoadX, _spawnContainer.position.y, _spawnContainer.position.z + _nextTerrainZ),Quaternion.identity, _spawnContainer);
        Terrain terrainRight = newTerrainRightSide.GetComponent<Terrain>();
        
        GameObject newTerrainLeftSide = Instantiate(TerrainPrefab, new Vector3(_spawnContainer.position.x + LeftSideRoadX - _terrainDimensions.x, _spawnContainer.position.y, _spawnContainer.position.z + _nextTerrainZ),Quaternion.identity, _spawnContainer);
        Terrain terrainLeft = newTerrainLeftSide.GetComponent<Terrain>();
            
        terrainRight.enabled = true;
        terrainLeft.enabled = true;

        _nextTerrainZ += _terrainDimensions.y;
        
        _activeTerrains.Add(newTerrainRightSide);
        _activeTerrains.Add(newTerrainLeftSide);
    }

    private void RemoveOldestTerrain() 
    {
        if (_activeTerrains.Count > 0) {
            Destroy(_activeTerrains[0].gameObject);
            _activeTerrains.RemoveAt(0);
        }
    }
}