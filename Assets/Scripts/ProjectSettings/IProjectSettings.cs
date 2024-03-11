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
    SupervisorAgent SupervisorAgent { get; set; }
    GameObject[] TasksGameObjects { get; set; }
    ITask[] Tasks { get; }
    List<AITentiveModel> AITentiveModels { get; set; }
    SupervisorChoice SupervisorChoice { get; set; }

    bool AtLeastOneTaskUsesFocusAgent();
    void GenerateFilename();
    void UpdateSettings();
    T GetManagedComponentFor<T>();
    Component GetManagedComponentFor(Type t);
}