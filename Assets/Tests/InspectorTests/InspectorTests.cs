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
    private BehaviorMeasurementBehavior _taskBehaviourMeasurement;

    [SetUp]
    public void SetUp()
    {
        _sceneBackupPath = SceneManagement.BackUpScene();
        Scene scene = SceneManagement.GetScene();

        _projectSettings = scene.GetRootGameObjectByName("ProjectSettings").GetComponent<ProjectSettings>();
        _projectSettings.AITentiveModels = new List<AITentiveModel>();
        _performanceMeasurement = _projectSettings.GetManagedComponentFor<PerformanceMeasurement>();
        _taskBehaviourMeasurement = _projectSettings.GetManagedComponentFor<BehaviorMeasurementBehavior>();
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
        Assert.AreEqual(123, _projectSettings.SupervisorAgent.StartCountdownAt);
    }

    [Test]
    public void GetProjectAssignFieldsForTasksTest()
    {
        List<(Component, FieldInfo)> projectAssignFields = _projectSettings.GetProjectAssignFieldsForTasks();

        projectAssignFields.FindAll(x => x.Item2.Name.Contains("IsAutonomous")).ForEach(x => x.Item2.SetValue(x.Item1, true));
        projectAssignFields.FindAll(x => x.Item2.Name.Contains("IsTerminatingTask")).ForEach(x => x.Item2.SetValue(x.Item1, true));
        _projectSettings.UpdateSettings();

        foreach (GameObject task in _projectSettings.SupervisorAgent.TaskGameObjects)
        {
            ITask taskScript = task.GetComponentInChildren<ITask>();

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            
            FieldInfo field = taskScript.GetType().GetBackingFieldInHierarchy("IsAutonomous", flags);
            Assert.AreEqual(true, taskScript.IsAutonomous);

            field = taskScript.GetType().GetBackingFieldInHierarchy("IsTerminatingTask", flags);
            Assert.AreEqual(true, taskScript.IsTerminatingTask);
        }
    }
}
