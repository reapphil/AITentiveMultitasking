using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using Supervisor;
using Unity.Barracuda;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;


public class PostProcessingTests
{
    private ICommandLineInterface _commandLineInterfaceMock;
    private IProjectSettings _projectSettings;
    private string _postProcessingPath;
    private string _modelPath;


    [SetUp]
    public void SetUp()
    {
        _commandLineInterfaceMock = Substitute.For<ICommandLineInterface>();
        _projectSettings = Substitute.For<IProjectSettings>();
        _postProcessingPath = Path.Combine("Assets", "Tests", "BuildTests", "postprocessing");
        _modelPath = Path.Combine(_postProcessingPath, "AUI_2024_NOFAST_2P", "AUITestConfAUI_2024_NOFAST_2P_TMP");

        Util.CopyDirectory(Path.Combine(_postProcessingPath, "AUI_2024_NOFAST_2P", "AUITestConfAUI_2024_NOFAST_2P"), _modelPath);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_modelPath, true);
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not delete directory: {0}", e.Message));
        }
    }

    [Test]
    public void PostProcessingTest()
    {
        EnrichModels(false, Path.Combine(_modelPath, "AUI_2024_NOFAST_2P.json"));
        string modelPath = Path.Combine(_modelPath, "AUIAUITestConfAUI_2024_NOFAST_2P_TMP.asset");

        AssetDatabase.ImportAsset(modelPath);
        AITentiveModel model = AssetDatabase.LoadAssetAtPath(modelPath, typeof(AITentiveModel)) as AITentiveModel;

        Assert.AreEqual(1, model.DecisionPeriod);
        Assert.AreEqual("SupervisorAgent", model.Type);
        Assert.IsFalse(model.SupervisorSettings.randomSupervisor);
        Assert.AreEqual(18, model.SupervisorSettings.vectorObservationSize);
        Assert.IsTrue(model.SupervisorSettings.setConstantDecisionRequestInterval);
        Assert.AreEqual(0.8f, model.SupervisorSettings.decisionRequestIntervalInSeconds);
        Assert.AreEqual(100, model.SupervisorSettings.difficultyIncrementInterval);
        Assert.AreEqual(0.3f, model.SupervisorSettings.advanceNoticeInSeconds);

        Assert.AreEqual("AUIAUITestConfAUI_2024_NOFAST_2P_TMP", model.Model.name);
    }


    private void EnrichModels(bool useMock, string configFilePath)
    {
        _commandLineInterfaceMock.GetCommandLineArgs().Returns(new string[] { "Dummy", "-executeMethod", "PostProcessing.EnrichModels", configFilePath, "-o" });
        APIHelper.CommandLineInterface = _commandLineInterfaceMock;

        DefineProjectSettings(useMock);

        PostProcessing.EnrichModels();
    }

    private void DefineProjectSettings(bool useMock)
    {
        if (useMock)
        {
            SceneManagement.ProjectSettings = _projectSettings;
        }
        else
        {
            SceneManagement.ProjectSettings = null;
        }
    }
}


