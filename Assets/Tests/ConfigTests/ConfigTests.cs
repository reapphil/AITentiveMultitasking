using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.IO;
using System;
using System.Linq;
using static UnityEditor.ShaderData;
using NSubstitute;
using Supervisor;
using NSubstitute.Core.Arguments;

public class ConfigTests
{
    private string _workingDirectory;
    private string _configTestPath;
    private IProjectSettings _projectSettings;
    private PerformanceMeasurement _performanceMeasurement;
    private BehaviorMeasurementBehavior _behaviorMeasurement;
    private Supervisor.SupervisorAgent _supervisorAgent;
    private MeasurementSettings _measurementSettings;

    [SetUp]
    public void SetUp()
    {
        _workingDirectory = Application.dataPath;
        _configTestPath = Path.Combine("..", _workingDirectory, "Tests", "ConfigTests");
        _projectSettings = Substitute.For<IProjectSettings>();
        _supervisorAgent = Substitute.For<Supervisor.SupervisorAgent>();
        _performanceMeasurement = Substitute.For<PerformanceMeasurement>();
        _behaviorMeasurement = Substitute.For<BehaviorMeasurementBehavior>();
        _measurementSettings = Substitute.For<MeasurementSettings>();
        _projectSettings.GetManagedComponentFor(typeof(SupervisorAgentV1)).Returns(_supervisorAgent);
        _projectSettings.GetManagedComponentFor<PerformanceMeasurement>().Returns(_performanceMeasurement);
        _projectSettings.GetManagedComponentFor<BehaviorMeasurementBehavior>().Returns(_behaviorMeasurement);
        _projectSettings.GetManagedComponentsFor(default).ReturnsForAnyArgs(new List<Component>());
        _projectSettings.MeasurementSettings.Returns(_measurementSettings);
    }

    [TearDown]
    public void TearDown()
    {

    }

    [Test]
    public void LoadTaskModelsTest()
    {
        string path = Path.Combine(_configTestPath, "modelsTestConfig.json");

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(path);

        Assert.IsTrue(settings.ContainsKey(typeof(Hyperparameters)));

        Hyperparameters hyperparameters = settings[typeof(Hyperparameters)] as Hyperparameters;

        Assert.AreEqual(2, hyperparameters.taskModels.Count);
        Assert.AreEqual("path\\to\\BallAgentModel", hyperparameters.taskModels["BallAgent"]);
        Assert.AreEqual("path\\to\\ChessAgentModel", hyperparameters.taskModels["ChessAgent"]);
    }

    [Test]
    public void ExperimentSettingsLoadingTest()
    {
        string path = Path.Combine(_configTestPath, "experiment_notification.json");

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(path);

        Validator.ValidateExperimentSettings(settings);

        SceneManagement.ProjectSettings = _projectSettings;
        SceneManagement.ConfigScene(settings);

        _projectSettings.Received().Mode = Supervisor.Mode.Notification;
        _projectSettings.Received().SupervisorChoice = SupervisorChoice.SupervisorAgentV1;
        _projectSettings.Received().GameMode = true;

        path = Path.Combine(_configTestPath, "experiment_NoSupervisor.json");
        settings = SettingsLoader.LoadSettings(path);
        SceneManagement.ConfigScene(settings);

        _projectSettings.Received().Mode = Supervisor.Mode.Force;
        _projectSettings.Received().SupervisorChoice = SupervisorChoice.NoSupport;
        _projectSettings.Received().GameMode = true;
    }
}
