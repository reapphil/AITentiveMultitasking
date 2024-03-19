using Unity.MLAgents.Sensors;


public interface ICrTask : ITask
{
    /// <summary>
    /// Adds observations that are perceived by the agent (e.g. the belief state) to the sensor of the focus agent.
    /// </summary>
    /// <param name="sensor"> Sensor of the supervisor </param>
    void AddPerceivedObservationsToSensor(VectorSensor sensor);

    /// <summary>
    /// Used by the focus agent to indicate that the task is currently focused. If the agent considers the task to be focused, it should update its
    /// belief state accordingly.
    /// </summary>
    bool IsFocused { get; set; }

    /// <summary>
    /// If true, the task does consider the focus agent to update its visibility state and effect therefore how the belief state is updated.
    /// </summary>
    bool UseFocusAgent { get; set; }
}
