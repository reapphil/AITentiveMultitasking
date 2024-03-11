using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;


public interface ITask
{
    //void CreateTask();

    bool IsActive { get; set; }

    bool IsFocused { get; set; }

    bool IsAutonomous { get; set; }

    int DecisionPeriod { get; set; }

    static event Action TaskCompleted;
    static void InvokeTaskCompletion() => TaskCompleted?.Invoke();

    //Adds true state information to the sensor of the supervisor agent.
    void AddObservationsToSensor(VectorSensor sensor);

    void UpdateDifficultyLevel();

    GameObject GetGameObject();

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

