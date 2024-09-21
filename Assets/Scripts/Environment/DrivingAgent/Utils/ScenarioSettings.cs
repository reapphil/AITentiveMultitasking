using UnityEngine;

[CreateAssetMenu(fileName = "ScenarioSettings", menuName = "HighwayDrivingSimulator/Scenario", order = 1)]
public class ScenarioSettings : ScriptableObject {

    [Header("Goal")]
    public float scenarioDistance = 5000;
    
    [Header("Speed Sign Settings")]
    [Tooltip("Min Speed on Sign")]
    public int minSpeed = 100;
    [Tooltip("Max Speed on Sign")]
    public int maxSpeed = 150;
    [Tooltip("Step size on sign")]
    public int step = 10;
    
    [Header("Overhead Sign Settings")]
    public int signEveryXRoad = 1;
    
    [Header("Self driving Cars")]
    public bool spawnCars = true;
    public float timeBetweenSpawns = 5f;
    public float maxCars = 10;
    [Tooltip("Speed in correlation to the current target speed of the player")]
    public float fasterCarsSpeed = 10f;
    [Tooltip("Speed in correlation to the current target speed of the player")]
    public float slowerCarsSpeed = -10f;

    [Header("Ml Agents")]
    public bool agentActive;
    
    [Header("Performance Log Settings")]
    public bool writePerformanceLog = true;
    
}