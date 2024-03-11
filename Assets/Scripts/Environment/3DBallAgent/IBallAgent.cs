using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public interface IBallAgent
{
    bool IsAutonomous { get; set; }

    float GlobalDrag { get; }

    bool UseNegativeDragDifficulty { get; }

    int BallAgentDifficulty { get; }

    double BallAgentDifficultyDivisionFactor { get; }

    float BallStartingRadius { get; }

    float ResetSpeed { get; }

    bool ResetPlatformToIdentity { get; set; }

    int DecisionPeriod { get; set; }

    void CollectObservations(VectorSensor sensor);

    int CompareTo(BallAgent other);

    Vector3 GetBallGlobalPosition();

    Vector3 GetBallLocalPosition();

    Vector3 GetBallVelocity();

    Vector3 GetPlatformGlobalPosition();

    Vector3 GetPlatformAngle();

    Vector3 GetObservedBallPosition();

    Vector3 GetObservedBallVelocity();

    float GetBallDrag();

    float GetScale();

    void Heuristic(in ActionBuffers actionsOut);

    void Initialize();

    void OnActionReceived(ActionBuffers actionBuffers);

    void OnEpisodeBegin();
}