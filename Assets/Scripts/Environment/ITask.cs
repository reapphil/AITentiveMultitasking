using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;


public interface ITask
{
    /// <summary>
    /// Used by the supervisor to indicate that the task is active. The task should only be controllable if this value is true.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Used by the supervisor to determine when its episode should end. The supervisor ends its episode if the task is terminal. Otherwise only the
    /// cumulated reward is reset. In case the task is terminating, InvokeTermination must be called, otherwise EndEpisode() s.t. only the episode
    /// of the task will be ended.
    /// episode 
    /// </summary>
    bool IsTerminatingTask { get; set; }

    /// <summary>
    /// The supervisor will ignore this task if this value is true. This value indicates that the task runs autonomously and will not be affected by
    /// the `IsActive` value controlled by the supervisor. This variable could be used for training purpose i.e. when the general task agent should
    /// be trained without the involvement of the supervisor
    /// </summary>.
    bool IsAutonomous { get; set; }

    /// <summary>
    /// Specifies the data that is used to analyze the switching behavior of the user/agent. The ISwitchingData should be extended with state 
    /// information of the task to see e.g. in which condition the user/agent has left the task before switching to another task.
    /// </summary>
    IStateInformation StateInformation { get; set; }

    /// <summary>
    /// The frequency with which the agent requests a decision. A DecisionPeriod of 5 means that the Agent will request a decision every 5 Academy
    /// steps. In contrast to other values related to CR, this value cannot be changed during runtime and stays fixed per training. Therefore, if 
    /// a agent was trained with a specific DecisionPeriod, the value cannot be changed afterwards.
    /// </summary>
    int DecisionPeriod { get; set; }

    /// <summary>
    /// Returns the reward of the task that should be used to calculate the reward of the supervisor agent.
    /// </summary>
    /// <returns> Reward </returns>
    Queue<float> TaskRewardForSupervisorAgent { get; }

    /// <summary>
    /// Returns the reward of the task that should be used to calculate the reward of the focus agent.
    /// </summary>
    /// <returns> Reward </returns>
    Queue<float> TaskRewardForFocusAgent { get; }

    /// <summary>
    /// Must be invoked when the task is completed and IsTerminatingTask. This triggers the end of the episode of the supervisor agent and therefore 
    /// of all other tasks. If the task is not a terminal task, the accumulated reward of the task is reseted but the episode of the supervisor agent
    /// continues.
    /// </summary>
    delegate void OnTaskCompletedAction();
    static event OnTaskCompletedAction OnTermination;
    static void InvokeTermination() => OnTermination?.Invoke();

    /// <summary>
    /// Must be invoked on action received. This information is used to analyze the switching behavior of the user/agent.
    /// </summary>
    delegate void OnActionReceivedAction(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch = -1);
    static event OnActionReceivedAction OnAction;
    static void InvokeOnAction(ActionBuffers actionBuffers, ITask task, double timeSinceLastSwitch = -1) => OnAction?.Invoke(actionBuffers, task, timeSinceLastSwitch);

    /// <summary>
    /// Adds true state information to the sensor of the supervisor agent. Therefore, the sum over all states of the tasks will be the state space 
    ///  of the supervisor agent.
    /// </summary>
    /// <param name="sensor"> Sensor of the supervisor or any other agent working with the true space of the tasks</param>
    void AddTrueObservationsToSensor(VectorSensor sensor);

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
            if (TaskGameObjects[i] != null)
            {
                tasks[i] = TaskGameObjects[i].transform.GetChildByName("Agent").GetComponent<ITask>();
            }
        }

        return tasks;
    }

    /// <summary>
    /// Dictionary of the task specific performance values of the current episode. This function is called by the PerformanceMeasurement class at the
    /// end of an episode and appends the values of the Dictionary as columns to the performance file for the current episode. The task is 
    /// responsible for continuously aggregating the values of all task instances of the same type for the current episode. The key value is used as 
    /// the column name in the performance file.
    /// </summary>.
    Dictionary<string, double> Performance { get; }

    /// <summary>
    /// Resets the performance values of the task for the current episode. This function is called by the PerformanceMeasurement class at the end of
    /// an episode.
    /// </summary>
    void ResetPerformance();

    /// <summary>
    /// Provides the current input value like configured in the ProjectSetting component and should be implemented if the tasks need to be
    /// controllable on instance level.
    /// </summary>
    /// <param name="value"></param>
    public void OnMove(InputValue value);
}

