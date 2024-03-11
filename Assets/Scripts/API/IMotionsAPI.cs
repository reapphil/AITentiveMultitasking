using Supervisor;
using System;
using System.ArrayExtensions;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class IMotionsAPI : MonoBehaviour
{
    private BallAgent[] _ballAgents;
    private SupervisorAgent _supervisorAgent;

    private void OnEnable()
    {
        SupervisorAgent.OnTaskSwitchCompleted += SendTaskSwitchEvent;
        SupervisorAgent.OnStartEpisode += SendEpisodeStartEvent;
        SupervisorAgent.EndEpisodeEvent += SendEpisodeEndEvent;

        _supervisorAgent = SupervisorAgent.GetSupervisor(gameObject);

        _ballAgents = _supervisorAgent.GetBallAgents();
    }

    private void OnDisable()
    {
        SupervisorAgent.OnTaskSwitchCompleted -= SendTaskSwitchEvent;
        SupervisorAgent.OnStartEpisode -= SendEpisodeStartEvent;
        SupervisorAgent.EndEpisodeEvent -= SendEpisodeEndEvent;
    }

    private void FixedUpdate()
    {
        SendDifficultyEvent();
    }

    private void SendDifficultyEvent()
    {
        float difficulty;
        int sourceDefinitionVersion = 1;

        for (int i = 0; i < _ballAgents.Length; i++)
        {
            difficulty = Distances.SecondsUntilGameOver(_ballAgents[i].GetScale()/2, _ballAgents[i].GetSlopeInDegrees(), _ballAgents[i].GetBallLocalPosition(), _ballAgents[i].GetBallVelocity(), _ballAgents[i].GetBallDrag());
            string message = BuildSensorEvent("Balancing", sourceDefinitionVersion, "ForceDifficulty", difficulty, i.ToString());
            NetworkAPI.SendUDPPacket("127.0.0.1", 8089, message, 1);
            //Debug.Log(string.Format("{0}", message));
        }
    }

    private void SendTaskSwitchEvent(double timeBetweenSwitches, ISupervisorAgent supervisorAgent, bool isNewEpisode)
    {
        if (!isNewEpisode)
        {
            string message = BuildDiscreteMarker("TaskSwitch", "TaskSwitch", "D");
            NetworkAPI.SendUDPPacket("127.0.0.1", 8089, message, 1);
        }
    }

    private void SendEpisodeStartEvent()
    {
        string message = BuildDiscreteMarker("Episode", "Episode has started.", "S");
        NetworkAPI.SendUDPPacket("127.0.0.1", 8089, message, 1);
    }

    private void SendEpisodeEndEvent(object sender, bool aborted)
    {
        string message = BuildDiscreteMarker("Episode", "Episode has ended.", "E");
        NetworkAPI.SendUDPPacket("127.0.0.1", 8089, message, 1);
    }

    private string BuildDiscreteMarker(string shortText, string description, string markerType, string sceneType = "")
    {
        char type = 'M';
        int version = 2;
        string message = string.Format("{0};{1};;;{2};{3};{4};{5}\r\n", type, version, shortText, description, markerType, sceneType);

        return message;
    }

    private string BuildSensorEvent(string source, int sourceDefinitionVersion, string sampleType, double value, string instance = "")
    {
        char type = 'E';
        int version = 2;
        string message = string.Format("{0};{1};{2};{3};{4};;;{5};{6}\r\n", type, version, source, sourceDefinitionVersion, instance, sampleType, value.ToString(CultureInfo.InvariantCulture));

        return message;
    }
}
