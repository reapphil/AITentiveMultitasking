using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BuildScript
{
    public static bool IsDevelopmentBuild { get; set; } = false;


    public static void BuildTrainingEnvironment()
    {
        List<string> args = APIHelper.GetArgs();
        string confFile = args[0];

        string[] parts = confFile.Split('.');
        Assert.AreEqual("json", parts[parts.Length - 1]);

        string buildPath = Path.Combine("Build", "TrainingEnvironment");

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(confFile);

        ((Hyperparameters)settings[typeof(Hyperparameters)]).saveBehavioralData = false;
        if (settings.ContainsKey(typeof(BehavioralDataCollectionSettings)))
        {
            ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).collectDataForComparison = false;
        }
        else
        {
            settings[typeof(BehavioralDataCollectionSettings)] = new BehavioralDataCollectionSettings();
            ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).collectDataForComparison = false;
        }
        
        //a value of -1 does not change the timeScale value via editor so that the timeScale value of ml-agents is used
        ((Hyperparameters)settings[typeof(Hyperparameters)]).timeScale = -1;

        BuildEnvironment(settings, buildPath, Validator.ValidateTraining);
    }

    public static void BuildAbcEnvironment()
    {
        List<string> args = APIHelper.GetArgs();
        string confFile = args[0];

        string[] parts = confFile.Split('.');
        Assert.AreEqual("json", parts[parts.Length - 1]);

        string buildPath = Path.Combine("Build", "abcSimulation");

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(confFile);

        ((Hyperparameters)settings[typeof(Hyperparameters)]).timeScale = 20;
        ((Hyperparameters)settings[typeof(Hyperparameters)]).abcSimulation = true;
        ((Hyperparameters)settings[typeof(Hyperparameters)]).saveBehavioralData = true;

        BuildEnvironment(settings, buildPath, Validator.ValidateAbc);
    }

    public static void BuildEvaluationEnvironment()
    {
        List<string> args = APIHelper.GetArgs();
        string buildName = args[0];
        string confFile = args[1];
        string ballAgentModelName = args[2];
        string evalConfFile = args[3]; ;
        string comparisonFileName = args.Count >= 5 ? args[4] : "";
        bool overwriteOldEvaluation = args.Contains("-o") ? true : false;

        string[] parts = confFile.Split('.');
        Assert.AreEqual("json", parts[parts.Length - 1]);

        parts = ballAgentModelName.Split('.');
        Assert.AreEqual("asset", parts[parts.Length - 1]);

        parts = comparisonFileName.Split('.');
        bool comparisonFileGiven = false;

        if ("json" == parts[parts.Length - 1])
        {
            comparisonFileGiven = true;
        }

        string buildPath = Path.Combine("Build", buildName, "Eval" + Path.GetFileNameWithoutExtension(confFile));
        string workingDirectory = Application.dataPath;
        workingDirectory = workingDirectory.Replace("Assets", ".");

        if (File.Exists(Path.Combine(workingDirectory, buildPath, "SupervisorML.exe")) && !overwriteOldEvaluation)
        {
            Debug.Log("Environment already built, skip building...");
            return;
        }
        else
        {
            Debug.Log("Building environment in progress...");
        }

        Dictionary<Type, ISettings> settings = SettingsLoader.LoadSettings(confFile);
        Dictionary<Type, ISettings> evaluationSettings = SettingsLoader.LoadSettings(evalConfFile);

        ((Hyperparameters)settings[typeof(Hyperparameters)]).saveBehavioralData = true;
        settings[typeof(BehavioralDataCollectionSettings)] = evaluationSettings[typeof(BehavioralDataCollectionSettings)];

        //TODO: Currently the values of the given settings are not nullable, so that it could be that the default value would be a valid definition but it would not be assigned.
        if (evaluationSettings.ContainsKey(typeof(SupervisorSettings)))
        {
            Util.AssignNonDefault((SupervisorSettings)settings[typeof(SupervisorSettings)], (SupervisorSettings)evaluationSettings[typeof(SupervisorSettings)]);
            Util.AssignNonDefault((BalancingTaskSettings)settings[typeof(BalancingTaskSettings)], (BalancingTaskSettings)evaluationSettings[typeof(BalancingTaskSettings)]);
        }

        //The following naming convention leads to too long paths: hyperparametersBase.behavioralDataCollectionSettings.fileNameForBehavioralData = Path.GetFileNameWithoutExtension(confFile) + ".json";
        ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).fileNameForBehavioralData = string.Format("eval{0}.json", DateTime.Now.ToString("yyyyMMddHHmm"));
        //higher time scale result in an imprecise time measurements
        ((Hyperparameters)settings[typeof(Hyperparameters)]).timeScale = 20;
        BehavioralDataCollectionSettings behavioralDataCollectionSettings = evaluationSettings[typeof(BehavioralDataCollectionSettings)] as BehavioralDataCollectionSettings;

        if (comparisonFileGiven)
        {
            ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).collectDataForComparison = true;
            ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).comparisonFileName = comparisonFileName;
        }
        else
        {
            ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).collectDataForComparison = false;
        }

        ((BehavioralDataCollectionSettings)settings[typeof(BehavioralDataCollectionSettings)]).updateExistingModelBehavior = true;

        if (!((SupervisorSettings)settings[typeof(SupervisorSettings)]).randomSupervisor)
        {
            parts = ((Hyperparameters)settings[typeof(Hyperparameters)]).supervisorModelName.Split('.');
            Assert.AreEqual("asset", parts[parts.Length - 1]);
        }

        ((Hyperparameters)settings[typeof(Hyperparameters)]).taskModels["BallAgent"] = ballAgentModelName;

        (string, string, string) paths = Util.BuildPathsForBehavioralDataFileName(comparisonFileName, behavioralDataCollectionSettings, ((SupervisorSettings)settings[typeof(SupervisorSettings)]), ((BalancingTaskSettings)settings[typeof(BalancingTaskSettings)]));

        BuildEnvironment(settings, buildPath, Validator.ValidateEvaluation);

        if (comparisonFileGiven)
        {
            //Copies the comparison files to the evaluation environment as well
            File.Copy(paths.Item1, Path.Combine(workingDirectory, buildPath, "SupervisorML_Data", "Scores", Util.GetScoreString((SupervisorSettings)settings[typeof(SupervisorSettings)], (BalancingTaskSettings)settings[typeof(BalancingTaskSettings)]), comparisonFileName), true);
            File.Copy(paths.Item2, Path.Combine(workingDirectory, buildPath, "SupervisorML_Data", "Scores", Util.GetScoreString((SupervisorSettings)settings[typeof(SupervisorSettings)], (BalancingTaskSettings)settings[typeof(BalancingTaskSettings)]), Path.GetFileName(paths.Item2)), true);
            File.Copy(paths.Item3, Path.Combine(workingDirectory, buildPath, "SupervisorML_Data", "Scores", Util.GetScoreString((SupervisorSettings)settings[typeof(SupervisorSettings)], (BalancingTaskSettings)settings[typeof(BalancingTaskSettings)]), Path.GetFileName(paths.Item3)), true);
        }
    }

    private static BuildPlayerOptions CreateBuildPlayerOptions(bool cleanBuild = true)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;

        if (cleanBuild)
        {
            buildPlayerOptions.options = BuildOptions.CleanBuildCache | BuildOptions.ForceEnableAssertions | BuildOptions.StrictMode;
        }
        else
        {
            buildPlayerOptions.options = BuildOptions.ForceEnableAssertions | BuildOptions.StrictMode;
        }

        if (IsDevelopmentBuild)
        {
            buildPlayerOptions.options |= BuildOptions.Development;
        }

        buildPlayerOptions.scenes = new[] { Path.Combine("Assets", "Scenes", "SupervisorML.unity") };

        return buildPlayerOptions;
    }

    private static void LogResult(BuildSummary summary)
    {
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[" + System.DateTime.Now + "] " + "Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("[" + System.DateTime.Now + "] " + "Build failed");
        }
    }

    private static void BuildEnvironment(Dictionary<Type, ISettings> settings, string buildPath, Action<Dictionary<Type, ISettings>> validate)
    {
        string workingDirectory = Application.dataPath;
        workingDirectory = workingDirectory.Replace("Assets", ".");

        validate(settings);

        string oldScenePath = SceneManagement.ConfigSceneWithBackUp(settings);

        BuildSummary summary = BuildPlayer(buildPath, true);

        Directory.CreateDirectory(Path.Combine(workingDirectory, buildPath, "SupervisorML_Data", "Scores", Util.GetScoreString((SupervisorSettings)settings[typeof(SupervisorSettings)], (BalancingTaskSettings)settings[typeof(BalancingTaskSettings)])));
        Directory.CreateDirectory(Path.Combine(workingDirectory, buildPath, "SupervisorML_Data", "Logs"));

        LogResult(summary);

        SceneManagement.RestoreScene(oldScenePath);
    }

    private static BuildSummary BuildPlayer(string buildPath, bool cleanBuild = true)
    {
        BuildPlayerOptions buildPlayerOptions = CreateBuildPlayerOptions(cleanBuild);

        string workingDirectory = Application.dataPath;
        workingDirectory = workingDirectory.Replace("Assets", ".");

        buildPlayerOptions.locationPathName = Path.Combine(buildPath, "SupervisorML.exe");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        return summary;
    }
}
