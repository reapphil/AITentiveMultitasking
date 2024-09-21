using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using Supervisor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;


public class BuildScriptTests
{
    private ICommandLineInterface _commandLineInterfaceMock;
    private IProjectSettings _projectSettings;
    private ITask _task;
    private string _workingDirectory;
    private string _comparisonFilePath;
    private string _reactionTimeFilePathNT1;
    private string _reactionTimeFilePathNT5;
    private string _scoresFilePath;
    private string _buildPath;


    public void Initialize(string scoreString, string deleteScoresPath = null)
    {
        if (deleteScoresPath != null)
        {
            try 
            {                 
                Directory.Delete(deleteScoresPath, true);
            }
            catch (Exception e)
            {
                Debug.Log(String.Format("Could not delete directory: {0}", e.Message));
            }
        }

        string scoresFile = "scores_test.csv";
        string comparisonFile = "testNA225NAN5NV6.json";
        string reationTimeFileNT5 = "test_rt_NT5ND12NVD12NA5.json";
        string reationTimeFileNT1 = "test_rt_NT1ND12NVD12NA5.json";
        string absoluteModelsDirPath = Path.Combine("..", _workingDirectory, "Tests", "BuildTests", "testsession");
        _comparisonFilePath = Path.Combine("Scores", scoreString, comparisonFile);
        _reactionTimeFilePathNT1 = Path.Combine("Scores", scoreString, reationTimeFileNT1);
        _reactionTimeFilePathNT5 = Path.Combine("Scores", scoreString, reationTimeFileNT5);
        _scoresFilePath = Path.Combine("Scores", scoreString, scoresFile);
        Directory.CreateDirectory(Path.Combine("Scores", scoreString));

        //Copies the comparison files to the local Scores folder s.t. the BuildScript then can copy it from this location to the corresponding Build
        File.Copy(Path.Combine(absoluteModelsDirPath, comparisonFile), _comparisonFilePath, true);
        File.Copy(Path.Combine(absoluteModelsDirPath, reationTimeFileNT5), _reactionTimeFilePathNT5, true);
        File.Copy(Path.Combine(absoluteModelsDirPath, reationTimeFileNT1), _reactionTimeFilePathNT1, true);
        File.Copy(Path.Combine(absoluteModelsDirPath, scoresFile), _scoresFilePath, true);
    }

    [SetUp]
    public void SetUp()
    {
        _commandLineInterfaceMock = Substitute.For<ICommandLineInterface>();
        _projectSettings = Substitute.For<IProjectSettings>();
        _task = Substitute.For<ITask>();
        _workingDirectory = Application.dataPath;
        _buildPath = "TestBuilds";
        BuildScript.IsDevelopmentBuild = true;
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            File.Delete(_comparisonFilePath);
            File.Delete(_reactionTimeFilePathNT1);
            File.Delete(_reactionTimeFilePathNT5);
            File.Delete(_scoresFilePath);

            Directory.Delete(Path.GetDirectoryName(_comparisonFilePath));
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not delete directory: {0}", e.Message));
        }
    }

    [Test]
    public void TrainingEnvironmentExecutionTest()
    {
        BuildTrainingEnvironment(false);

        string text = RunProductionEnvironment("TrainingEnvironment");
        string textLower = text.ToLower();

        if (textLower.Contains("exception") || textLower.Contains("error"))
        {
            Debug.Log("Exception found in LogFile.txt:");
            Debug.Log(text);
        }

        AssertHasException(text);
    }

    [Test]
    public void TrainingEnvironmentExecutionParameterBallAgentTest()
    {
        BuildTrainingEnvironment(false, "testEnvParameter1.json");

        string text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("DecisionPeriod = 6"));
        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognitionSingleProbabilityDistribution"));
        Assert.IsTrue(text.Contains("ShowBeliefState = True"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 40"));
        Assert.IsTrue(text.Contains("Sigma = 0,3") || text.Contains("Sigma = 0.3"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,05") || text.Contains("SigmaMean = 0.05"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,3") || text.Contains("UpdatePeriod = 0.3"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,9") || text.Contains("ObservationProbability = 0.9"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,2") || text.Contains("ConstantReactionTime = 0.2"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,6") || text.Contains("OldDistributionPersistenceTime = 0.6"));
        Assert.IsTrue(text.Contains("FullVision = True"));
        Assert.IsTrue(text.Contains("UseFocusAgent = False"));

        Assert.IsTrue(text.Contains("Model = AUI1"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsTrue(text.Contains("BehaviorType = Default"));


        BuildTrainingEnvironment(false, "testEnvParameter2.json");

        text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("DecisionPeriod = 7"));
        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognition"));
        Assert.IsTrue(text.Contains("NumberOfBins = 5041"));
        Assert.IsTrue(text.Contains("ShowBeliefState = False"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 1000"));
        Assert.IsTrue(text.Contains("Sigma = 0,1") || text.Contains("Sigma = 0.1"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,01") || text.Contains("SigmaMean = 0.01"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,2") || text.Contains("UpdatePeriod = 0.2"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,1") || text.Contains("ObservationProbability = 0.1"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,3") || text.Contains("ConstantReactionTime = 0.3"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,9") || text.Contains("OldDistributionPersistenceTime = 0.9"));
        Assert.IsTrue(text.Contains("FullVision = False"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));

        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsTrue(text.Contains("Model = Focus2"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsTrue(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void TrainingEnvironmentExecutionParameterSupervisorTest()
    {
        BuildTrainingEnvironment(false, "testEnvParameter1.json");

        string text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("GlobalDrag = 1"));
        Assert.IsTrue(text.Contains("UseNegativeDragDifficulty = True"));    
        Assert.IsTrue(text.Contains("BallAgentDifficulty = 60"));
        Assert.IsTrue(text.Contains("BallAgentDifficultyDivisionFactor = 2"));
        Assert.IsTrue(text.Contains("BallStartingRadius = 1,5") || text.Contains("BallStartingRadius = 1.5"));
        Assert.IsTrue(text.Contains("ResetSpeed = 15"));
        Assert.IsTrue(text.Contains("UseFocusAgent = False"));

        Assert.IsTrue(text.Contains("Model = AUI1"));
        Assert.IsTrue(text.Contains("AdvanceNoticeInSeconds = 0"));
        Assert.IsTrue(text.Contains("DifficultyIncrementInterval = 30"));
        Assert.IsTrue(text.Contains("SetConstantDecisionRequestInterval = False"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 0,5") || text.Contains("DecisionRequestIntervalInSeconds = 0.5"));
        Assert.IsFalse(text.Contains("DecisionRequestIntervalRangeInSeconds = 3"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 10"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsTrue(text.Contains("BehaviorType = Default"));


        BuildTrainingEnvironment(false, "testEnvParameter2.json");

        text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("GlobalDrag = -0,1") || text.Contains("GlobalDrag = -0.1"));
        Assert.IsTrue(text.Contains("UseNegativeDragDifficulty = False"));
        Assert.IsTrue(text.Contains("BallAgentDifficulty = 70"));
        Assert.IsTrue(text.Contains("BallAgentDifficultyDivisionFactor = 3"));
        Assert.IsTrue(text.Contains("BallStartingRadius = 1"));
        Assert.IsTrue(text.Contains("ResetSpeed = 10"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsTrue(text.Contains("Model = Focus2"));

        Assert.IsTrue(text.Contains("AdvanceNoticeInSeconds = 0"));
        Assert.IsTrue(text.Contains("DifficultyIncrementInterval = 45"));
        Assert.IsTrue(text.Contains("SetConstantDecisionRequestInterval = True"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 5"));
        Assert.IsFalse(text.Contains("DecisionRequestIntervalRangeInSeconds = 0"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 15"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsTrue(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void TrainingEnvironmentExecutionParameterJsonSettingsTest()
    {
        BuildTrainingEnvironment(false, "testEnvJsonParameter1.json");

        string text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("QuizName = quiztest.csv"));
        Assert.IsTrue(text.Contains("ScreenWidthPixel = 1920"));
        Assert.IsTrue(text.Contains("ScreenHightPixel = 1080"));
        Assert.IsTrue(text.Contains("ScreenDiagonalInch = 27"));
        Assert.IsTrue(text.Contains("ShowBeliefState = True"));
        Assert.IsTrue(text.Contains("FullVision = True"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 98"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 99"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,5") || text.Contains("ObservationProbability = 0.5"));
        
        Assert.IsTrue(text.Contains("Mode = Force"));
        Assert.IsTrue(text.Contains("IsAutonomous = False"));
        Assert.IsTrue(text.Contains("MaxCarSpeed = 150"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 4"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,1") || text.Contains("UpdatePeriod = 0.1"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 100"));
        Assert.IsTrue(text.Contains("Sigma = 0,111") || text.Contains("Sigma = 0.111"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,23") ||text.Contains("SigmaMean = 0.23"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,005") || text.Contains("ObservationProbability = 0.005"));
        Assert.IsTrue(text.Contains("NumberOfBins = 972"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));
        Assert.IsTrue(text.Contains("FullVision = True"));
        Assert.IsTrue(text.Contains("ShowBeliefState = False"));
        Assert.IsTrue(text.Contains("IsTerminatingTask = True"));

        BuildTrainingEnvironment(false, "testEnvJsonParameter2.json");

        text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("QuizName = quiztest.csv"));
        Assert.IsTrue(text.Contains("ScreenWidthPixel = 1920"));
        Assert.IsTrue(text.Contains("ScreenHightPixel = 1080"));
        Assert.IsTrue(text.Contains("ScreenDiagonalInch = 43"));
        Assert.IsTrue(text.Contains("ShowBeliefState = True"));
        Assert.IsTrue(text.Contains("FullVision = True"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 33"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 33"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,22") || text.Contains("ObservationProbability = 0.22"));

        Assert.IsTrue(text.Contains("Mode = Suggestion"));
        Assert.IsTrue(text.Contains("IsAutonomous = True")); //Suggestion Mode
        Assert.IsTrue(text.Contains("MaxCarSpeed = 140"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 5"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,3") || text.Contains("UpdatePeriod = 0.3"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 100"));
        Assert.IsTrue(text.Contains("Sigma = 0,111") || text.Contains("Sigma = 0.111"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,23") || text.Contains("SigmaMean = 0.23"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,0033") || text.Contains("ObservationProbability = 0.0033"));
        Assert.IsTrue(text.Contains("NumberOfBins = 999"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));
        Assert.IsTrue(text.Contains("FullVision = False"));
        Assert.IsTrue(text.Contains("ShowBeliefState = True"));
        Assert.IsTrue(text.Contains("IsTerminatingTask = False"));
    }

    [Test]
    public void TrainingEnvironmentMixedExecutionParameterJsonSettingsTest()
    {
        BuildTrainingEnvironment(false, "testEnvMixedJsonParameter.json");

        string text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognition"));
        Assert.IsTrue(text.Contains("NumberOfBins = 666"));
        Assert.IsTrue(text.Contains("ShowBeliefState = False"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 1100"));
        Assert.IsTrue(text.Contains("Sigma = 0,7") || text.Contains("Sigma = 0.7"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,11") || text.Contains("SigmaMean = 0.11"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,22") || text.Contains("UpdatePeriod = 0.22"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,123") || text.Contains("ObservationProbability = 0.123"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,5") || text.Contains("ConstantReactionTime = 0.5"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,45") || text.Contains("OldDistributionPersistenceTime = 0.45"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));
    }

    [Test]
    public void TrainingEnvironmentSupervisorModeTest()
    {
        BuildTrainingEnvironment(false, "testEnvSupervisorSuggestionMode.json");

        string text = RunProductionEnvironment("TrainingEnvironment");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("Mode = Suggestion"));


        BuildTrainingEnvironment(false, "testEnvSupervisorForceMode.json");

        text = RunProductionEnvironment("TrainingEnvironment");

        Assert.IsTrue(text.Contains("Mode = Force"));
    }

    [Test]
    public void PerformanceMeasurementTest()
    {
        Initialize("CDFDRI0.5DII30DP10b3ahcspd3", Path.Combine(_workingDirectory, "..", "Build", _buildPath, "EvaltestPerformance", "SupervisorML_Data", "Scores"));

        string model = BuildEvaluationEnvironment(false, configFileName: "testPerformance.json", evalConfigFileName: "EvalConfPerformance.json", model: "BuildTest/3DBall2.asset");

        string text = RunEvaluationEnvironment("EvaltestPerformance");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("Performance will be collected..."));
        Assert.IsTrue(text.Contains("IsTerminatingTask = True"));
        Assert.IsTrue(text.Contains("Performance Measurement active: 1/17 episodes completed!"));
        Assert.IsTrue(text.Contains("PlayerName = testPlayer"));
        Assert.IsTrue(text.Contains("MinimumScoreForMeasurement = 1"));
        Assert.IsTrue(text.Contains("MaxNumberEpisodes = 17"));
    }

    [Test]
    public void PerformanceMeasurementMaxNumberEpisodesReachedTest()
    {
        Initialize("CDFDRI0.5DII30DP10b3ahcspd3", Path.Combine(_workingDirectory, "..", "Build", _buildPath, "EvaltestPerformance", "SupervisorML_Data", "Scores"));

        string model = BuildEvaluationEnvironment(false, configFileName: "testPerformance.json", evalConfigFileName: "EvalConfPerformance.json", model: "BuildTest/3DBall2.asset");

        string text = RunEvaluationEnvironment("EvaltestPerformance", 25000);
        text = RunEvaluationEnvironment("EvaltestPerformance");

        Debug.Log(text);

        Assert.IsTrue(text.Contains("IsTerminatingTask = True"));
        Assert.IsTrue(text.Contains("Performance will be collected..."));
        Assert.IsTrue(text.Contains("MaxNumberEpisodes already reached, quit application."));

        AssertHasException(text);
    }

    [Test]
    public void EvaluationEnvironmentExecutionTest()
    {
        Initialize("CDTDRI1R1DII15DP5b3ahcspd2");

        string model = BuildEvaluationEnvironment(false, configFileName: "testEnvEval.json");

        string text = RunEvaluationEnvironment("EvaltestEnvEval");

        string textLower = text.ToLower();

        if (textLower.Contains("exception") || textLower.Contains("error"))
        {
            Debug.Log(text);
        }

        AssertHasException(text);
    }

    [Test]
    public void EvaluationEnvironmentExecutionBehaviorParameterTest()
    {
        Initialize("CDFDRI0.5R3DII30DP10b3ahcspd3");

        BuildEvaluationEnvironment(false, "testEnvParameter1Eval.json", false, model:"BuildTest/3DBall1.asset");

        string text = RunEvaluationEnvironment("EvaltestEnvParameter1Eval");

        //Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("TimeScale = 20"));

        Assert.IsTrue(text.Contains("MaxNumberOfActions = 133625"));
        Assert.IsTrue(text.Contains("NumberOfAreaBinsPerDirection = 15"));
        Assert.IsTrue(text.Contains("NumberOfAngleBinsPerAxis = 5"));
        Assert.IsTrue(text.Contains("NumberOfBallVelocityBinsPerAxis = 6"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_ballPosition = 12"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_angle = 12"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_velocity = 12"));
        Assert.IsTrue(text.Contains("NumberOfActionBinsPerAxis = 5"));
        Assert.IsTrue(text.Contains("NumberOfTimeBins = 5"));

        Assert.IsTrue(text.Contains("SaveBehavioralData = True"));

        Assert.IsTrue(text.Contains("Supervisor.SupervisorAgentRandom"));
        Assert.IsTrue(text.Contains("Model = 3DBall1"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));


        TearDown();
        Initialize("CDTDRI5DII45DP15b3ahc4");

        BuildEvaluationEnvironment(false, "testEnvParameter2Eval.json", false, evalConfigFileName: "EvalConf2.json", model: "BuildTest/3DBall2.asset");

        text = RunEvaluationEnvironment("EvaltestEnvParameter2Eval");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("MaxNumberOfActions = 10000"));
        Assert.IsTrue(text.Contains("NumberOfAreaBinsPerDirection = 14"));
        Assert.IsTrue(text.Contains("NumberOfAngleBinsPerAxis = 4"));
        Assert.IsTrue(text.Contains("NumberOfBallVelocityBinsPerAxis = 6"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_ballPosition = 45"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_angle = 47"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_velocity = 46"));
        Assert.IsTrue(text.Contains("NumberOfActionBinsPerAxis = 9"));
        Assert.IsTrue(text.Contains("NumberOfTimeBins = 1"));

        Assert.IsTrue(text.Contains("Model = AUI2"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsTrue(text.Contains("Model = Focus2"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void EvaluationEnvironmentExecutionParameterBallAgentTest()
    {
        Initialize("CDFDRI0.5R3DII30DP10b3ahcspd3");

        BuildEvaluationEnvironment(false, "testEnvParameter1Eval.json", model: "BuildTest/3DBall1.asset");

        string text = RunEvaluationEnvironment("EvaltestEnvParameter1Eval");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("DecisionPeriod = 6"));
        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognitionSingleProbabilityDistribution"));
        Assert.IsTrue(text.Contains("ShowBeliefState = True"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 40"));
        Assert.IsTrue(text.Contains("Sigma = 0,3") || text.Contains("Sigma = 0.3"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,05") || text.Contains("SigmaMean = 0.05"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,3") || text.Contains("UpdatePeriod = 0.3"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,9") || text.Contains("ObservationProbability = 0.9"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,2") || text.Contains("ConstantReactionTime = 0.2"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,6") || text.Contains("OldDistributionPersistenceTime = 0.6"));

        Assert.IsTrue(text.Contains("Supervisor.SupervisorAgentRandom"));
        Assert.IsTrue(text.Contains("Model = 3DBall1"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));


        TearDown();
        Initialize("CDTDRI5DII45DP15b3ahc4");

        BuildEvaluationEnvironment(false, "testEnvParameter2Eval.json", model: "BuildTest/3DBall2.asset");

        text = RunEvaluationEnvironment("EvaltestEnvParameter2Eval");

        AssertHasException(text);

        Assert.IsTrue(text.Contains("DecisionPeriod = 7"));
        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognition"));
        Assert.IsTrue(text.Contains("NumberOfBins = 5041"));
        Assert.IsTrue(text.Contains("ShowBeliefState = False"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 1000"));
        Assert.IsTrue(text.Contains("Sigma = 0,1") || text.Contains("Sigma = 0.1"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,01") || text.Contains("SigmaMean = 0.01"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,2") || text.Contains("UpdatePeriod = 0.2"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,1") || text.Contains("ObservationProbability = 0.1"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,3") || text.Contains("ConstantReactionTime = 0.3"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,9") || text.Contains("OldDistributionPersistenceTime = 0.9"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));

        Assert.IsTrue(text.Contains("Model = AUI2"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsTrue(text.Contains("Model = Focus2"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void EvaluationEnvironmentExecutionParameterSupervisorTest()
    {
        Initialize("CDFDRI0.5R3DII30DP10b3ahcspd3");

        BuildEvaluationEnvironment(false, "testEnvParameter1Eval.json", model: "BuildTest/3DBall1.asset");

        string text = RunEvaluationEnvironment("EvaltestEnvParameter1Eval");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("GlobalDrag = 1"));
        Assert.IsTrue(text.Contains("UseNegativeDragDifficulty = True"));
        Assert.IsTrue(text.Contains("BallAgentDifficulty = 60"));
        Assert.IsTrue(text.Contains("BallAgentDifficultyDivisionFactor = 2"));
        Assert.IsTrue(text.Contains("BallStartingRadius = 1,5") || text.Contains("BallStartingRadius = 1.5"));
        Assert.IsTrue(text.Contains("ResetSpeed = 15"));
        Assert.IsTrue(text.Contains("UseFocusAgent = False"));
        Assert.IsTrue(text.Contains("Model = 3DBall1"));

        Assert.IsTrue(text.Contains("Supervisor.SupervisorAgentRandom"));
        Assert.IsTrue(text.Contains("AdvanceNoticeInSeconds = 0"));
        Assert.IsTrue(text.Contains("DifficultyIncrementInterval = 30"));
        Assert.IsTrue(text.Contains("SetConstantDecisionRequestInterval = False"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 0,5") || text.Contains("DecisionRequestIntervalInSeconds = 0.5"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalRangeInSeconds = 3"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 10"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));


        TearDown();
        Initialize("CDTDRI5DII45DP15b3ahc4");

        BuildEvaluationEnvironment(false, "testEnvParameter2Eval.json", model: "BuildTest/3DBall2.asset");

        text = RunEvaluationEnvironment("EvaltestEnvParameter2Eval");

        AssertHasException(text);

        Assert.IsTrue(text.Contains("GlobalDrag = -0,1") || text.Contains("GlobalDrag = -0.1"));
        Assert.IsTrue(text.Contains("UseNegativeDragDifficulty = False"));
        Assert.IsTrue(text.Contains("BallAgentDifficulty = 70"));
        Assert.IsTrue(text.Contains("BallAgentDifficultyDivisionFactor = 3"));
        Assert.IsTrue(text.Contains("BallStartingRadius = 1"));
        Assert.IsTrue(text.Contains("ResetSpeed = 10"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsTrue(text.Contains("Model = Focus2"));

        Assert.IsTrue(text.Contains("Model = AUI2"));
        Assert.IsTrue(text.Contains("AdvanceNoticeInSeconds = 0"));
        Assert.IsTrue(text.Contains("DifficultyIncrementInterval = 45"));
        Assert.IsTrue(text.Contains("SetConstantDecisionRequestInterval = True"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 5"));
        Assert.IsFalse(text.Contains("DecisionRequestIntervalRangeInSeconds = 1"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 15"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void EvaluationEnvironmentExecutionParameterSupervisorOverwriteTest()
    {
        Initialize("CDTDRI99DII99DP99b3ahcspd3");

        BuildEvaluationEnvironment(false, "testEnvParameter1Eval.json", true, "EvalConfIncSupervisorSettings.json", model: "BuildTest/3DBall1.asset");

        string text = RunEvaluationEnvironment("EvaltestEnvParameter1Eval");

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("GlobalDrag = 99"));
        Assert.IsTrue(text.Contains("UseNegativeDragDifficulty = True"));

        Assert.IsTrue(text.Contains("SetConstantDecisionRequestInterval = True"));
        Assert.IsTrue(text.Contains("DecisionRequestIntervalInSeconds = 99"));
        Assert.IsTrue(text.Contains("DifficultyIncrementInterval = 99"));
        Assert.IsTrue(text.Contains("DecisionPeriod = 99"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void AbcModelForDecisionPeriodNotFoundTest()
    {
        BuildAbcEnvironment(false, configFileName: "testEnvAbc1.json");

        LogAssert.Expect(LogType.Log, new Regex("Build succeeded"));

        string arguments = "-simulation 5 " +
                            "-sigma 0.2 " +
                            "-sigmaMean 0,6 " +
                            "-updatePeriode 0,7 " +
                            "-observationProbability 0,3 " +
                            "-constantReactionTime 0,7 " +
                            "-oldDistributionPersistenceTime 0,4 " +
                            "-decisionPeriodBallAgent 99 " +
                            "-id 1";

        string text = RunProductionEnvironment("abcSimulation", arguments);

        Debug.Log(text);

        Assert.IsTrue(text.Contains("Could not find model with decision period of 99."));
    }

    [Test]
    public void AbcMultipleExecutionTest()
    {
        BuildAbcEnvironment(false, configFileName:"testEnvAbc1.json");

        LogAssert.Expect(LogType.Log, new Regex("Build succeeded"));

        string arguments =  "-simulation 5 " +
                            "-sigma 0.2 " +
                            "-sigmaMean 0,6 " +
                            "-updatePeriode 0,7 " +
                            "-observationProbability 0,3 " +
                            "-constantReactionTime 0,7 " +
                            "-oldDistributionPersistenceTime 0,4 " +
                            "-decisionPeriodBallAgent 6 " +
                            "-id 1";

        string text = RunProductionEnvironment("abcSimulation", arguments, 50000);

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("Ball3DAgentHumanCognition"));
        Assert.IsTrue(text.Contains("NumberOfBins = 200"));
        Assert.IsTrue(text.Contains("_numberOfBins = 225"));
        Assert.IsTrue(text.Contains("ShowBeliefState = False"));
        Assert.IsTrue(text.Contains("NumberOfSamples = 300"));
        Assert.IsTrue(text.Contains("Sigma = 0,2") || text.Contains("Sigma = 0.2"));
        Assert.IsTrue(text.Contains("SigmaMean = 0,6") || text.Contains("SigmaMean = 0.6"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 0,7") || text.Contains("UpdatePeriod = 0.7"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,3") || text.Contains("ObservationProbability = 0.3"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 0,7") || text.Contains("ConstantReactionTime = 0.7"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,4") || text.Contains("OldDistributionPersistenceTime = 0.4"));
        Assert.IsTrue(text.Contains("UseFocusAgent = True"));

        Assert.IsTrue(text.Contains("SaveBehavioralData = True"));

        Assert.IsTrue(text.Contains("NumberOfAreaBinsPerDirection = 14"));
        Assert.IsTrue(text.Contains("NumberOfAngleBinsPerAxis = 4"));
        Assert.IsTrue(text.Contains("NumberOfBallVelocityBinsPerAxis = 5"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_ballPosition = 12"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_angle = 12"));
        Assert.IsTrue(text.Contains("NumberOfDistanceBins_velocity = 12"));
        Assert.IsTrue(text.Contains("NumberOfActionBinsPerAxis = 5"));
        Assert.IsTrue(text.Contains("NumberOfTimeBins = 1"));

        Assert.IsTrue(text.Contains("IsTerminatingTask = True"));
        Assert.IsTrue(text.Contains("MaxNumberEpisodes = 5"));
        Assert.IsTrue(text.Contains("MinimumScoreForMeasurement = 0"));
        Assert.IsTrue(text.Contains("SampleSize = 5"));
        Assert.IsTrue(text.Contains("SimulationId = 1"));

        Assert.IsTrue(text.Contains("Model = AUI1"));
        Assert.IsTrue(text.Contains("Model = 3DBall1"));
        Assert.IsTrue(text.Contains("Model = Focus1"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));

        Assert.IsTrue(text.Contains("MaxNumberEpisodes reached, quit application."));
        Assert.IsTrue(text.Contains("Max number of samples collected. Quit Application..."));


        arguments = "-simulation 7 " +
                    "-sigma 0.4 " +
                    "-sigmaMean 1,2 " +
                    "-updatePeriode 1,4 " +
                    "-observationProbability 0,6 " +
                    "-constantReactionTime 1,4 " +
                    "-oldDistributionPersistenceTime 0,8 " +
                    "-decisionPeriodBallAgent 6 " +
                    "-id 2";

        text = RunProductionEnvironment("abcSimulation", arguments);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("Sigma = 0,4") || text.Contains("Sigma = 0.4"));
        Assert.IsTrue(text.Contains("SigmaMean = 1,2") || text.Contains("SigmaMean = 1.2"));
        Assert.IsTrue(text.Contains("UpdatePeriod = 1,4") || text.Contains("UpdatePeriod = 1.4"));
        Assert.IsTrue(text.Contains("ObservationProbability = 0,6") || text.Contains("ObservationProbability = 0.6"));
        Assert.IsTrue(text.Contains("ConstantReactionTime = 1,4") || text.Contains("ConstantReactionTime = 1.4"));
        Assert.IsTrue(text.Contains("OldDistributionPersistenceTime = 0,8") || text.Contains("OldDistributionPersistenceTime = 0.8"));

        Assert.IsTrue(text.Contains("MaxNumberEpisodes = 7"));
        Assert.IsTrue(text.Contains("SampleSize = 7"));
        Assert.IsTrue(text.Contains("SimulationId = 2"));

        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void AbcExecutionRandomSupervisorTest()
    {
        string arguments = "-simulation 200 " +
                    "-sigma 0.4 " +
                    "-sigmaMean 1,2 " +
                    "-updatePeriode 1,4 " +
                    "-observationProbability 0,6 " +
                    "-constantReactionTime 1,4 " +
                    "-oldDistributionPersistenceTime 0,8 " +
                    "-decisionPeriodBallAgent 7 " +
                    "-id 2";

        BuildAbcEnvironment(false, configFileName: "testEnvAbc2.json");

        LogAssert.Expect(LogType.Log, new Regex("Build succeeded"));

        string text = RunProductionEnvironment("abcSimulation", arguments);

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("NumberOfTimeBins = 5"));
        Assert.IsTrue(text.Contains("Supervisor.SupervisorAgentRandom"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void AbcExecutionNoFocusAgentTest()
    {
        string arguments = "-simulation 200 " +
                    "-sigma 0.4 " +
                    "-sigmaMean 1,2 " +
                    "-updatePeriode 1,4 " +
                    "-observationProbability 0,6 " +
                    "-constantReactionTime 1,4 " +
                    "-oldDistributionPersistenceTime 0,8 " +
                    "-decisionPeriodBallAgent 7 " +
                    "-id 2";

        BuildAbcEnvironment(false, configFileName: "testEnvAbc2.json");

        LogAssert.Expect(LogType.Log, new Regex("Build succeeded"));

        string text = RunProductionEnvironment("abcSimulation", arguments);

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("NumberOfTimeBins = 5"));

        Assert.IsTrue(text.Contains("UseFocusAgent = False"));
        Assert.IsTrue(text.Contains("Model = 3DBall2"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }

    [Test]
    public void AbcModelLoadingTest()
    {
        string arguments = "-simulation 200 " +
                    "-sigma 0.4 " +
                    "-sigmaMean 1,2 " +
                    "-updatePeriode 1,4 " +
                    "-observationProbability 0,6 " +
                    "-constantReactionTime 1,4 " +
                    "-oldDistributionPersistenceTime 0,8 " +
                    "-decisionPeriodBallAgent 3 " + 
                    "-id 2";

        BuildAbcEnvironment(false, configFileName: "testEnvAbcMultipleModels.json");

        LogAssert.Expect(LogType.Log, new Regex("Build succeeded"));

        string text = RunProductionEnvironment("abcSimulation", arguments);

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("NumberOfTimeBins = 5"));

        Assert.IsTrue(text.Contains("UseFocusAgent = False"));
        Assert.IsTrue(text.Contains("Model = 3DBallBall4EnvAfDP3RtAhcTfTBt"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));


        arguments = "-simulation 200 " +
                    "-sigma 0.4 " +
                    "-sigmaMean 1,2 " +
                    "-updatePeriode 1,4 " +
                    "-observationProbability 0,6 " +
                    "-constantReactionTime 1,4 " +
                    "-oldDistributionPersistenceTime 0,8 " +
                    "-decisionPeriodBallAgent 6 " +
                    "-id 2";

        text = RunProductionEnvironment("abcSimulation", arguments);

        Debug.Log(text);

        AssertHasException(text);

        Assert.IsTrue(text.Contains("NumberOfTimeBins = 5"));

        Assert.IsTrue(text.Contains("UseFocusAgent = False"));
        Assert.IsTrue(text.Contains("Model = 3DBallBall4EnvAfDP6RtAhcTfTBt"));
        Assert.IsFalse(text.Contains("BehaviorType = HeuristicOnly"));
        Assert.IsFalse(text.Contains("BehaviorType = Default"));
    }


    private void AssertHasException(string text)
    {
        string textLower = text.ToLower();

        if (textLower.Contains("exception"))
        {
            int i = textLower.IndexOf("exception");
            int start = i - 200 >= 0 ? i - 200 : 0;
            int length = i + 200 < textLower.Length ? 400 : textLower.Length - i;

            string subString = textLower.Substring(start, length);
            Debug.Log(subString);
        }

        Assert.IsFalse(textLower.Contains("exception"));
        Assert.IsFalse(textLower.Contains("error"));
    }

    private void BuildAbcEnvironment(bool useMock = true, string configFileName = "testEnvAbc.json")
    {
        string configPath = Path.Combine("Assets", "Tests", "BuildTests", configFileName);
        _commandLineInterfaceMock.GetCommandLineArgs().Returns(new string[] { "Dummy", "-executeMethod", "BuildScript.BuildAbcEnvironment", configPath, "-o"});
        APIHelper.CommandLineInterface = _commandLineInterfaceMock;

        DefineProjectSettings(useMock);

        BuildScript.BuildAbcEnvironment();
    }

    private void BuildTrainingEnvironment(bool useMock = true, string configFileName = "testEnv.json")
    {
        string configPath = Path.Combine("Assets", "Tests", "BuildTests", configFileName);
        _commandLineInterfaceMock.GetCommandLineArgs().Returns(new string[] { "Dummy", "-executeMethod", "BuildScript.BuildTrainingEnvironment", configPath, "-o"});
        APIHelper.CommandLineInterface = _commandLineInterfaceMock;

        DefineProjectSettings(useMock);

        BuildScript.BuildTrainingEnvironment();
    }

    private string BuildEvaluationEnvironment(bool useMock = true, string configFileName = "testEnvEval.json", bool provideComparisonFile = true, string evalConfigFileName = "EvalConf.json", string model = "BuildTest/testsession/BALL1EnvTest/testEnv.asset")
    {
        string configDir = Path.Combine("Assets", "Tests", "BuildTests", "testsession");;
        string configPath = Path.Combine(configDir, "BALL1EnvTest", configFileName);
        string evalConfFile = Path.Combine(configDir, "config", evalConfigFileName);
        string comparisonFile = Path.GetFileName(_comparisonFilePath);

        if (provideComparisonFile)
        {
            _commandLineInterfaceMock.GetCommandLineArgs().Returns(new string[] { "Dummy", "-executeMethod", "BuildScript.BuildEvaluationEnvironment", _buildPath, configPath, model, evalConfFile, comparisonFile, "-o" });
        }
        else
        {
            _commandLineInterfaceMock.GetCommandLineArgs().Returns(new string[] { "Dummy", "-executeMethod", "BuildScript.BuildEvaluationEnvironment", _buildPath, configPath, model, evalConfFile, "-o" });
        }

        APIHelper.CommandLineInterface = _commandLineInterfaceMock;

        DefineProjectSettings(useMock);

        BuildScript.BuildEvaluationEnvironment();

        return model;
    }

    private string RunProductionEnvironment(string environmentName, string arguments = "", int period = 20000)
    {
        string path = Path.Combine(_workingDirectory, "..", "Build", environmentName);
        string logPath = Path.Combine(path, "SupervisorML_Data", "Logs");

        BackUpLogFilesOfProductionEnvironment(logPath);

        RunEnvironment(path, arguments, period);

        string text = File.ReadAllText(@Path.Combine(logPath, "LogFile.txt"));

        RestoreLogFilesOfProductionEnvironment(logPath);

        return text;
    }

    private void BackUpLogFilesOfProductionEnvironment(string logPath)
    {
        try
        {
            File.Delete(Path.Combine(logPath, "LogFileTMP.txt"));
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not delete LogFile: {0}", e.Message));
        }

        try
        {
            File.Move(Path.Combine(logPath, "LogFile.txt"), Path.Combine(logPath, "LogFileTMP.txt"));
            File.Delete(Path.Combine(logPath, "LogFile.txt"));
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not move and delete LogFile: {0}", e.Message));
        }
    }

    private void RestoreLogFilesOfProductionEnvironment(string logPath)
    {
        try
        {
            File.Move(Path.Combine(logPath, "LogFile.txt"), Path.Combine(logPath, "LogFileTestCase.txt"));
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not move LogFile: {0}", e.Message));
        }

        try
        {
            File.Move(Path.Combine(logPath, "LogFileTMP.txt"), Path.Combine(logPath, "LogFile.txt"));
            File.Delete(Path.Combine(logPath, "LogFileTMP.txt"));
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not move and delete LogFile: {0}", e.Message));
        }
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

    private string RunEvaluationEnvironment(string buildName = "EvaltestEnv", int period = 20000)
    {
        string path = Path.Combine(_workingDirectory, "..", "Build", _buildPath, buildName);
        string logPath = Path.Combine(path, "SupervisorML_Data", "Logs", "LogFile.txt");

        try
        {
            File.Delete(logPath);
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Could not delete LogFile: {0}", e.Message));
        }

        RunEnvironment(path, "", period);

        string logPathBackUp = Path.Combine(path, "SupervisorML_Data", "Logs", string.Format("LogFile{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmss")));

        string text = File.ReadAllText(logPath);

        File.Move(logPath, logPathBackUp);

        return text;
    }

    private void RunEnvironment(string path, string arguments = "", int period = 20000)
    {
        Process proc = new Process();
        proc.StartInfo.FileName = Path.Combine(path, "SupervisorML.exe");

        proc.StartInfo.Arguments = arguments;
        proc.Start();
        //wait for program start
        System.Threading.Thread.Sleep(period);
        try
        {
            proc.Kill();
        }
        catch (InvalidOperationException)
        {
            Debug.Log("Process already exited.");
        }
        System.Threading.Thread.Sleep(1000);
    }
}


