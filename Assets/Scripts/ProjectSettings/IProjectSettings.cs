using Supervisor;
using System;
using System.Collections.Generic;
using Unity.Barracuda;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public interface IProjectSettings
{
    Agent[] Agents { get; }
    Mode Mode { get; set; }
    Text ProjectSettingsText { get; set; }
    SupervisorAgent SupervisorAgent { get; }
    GameObject[] TasksGameObjects { get; set; }
    ITask[] Tasks { get; }
    List<AITentiveModel> AITentiveModels { get; set; }
    SupervisorChoice SupervisorChoice { get; set; }
    public bool GameMode { get; set; }
    MeasurementSettings MeasurementSettings { get; }
    bool AtLeastOneTaskUsesFocusAgent();
    void GenerateFilename();
    void UpdateSettings(bool isBuild = false);
    T GetManagedComponentFor<T>();
    public SupervisorAgent GetActiveSupervisor();
    Component GetManagedComponentFor(Type t);
    List<Component> GetManagedComponentsFor(Type t);
}