using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Supervisor
{
    public interface ISupervisorAgent
    {
        int EpisodeCount { get; }

        float FixedEpisodeDuration { get; }

        bool FocusActiveTask { get; set; }

        float AdvanceNoticeInSeconds { get; set; }

        bool SetConstantDecisionRequestInterval { get;}

        float DecisionRequestIntervalInSeconds { get;}

        int DifficultyIncrementInterval { get;}

        int DecisionPeriod { get;}

        bool UseHeuristic { get; }

        void CollectObservations(VectorSensor sensor);

        ITask GetActiveTask();

        void Heuristic(in ActionBuffers actionsOut);

        void Initialize();

        void OnActionReceived(ActionBuffers actionBuffers);

        void OnEpisodeBegin();

        T GetComponent<T>();
    }
}