using System;
using Unity.MLAgents.Sensors;
using UnityEngine;


public interface ITask
{
    /// <summary>
    /// Used by the supervisor to indicate that the task is active. The task should only be controllable if this value is true.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// The supervisor will ignore this task if this value is true. This value indicates that the task runs autonomously and will not be affected by
    /// the `IsActive` value controlled by the supervisor. This variable could be used for training purpose i.e. when the general task agent should
    /// be trained without the involvement of the supervisor
    /// </summary>.
    bool IsAutonomous { get; set; }

    /// <summary>
    /// The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request a decision every 5 Academy
    /// steps. In contrast to other values related to CR, this value cannot be changed during runtime and stays fixed per training. Therefore, if 
    /// a agent was trained with a specific DecisionPeriod, the value cannot be changed afterwards.
    /// </summary>
    int DecisionPeriod { get; set; }

    /// <summary>
    /// Must be invoked when the task is completed. This will trigger the end of the episode of the supervisor agent.
    /// </summary>
    static event Action TaskCompleted;
    static void InvokeTaskCompletion() => TaskCompleted?.Invoke();

    /// <summary>
    /// Adds true state information to the sensor of the supervisor agent. Therefore, the sum over all states of the tasks will be the state space 
    ///  of the supervisor agent.
    /// </summary>
    /// <param name="sensor"> Sensor of the supervisor </param>
    void AddObservationsToSensor(VectorSensor sensor);

    /// <summary>
    /// The supervisor agent will call this method to update the difficulty level of the task. How the difficulty level is updated is task specific.
    /// </summary>
    void UpdateDifficultyLevel();

    /// <summary>
    /// Returns the Agent GameObject of the task.
    /// </summary>
    /// <returns> Agent GameObject </returns>
    GameObject GetGameObject();

    /// <summary>
    /// Returns the agent script implementing the ITask interface for the given GameObjects.
    /// </summary>
    /// <param name="TaskGameObjects"> Task prefabs</param>
    /// <returns> The agent script implementing the ITask interface</returns>
    static ITask[] GetTasksFromGameObjects(GameObject[] TaskGameObjects)
    {
        ITask[] tasks = new ITask[TaskGameObjects.Length];

        for (int i = 0; i < TaskGameObjects.Length; i++)
        {
            if(TaskGameObjects[i] != null)
            {
                tasks[i] = TaskGameObjects[i].transform.GetChildByName("Agent").GetComponent<ITask>();
            }
        }

        return tasks;
    }
}

