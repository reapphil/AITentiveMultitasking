using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.IO;
using System;
using System.Linq;

public class ConfigTests
{
    private string _workingDirectory;
    private string _configTestPath;

    [SetUp]
    public void SetUp()
    {
        _workingDirectory = Application.dataPath;
        _configTestPath = Path.Combine("..", _workingDirectory, "Tests", "ConfigTests");
    }

    [TearDown]
    public void TeardDown()
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
}
