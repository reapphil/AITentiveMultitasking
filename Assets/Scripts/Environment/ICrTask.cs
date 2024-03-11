using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public interface ICrTask : ITask
{
    //Adds observations that are perceived by the agent to the sensor of the focus agent.
    void AddPerceivedObservationsToSensor(VectorSensor sensor);

    bool IsVisible { get; }

    bool UseFocusAgent { get; set; }
}
