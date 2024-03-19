using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using Supervisor;
using System.Linq;

public class InspectorTests
{
    private string _sceneBackupPath;
    private ProjectSettings _projectSettings;
    private PerformanceMeasurement _performanceMeasurement;
    private BalancingTaskBehaviourMeasurementBehaviour _taskBehaviourMeasurement;
    private SupervisorAgent _supervisorAgent;

    [SetUp]
    public void SetUp()
    {
        _sceneBackupPath = SceneManagement.BackUpScene();
        Scene scene = SceneManagement.GetScene();

        _projectSettings = scene.GetRootGameObjectByName("ProjectSettings").GetComponent<ProjectSettings>();
        _performanceMeasurement = _projectSettings.GetManagedComponentFor<PerformanceMeasurement>();
        _taskBehaviourMeasurement = _projectSettings.GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>();
        _supervisorAgent = (SupervisorAgent)(object)_projectSettings.GetManagedComponentFor(Util.GetType("Supervisor." + _projectSettings.SupervisorChoice.ToString()));
    }

    [TearDown]
    public void TeardDown()
    {
        SceneManagement.RestoreScene(_sceneBackupPath);
    }

    [Test]
    public void GetProjectAssignFieldsForSupervisorTest()
    {
        List<(Component, FieldInfo)> projectAssignFields = _projectSettings.GetProjectAssignFieldsForSupervisor();

        (Component, FieldInfo) playerName = projectAssignFields.Find(x => x.Item2.Name.Contains("PlayerName"));
        playerName.Item2.SetValue(playerName.Item1, "Test");
        (Component, FieldInfo) numberOfTimeBins = projectAssignFields.Find(x => x.Item2.Name.Contains("NumberOfTimeBins"));
        numberOfTimeBins.Item2.SetValue(numberOfTimeBins.Item1, 123);
        (Component, FieldInfo) startCountdownAt = projectAssignFields.Find(x => x.Item2.Name.Contains("StartCountdownAt"));
        startCountdownAt.Item2.SetValue(startCountdownAt.Item1, 123);

        Assert.AreEqual("Test", _performanceMeasurement.PlayerName);
        Assert.AreEqual(123, _taskBehaviourMeasurement.NumberOfTimeBins);
        Assert.AreEqual(123, _supervisorAgent.StartCountdownAt);
    }

    [Test]
    public void GetProjectAssignFieldsForTasksTest()
    {
        List<(Component, FieldInfo)> projectAssignFields = _projectSettings.GetProjectAssignFieldsForTasks();

        (Component, FieldInfo) decisionPeriod = projectAssignFields.Find(x => x.Item2.Name.Contains("DecisionPeriod"));
        decisionPeriod.Item2.SetValue(decisionPeriod.Item1, 123);
        _projectSettings.UpdateSettings();

        foreach (GameObject task in _supervisorAgent.TaskGameObjects)
        {
            ITask taskScript = task.GetComponentInChildren<ITask>();

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            FieldInfo field = taskScript.GetType().GetBackingFieldInHierarchy("DecisionPeriod", flags);

            if (field != null)
            {
                if (Attribute.IsDefined(field, typeof(ProjectAssignAttribute)))
                {
                    Assert.AreEqual(123, taskScript.DecisionPeriod);
                }
            }

        }
    }
}
