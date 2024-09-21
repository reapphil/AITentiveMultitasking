using Unity.MLAgents.Sensors;


public interface ICrTask : ITask
{
    /// <summary>
    /// Adds observations that are perceived by the agent (e.g. the belief state) to the sensor of the focus agent. This should reflect the agent's
    /// uncertainty about the environment and the task.
    /// </summary>
    /// <param name="sensor"> Sensor of the focus agent or any other agent working with the belief state of the tasks </param>
    void AddBeliefObservationsToSensor(VectorSensor sensor);

    /// <summary>
    /// Specifies the elements of a task that the focus agent can concentrate on. These elements should be defined within the editor. If there are no
    /// visual elements and the task's general focus needs to be determined, add the agent game object as a single entry to the FocusStateSpace. The
    /// task's belief state should then be updated accordingly.
    /// </summary>
    VisualStateSpace FocusStateSpace { get; set; }

    /// <summary>
    /// If true, the task does consider the focus agent to update its visibility state and effect therefore how the belief state is updated.
    /// </summary>
    bool UseFocusAgent { get; set; }
}
