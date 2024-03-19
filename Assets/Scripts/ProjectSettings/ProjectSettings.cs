using Castle.Core.Internal;
using CsvHelper;
using NSubstitute.Routing.Handlers;
using Numpy;
using Supervisor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Search;
using UnityEngine.UI;

public enum Mode
{
    GameModeSupervisor,
    GameModeNoSupervisor,
    GameModeNotification,
    DefaultMode
};


public enum AgentChoice
{
    Ball3DAgentOptimal,
    Ball3DAgentHumanCognition,
    Ball3DAgentHumanCognitionSingleProbabilityDistribution
}


public enum SupervisorChoice
{
    SupervisorAgent,
    SupervisorAgentV1,
    SupervisorAgentRandom
}


public class ProjectSettings : MonoBehaviour, IProjectSettings
{
    //Managed by Script
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public Supervisor.SupervisorAgent SupervisorAgent { get; set; }

    //Managed by Script
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public FocusAgent FocusAgent { get; set; }

    //Managed by Script
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public Text ProjectSettingsText { get; set; }

    [field: SerializeField, Header("General Settings"), Tooltip("Definition of the following modes: \n" +
    "GameModeSupervisor - human player controls only the platform, \n" +
    "GameModeNoSupervisor - human player controls the platform and the task switch")]
    public Mode Mode { get; set; }

    [field: SerializeField]
    public SupervisorChoice SupervisorChoice { get; set; }

    [field: Header("Generate Bin Data with Raw Data"),
    SerializeField, Tooltip("Path to raw data which should be used for the generation of bin data based on the bin settings.")]
    public string RawData { get; set; }

    [InspectorButton("OnSearchRawDataOnDiskButtonClicked")]
    public bool BrowseRawData;

    [InspectorButton("OnGenerateBinDataButtonClicked")]
    public bool GenerateBinData;

    [field: SerializeField]
    public List<AITentiveModel> AITentiveModels { get; set; }

    [field: SerializeField]
    public GameObject[] TasksGameObjects { get; set; }

    public Agent[] Agents { get; private set; }

    public ITask[] Tasks 
    { 
        get => TasksGameObjects?.ToList().ConvertAll(x => x != null ? x.transform.GetChildByName("Agent").GetComponent<ITask>() : null).ToArray(); 
    }


    private int _sampleSize;

    private int _simulationId;


    public bool AtLeastOneTaskUsesFocusAgent()
    {
        if (Tasks is not null)
        {
            foreach (ITask task in Tasks)
            {
                if (task != null && task.GetType().GetInterfaces().Contains(typeof(ICrTask)) && ((ICrTask)task).UseFocusAgent)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool AtLeastOneTaskIsAutonomous()
    {
        if (Tasks is not null)
        {
            foreach (ITask task in Tasks)
            {
                if (task != null && task.IsAutonomous)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool SupervisorIsRandomSupervisor()
    {
        return SupervisorChoice == SupervisorChoice.SupervisorAgentRandom;
    }

    public bool ModeIsGameModeSupervisor()
    {
        return Mode == Mode.GameModeSupervisor;
    }

    public bool ModeIsGameModeNoSupervisor()
    {
        return Mode == Mode.GameModeNoSupervisor;
    }

    public bool ModeIsGameModeNotification()
    {
        return Mode == Mode.GameModeNotification;
    }

    public bool ModeIsDefaultMode()
    {
        return Mode == Mode.DefaultMode;
    }

    public bool ModeIsGameMode()
    {
        return Mode == Mode.GameModeSupervisor || Mode == Mode.GameModeNoSupervisor || Mode == Mode.GameModeNotification;
    }

    public bool ModeIsNotGameMode()
    {
        return Mode != Mode.GameModeSupervisor && Mode != Mode.GameModeNoSupervisor && Mode != Mode.GameModeNotification;
    }

    public static IProjectSettings GetProjectSettings(Scene scene)
    {
        GameObject[] allObjects = scene.GetRootGameObjects();
        IProjectSettings projectSettings = null;

        if (scene.isLoaded)
        {
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].name == "ProjectSettings")
                {
                    projectSettings = allObjects[i].GetComponent<ProjectSettings>();
                }
            }
        }

        return projectSettings;
    }

    public List<(Component, FieldInfo)> GetProjectAssignFieldsForTasks()
    {
        List<GameObject> allObjects = GetGameObjectHierarchyOf(TasksGameObjects);
        allObjects = allObjects.Distinct().ToList();

        return GetProjectAssignFieldsFor(allObjects);
    }

    public List<(Component, FieldInfo)> GetProjectAssignFieldsForSupervisor()
    {
        List<(Component, FieldInfo)> result = GetProjectAssignFieldsFor(new List<GameObject>() { SupervisorAgent.gameObject });
        result.RemoveAll(x => x.Item1.GetType().IsSubclassOf(typeof(SupervisorAgent)) || x.Item1.GetType() == typeof(SupervisorAgent));

        return GetProjectAssignFieldsFor(GetManagedComponentFor(Util.GetType("Supervisor." + SupervisorChoice.ToString())), result);
    }

    public List<(Component, FieldInfo)> GetProjectAssignFieldsForFocusAgent()
    {
        return GetProjectAssignFieldsFor(new List<GameObject>() { FocusAgent.gameObject });
    }

    public void ProjectAssignValuesToFields()
    {
        List<string> missingVariableWarnings = new List<string>();

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            foreach (Component component in go.GetComponents(typeof(Component)))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                List<Type> types = new();

                if(component != null)
                {
                    types = component.GetType().GetParentTypes().ToList();
                    types.Add(component.GetType());
                }

                foreach (Type t in types)
                {
                    foreach (var field in t.GetFields(flags))
                    {
                        if (Attribute.IsDefined(field, typeof(ProjectAssignAttribute)))
                        {
                            Component projectAssignComponent = GetManagedComponentFor(t);

                            // If the field exists, set its value
                            if (projectAssignComponent != null)
                            {
                                field.SetValue(component, field.GetValue(projectAssignComponent));
                            }
                            else if (!TasksGameObjects.IsNullOrEmpty() && Tasks.FirstOrDefault(x => x != null && x.GetType() == t) != null)
                            {
                                missingVariableWarnings.Add(string.Format("Could not find any corresponding variable in the DataStorage for variable {0} of class {1} with assigned ProjectAssign attribute.", GetCleanName(field.Name), t.Name));
                            }
                        }
                    }
                }
            }
        }

        missingVariableWarnings = missingVariableWarnings.Distinct().ToList();

        foreach (string missingVariableWarning in missingVariableWarnings)
        {
            Debug.LogWarning(missingVariableWarning);
        }
    }

    //the optional parameter t is needed for Build scripts since the UnityEditor library cannot be used for these scripts.
    public void UpdateSettings()
    {
        CreateAgents(GetTasks());
        UpdateSupervisorAgent();
        UpdateMode();

        ConfigurePerformanceMeasurment(GetTaskModels(), GetSupervisorModels());
    }

    public void GenerateFilename()
    {
        GetManagedComponentFor<PerformanceMeasurement>().FileNameForScores = Util.GenerateScoreFilename();
        GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().FileNameForBehavioralData = Util.GenerateBehavioralFilename();
    }

    public Dictionary<Type, List<AITentiveModel>> GetTaskModels()
    {
        Dictionary<Type, List<AITentiveModel>> models = new();

        foreach (AITentiveModel aITentiveModel in AITentiveModels)
        {
            if (aITentiveModel.GetType().GetInterface("ITask") != null)
            {
                if (!models.ContainsKey(aITentiveModel.GetType()))
                {
                    models[aITentiveModel.GetType()] = new();
                }

                models[aITentiveModel.GetType()].Add(aITentiveModel);
            }
        }

        return models;
    }

    public Dictionary<Type, List<AITentiveModel>> GetModels()
    {
        Dictionary<Type, List<AITentiveModel>> models = new();

        foreach (AITentiveModel aITentiveModel in AITentiveModels)
        {
            if (!models.ContainsKey(aITentiveModel.GetType()))
            {
                models[aITentiveModel.GetType()] = new();
            }

            models[aITentiveModel.GetType()].Add(aITentiveModel);
        }

        return models;
    }

    public List<AITentiveModel> GetSupervisorModels()
    {
        return GetModels().GetValueOrDefault(typeof(Supervisor.SupervisorAgent), new()); ;
    }

    public List<AITentiveModel> GetFocusModels()
    {
        return GetModels().GetValueOrDefault(typeof(FocusAgent), new()); ;
    }

    public T GetManagedComponentFor<T>()
    {
        return (T)(object)GetManagedComponentFor(typeof(T));
    }

    public Component GetManagedComponentFor(Type t)
    {
        if (SupervisorAgent.gameObject.GetBaseComponent(t) != null)
        {
            return SupervisorAgent.gameObject.GetBaseComponent(t);
        }

        if (FocusAgent.GetComponent(t) != null)
        {
            return FocusAgent.GetComponent(t);
        }

        foreach (GameObject task in TasksGameObjects)
        {
            if (task != null && task.GetComponentInChildren(t) != null)
            {
                return task.GetComponentInChildren(t);
            }
        }

        return default;
    }

    public bool IsTrainingModeSupervisor(List<AITentiveModel> supervisorAgentModels)
    {
        bool isModelProvided = false;

        foreach (AITentiveModel aITentiveModel in supervisorAgentModels)
        {
            if (aITentiveModel.Model != null)
            {
                isModelProvided = true;
                break;
            }
        }

        return !isModelProvided && SupervisorChoice != SupervisorChoice.SupervisorAgentRandom && ModeIsNotGameMode();
    }

    public bool IsTrainingModeTasks(Dictionary<Type, List<AITentiveModel>> taskModels = null)
    {
        return (taskModels == null || GetNumberOfDifferentTasks() != taskModels.Count) && ModeIsNotGameMode();
    }

    public bool IsTrainingMode(Dictionary<Type, List<AITentiveModel>> taskModels, List<AITentiveModel> supervisorAgentModels)
    {
        return (IsTrainingModeSupervisor(supervisorAgentModels) || IsTrainingModeTasks(taskModels));
    }


    protected void Awake()
    {
        HandleSimulationCMDArgs(Util.GetArgs());
    }


    private bool False()
    {
        return false;
    }

    private void HandleSimulationCMDArgs(Dictionary<string, string> args)
    {
        if (args.ContainsKey("-simulation"))
        {
            GetManagedComponentFor<PerformanceMeasurement>().IsAbcSimulation = GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().IsAbcSimulation = true;
            SupervisorChoice = SupervisorChoice.SupervisorAgentRandom;
            Core.ExitOnNumberOfCalls = 2;
            
            SetHumanCognitionParameters(args);
            UpdateSimulationSettings(int.Parse(args["-simulation"]));
            SetBehaviorTypeOfTaskAgents(GetTaskModels());

            ProjectAssignValuesToFields();
        }
        else
        {
            GetManagedComponentFor<PerformanceMeasurement>().IsAbcSimulation = GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().IsAbcSimulation = false;
        }
    }

    private void SetHumanCognitionParameters(Dictionary<string, string> args)
    {
        Ball3DAgentHumanCognition ball3DAgentHumanCognition = GetManagedComponentFor<Ball3DAgentHumanCognition>();

        if (ball3DAgentHumanCognition != null)
        {
            ball3DAgentHumanCognition.Sigma = double.Parse(args["-sigma"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.SigmaMean = double.Parse(args["-sigmaMean"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.UpdatePeriod = float.Parse(args["-updatePeriode"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.ObservationProbability = double.Parse(args["-observationProbability"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.ConstantReactionTime = double.Parse(args["-constantReactionTime"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.OldDistributionPersistenceTime = float.Parse(args["-oldDistributionPersistenceTime"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ball3DAgentHumanCognition.DecisionPeriod = int.Parse(args["-decisionPeriodBallAgent"], CultureInfo.InvariantCulture);
        }

        GetManagedComponentFor<PerformanceMeasurement>().SimulationId = int.Parse(args["-id"], CultureInfo.InvariantCulture);
        GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>().SimulationId = int.Parse(args["-id"], CultureInfo.InvariantCulture);

        double ObservationProbability = double.Parse(args["-observationProbability"].Replace(',', '.'), CultureInfo.InvariantCulture);

        Assert.IsTrue(ObservationProbability >= 0 && ObservationProbability <= 1);
    }

    private void UpdateSimulationSettings(int sample)
    {
        InitBallAgents();

        PerformanceMeasurement performanceMeasurement = GetManagedComponentFor<PerformanceMeasurement>();
        BalancingTaskBehaviourMeasurementBehaviour behaviourMeasurementBehaviour = GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>();

        behaviourMeasurementBehaviour.UpdateExistingModelBehavior = false;
        performanceMeasurement.FileNameForScores = "sim_scores.csv";
        behaviourMeasurementBehaviour.FileNameForBehavioralData = "sim.csv";
        performanceMeasurement.MaxNumberEpisodes = sample;
        behaviourMeasurementBehaviour.SampleSize = sample;
        performanceMeasurement.MinimumScoreForMeasurement = 0;
        GetManagedComponentFor<SupervisorAgent>().TimeScale = 20;
    }

#if UNITY_EDITOR
    private string GetModelFromFilePanel()
    {
        string path = Application.dataPath;
        path = EditorUtility.OpenFilePanel("Model Selection", Path.Combine(path, "Models"), "onnx");

        if (path == "")
        {
            return "";
        }

        string[] subs = path.Split(@"Models/");

        return subs[1];
    }

    private void OnSearchRawDataOnDiskButtonClicked()
    {
        string rawDataOld = RawData;

        string path = Application.dataPath;
        RawData = EditorUtility.OpenFilePanel("Raw Data Selection", Path.Combine(Directory.GetParent(path).ToString(), "Scores"), "csv");

        if (RawData == "")
        {
            RawData = rawDataOld;
        }

        GUIUtility.ExitGUI();
    }

    private void OnGenerateBinDataButtonClicked()
    {
        SupervisorSettings supervisorSettings = new SupervisorSettings(
            SupervisorChoice == SupervisorChoice.SupervisorAgentRandom ? true : false,
            SupervisorAgent.SetConstantDecisionRequestInterval,
            SupervisorAgent.DecisionRequestIntervalInSeconds,
            SupervisorChoice == SupervisorChoice.SupervisorAgentRandom ? ((SupervisorAgentRandom)SupervisorAgent).DecisionRequestIntervalRangeInSeconds : 0,
            SupervisorAgent.DifficultyIncrementInterval,
            SupervisorAgent.DecisionPeriod,
            SupervisorAgent.AdvanceNoticeInSeconds);

        BallAgent ballAgent = GetManagedComponentFor<BallAgent>();

        BalancingTaskSettings balancingTaskSettings = new BalancingTaskSettings(
            -1,
            ballAgent.GlobalDrag,
            ballAgent.UseNegativeDragDifficulty,
            ballAgent.BallAgentDifficulty,
            ballAgent.BallAgentDifficultyDivisionFactor,
            ballAgent.BallStartingRadius,
            ballAgent.ResetSpeed,
            ballAgent.ResetPlatformToIdentity,
            ballAgent.DecisionPeriod);

        BalancingTaskBehaviourMeasurementBehaviour behaviourMeasurementBehaviour = GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>();

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = behaviourMeasurementBehaviour.UpdateExistingModelBehavior,
            fileNameForBehavioralData = behaviourMeasurementBehaviour.FileNameForBehavioralData,
            numberOfAreaBins_BehavioralData = behaviourMeasurementBehaviour.NumberOfAreaBins_BehavioralData,
            numberOfBallVelocityBinsPerAxis_BehavioralData = behaviourMeasurementBehaviour.NumberOfBallVelocityBinsPerAxis_BehavioralData,
            numberOfAngleBinsPerAxis = behaviourMeasurementBehaviour.NumberOfAngleBinsPerAxis,
            numberOfTimeBins = behaviourMeasurementBehaviour.NumberOfTimeBins,
            numberOfDistanceBins = behaviourMeasurementBehaviour.NumberOfDistanceBins,
            numberOfDistanceBins_velocity = behaviourMeasurementBehaviour.NumberOfDistanceBins_velocity,
            numberOfActionBinsPerAxis = behaviourMeasurementBehaviour.NumberOfActionBinsPerAxis,
            collectDataForComparison = behaviourMeasurementBehaviour.CollectDataForComparison,
            comparisonFileName = behaviourMeasurementBehaviour.ComparisonFileName,
            comparisonTimeLimit = behaviourMeasurementBehaviour.ComparisonTimeLimit,
        };

        BalancingTaskBehaviourMeasurementConverter.ConvertRawToBinData(supervisorSettings, balancingTaskSettings, behavioralDataCollectionSettings, RawData);

        GUIUtility.ExitGUI();
    }
#endif

    private void UpdateSupervisorAgent()
    {
        GameObject gameObject = SupervisorAgent.gameObject;
        List<SupervisorAgent> supervisorAgents = new();
        SupervisorAgent.GetComponents(supervisorAgents);
        supervisorAgents.ForEach(i => i.enabled = false);

        SupervisorAgent supervisorAgent = (SupervisorAgent)gameObject.GetBaseComponent(Util.GetType("Supervisor." + SupervisorChoice.ToString()));
        RestoreSupervisorConfiguration(SupervisorAgent, supervisorAgent);
        SupervisorAgent = supervisorAgent;

        SupervisorAgent.enabled = true;
    }

    private void RestoreSupervisorConfiguration(Supervisor.SupervisorAgent source, Supervisor.SupervisorAgent target)
    {
        target.TaskGameObjects = source.TaskGameObjects;
        target.StopwatchText = source.StopwatchText;
        target.TextMeshProUGUI = source.TextMeshProUGUI;
    }

    private void InitBallAgents()
    {
        Agents = SupervisorAgent.GetBallAgents();
    }

    private List<GameObject> GetTasks()
    {
        List<GameObject> result = new();

        foreach (GameObject taskPrefab in TasksGameObjects)
        {
            if (taskPrefab != null)
            {
                GameObject prefab = Resources.Load<GameObject>(taskPrefab.name);

                if (prefab == null)
                {
                    throw new FileNotFoundException(String.Format("Could not find {0} in Resources folder. Please select a task prefab instead.", taskPrefab.name));
                }
                result.Add(prefab);
            }

        }

        //The order of the state information is relevant. Therefore, a supervisor trained on TaskA, TaskB would not work for the order TaskB, TaskA
        result.Sort((x, y) => string.Compare(x.name, y.name));

        return result;
    }

    private List<(Component, FieldInfo)> GetProjectAssignFieldsFor(List<GameObject> allObjects)
    {
        List<(Component, FieldInfo)> projectAssignFields = new();

        foreach (GameObject go in allObjects)
        {
            foreach (Component component in go.GetComponents(typeof(Component)))
            {
                projectAssignFields = GetProjectAssignFieldsFor(component, projectAssignFields);
            }
        }

        return projectAssignFields;
    }

    private List<(Component, FieldInfo)> GetProjectAssignFieldsFor(Component component, List<(Component, FieldInfo)> projectAssignFields = null)
    {
        if(projectAssignFields == null)
        {
            projectAssignFields = new();
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        List<Type> types = new();

        if (component != null)
        {
            types = component.GetType().GetParentTypes().ToList();
            types.Add(component.GetType());
        }

        foreach (Type t in types)
        {
            foreach (var field in t.GetFields(flags))
            {
                if (Attribute.IsDefined(field, typeof(ProjectAssignAttribute)))
                {
                    //The following if condition prevents that the same field of the same type is added multiple times to the list
                    if (!projectAssignFields.ConvertAll(x => (x.Item1.GetType().GetLowestBaseTypeInHierarchyOf(typeof(Agent)), x.Item2)).ToList().Contains((component.GetType().GetLowestBaseTypeInHierarchyOf(typeof(Agent)), field)))
                    {
                        projectAssignFields.Add((component, field));
                    }
                }
            }
        }

        return projectAssignFields;
    }

    private List<GameObject> GetGameObjectHierarchyOf(GameObject[] gameObjects)
    {
        List<GameObject> allObjects = new();

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject != null)
            {
                allObjects.Add(gameObject);

                foreach (Transform transform in gameObject.transform.GetChildren())
                {
                    allObjects.Add(transform.gameObject);
                }
            }
        }

        return allObjects;
    }

    private void CreateAgents(List<GameObject> taskGameObjects)
    {
        DestroyGameObjects(SupervisorAgent.TaskGameObjects);
        Agents = new Agent[taskGameObjects.Count];
        Vector3[] coordinatesForTasks = GetCoordinatesForTasks(taskGameObjects.Count);

        bool useFocusAgent = AtLeastOneTaskUsesFocusAgent();

        if (useFocusAgent)
        {
            FocusAgent.gameObject.SetActive(true);
        }
        else
        {
            FocusAgent.gameObject.SetActive(false);
        }

        for (int i = 0; i < taskGameObjects.Count; i++)
        {
            GameObject result;

            if (useFocusAgent)
            {
                result = Instantiate(taskGameObjects[i], coordinatesForTasks[i], Quaternion.identity, FocusAgent.transform);
            }
            else
            {
                result = Instantiate(taskGameObjects[i], coordinatesForTasks[i], Quaternion.identity, SupervisorAgent.transform);
            }

            ConfigCamera(result.transform.GetChildByName("Camera").gameObject, i);

            Agents[i] = result.transform.GetChildByName("Agent").GetComponent<Agent>();
            taskGameObjects[i] = result;
        }

        FocusAgent.TaskGameObjects = taskGameObjects.ToArray();
        SupervisorAgent.TaskGameObjects = taskGameObjects.ToArray();
    }

    private void ConfigCamera(GameObject cameraGameObject, int platformNumber)
    {
        float space = 1 / (float)TasksGameObjects.Length;
        Camera camera = cameraGameObject.GetComponent<Camera>();
        //camera.transform.localPosition = new Vector3(0, (float)(6.5 + Tasks.Length * 0.5), -10 - Tasks.Length * 5);
        camera.rect = new Rect(space * platformNumber, 0, space, 1);
    }

    private void DestroyGameObjects(GameObject[] gameObjects, int level = 0)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            GameObject parent = gameObject;
            GameObject child = null;

            for(int i = 0; i <= level && parent != null; i++)
            {
                child = parent;
                parent = parent.transform.parent != null ? parent.transform.parent.gameObject : null;
            }

            DestroyImmediate(child);
        }
    }

    private Vector3[] GetCoordinatesForTasks(int numberOfTasks)
    {
        int distanceBetweenTasks = 1500;
        Vector3[] coordinates = new Vector3[numberOfTasks];
        int xStart = numberOfTasks % 2 == 0 ? (numberOfTasks / 2) * (-distanceBetweenTasks) + distanceBetweenTasks/2 : (numberOfTasks / 2) * (-distanceBetweenTasks);
        coordinates[0] = new Vector3(xStart, 0, 5);

        for (int i = 1; i < numberOfTasks; i++)
        {
            coordinates[i] = new Vector3(coordinates[i - 1].x + distanceBetweenTasks, 0, 5);
        }

        return coordinates;
    }

    private void UpdateMode()
    {
        Dictionary<Type, List<AITentiveModel>> taskModels = GetTaskModels();
        List<AITentiveModel> supervisorAgentModels = GetSupervisorModels();
        List<AITentiveModel> focusAgentModels = GetFocusModels();

        SetBehaviorTypeOfTaskAgents(taskModels);
        NNModel focusAgentModel = SetBehaviorTypeOfFocusAgent(taskModels, focusAgentModels);
        NNModel supervisorAgentModel = SetBehaviorTypeOfSupervisorAgent(supervisorAgentModels);
        SetBehaviorTypeForAutonomousTaskAgentsTraining(taskModels, supervisorAgentModels);

        SetObservationShape(supervisorAgentModel, SupervisorAgent);
        SetActionSpec(SupervisorAgent);

        SetObservationShape(focusAgentModel, FocusAgent);
        SetActionSpec(FocusAgent);
    }

    private void SetBehaviorTypeForAutonomousTaskAgentsTraining(Dictionary<Type, List<AITentiveModel>> taskModels, List<AITentiveModel> supervisorAgentModels)
    {
        if (IsTrainingMode(taskModels, supervisorAgentModels))
        {
            SupervisorAgent.StartCountdownAt = 0;

            //For a independent training of 3DAgent (Autonomous == true) SupervisorAgent BehaviorType must be set to InferenceOnly, otherwise
            //also the Behavior of the SupervisorAgent must be defined in the config file.
            if (AtLeastOneTaskIsAutonomous())
            {
                GetManagedComponentFor<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
            }
        }
    }

    private void SetBehaviorTypeOfTaskAgents(Dictionary<Type, List<AITentiveModel>> taskModels = null)
    {
        for (int i = 0; i < Agents.Length; i++)
        {
            if (ModeIsGameMode())
            {
                Agents[i].GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
            }
            else
            {
                Type baseType = Agents[i].GetType().GetLowestBaseTypeInHierarchyOf(typeof(Agent));

                if (taskModels.ContainsKey(baseType))
                {
                    Agents[i].GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;

                    SetModelForAgent(Agents[i], taskModels[baseType], ((ITask)GetManagedComponentFor(baseType)).DecisionPeriod);
                }
                else
                {
                    Agents[i].GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.Default;
                }
            }
        }
    }

    private NNModel SetBehaviorTypeOfFocusAgent(Dictionary<Type, List<AITentiveModel>> taskModels, List<AITentiveModel> focusAgentModels = null)
    {
        NNModel focusAgentModel = null;

        if (AtLeastOneTaskUsesFocusAgent())
        {
            if (!focusAgentModels.IsNullOrEmpty())
            {
                focusAgentModel = SetModelForAgent(FocusAgent, focusAgentModels);
            }

            if (IsTrainingModeTasks(taskModels))
            {
                FocusAgent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.Default;
            }
        }
        else
        {
            FocusAgent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
        }

        return focusAgentModel;
    }

    private NNModel SetBehaviorTypeOfSupervisorAgent(List<AITentiveModel> supervisorAgentModels = null)
    {
        NNModel supervisorAgentModel = null;

        if (!supervisorAgentModels.IsNullOrEmpty())
        {
            supervisorAgentModel = SetModelForAgent(SupervisorAgent, supervisorAgentModels);
        }

        if (Mode == Mode.GameModeNotification)
        {
            SupervisorAgent.NotificationMode = true;
        }
        else
        {
            SupervisorAgent.NotificationMode = false;
        }

        if (ModeIsGameModeNoSupervisor())
        {
            SupervisorAgent.AdvanceNoticeInSeconds = 0;
            GetManagedComponentFor<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
        }
        else if (IsTrainingModeSupervisor(supervisorAgentModels))
        {
            GetManagedComponentFor<BehaviorParameters>().BehaviorType = BehaviorType.Default;
        }
        else
        {
            GetManagedComponentFor<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
        }

        return supervisorAgentModel;
    }

    private NNModel GetModelForDecisionPeriod(List<AITentiveModel> models, int decisionPeriod = 0)
    {
        string availableDecisionPeriods = "";

        foreach (AITentiveModel aITentiveModel in models)
        {
            if (aITentiveModel.DecisionPeriod == decisionPeriod || decisionPeriod == 0)
            {
                return aITentiveModel.Model;
            }

            availableDecisionPeriods += string.Format(" {0}: {1}", aITentiveModel.name, aITentiveModel.DecisionPeriod);
        }

        Debug.LogError(string.Format("Could not find model with decision period of {0}. Found models with the following decision periods: {1}.", decisionPeriod, availableDecisionPeriods));

        return null;
    }

    private NNModel SetModelForAgent(Agent agent, List<AITentiveModel> models, int decisionPeriod = 0)
    {
        NNModel model = GetModelForDecisionPeriod(models, decisionPeriod);

        agent.GetComponent<BehaviorParameters>().Model = model;

        return model;
    }

    private int CountUniqueTypes(object[] array)
    {
        HashSet<Type> uniqueTypes = new HashSet<Type>();

        foreach (var item in array)
        {
            uniqueTypes.Add(item.GetType());
        }

        return uniqueTypes.Count;
    }

    private int GetNumberOfDifferentTasks()
    {
        return CountUniqueTypes(Agents);
    }

    private void SetObservationShape(NNModel agentModel, Agent agent)
    {
        if (agentModel != null)
        {
            int observationShape = GetObservationShape(ModelLoader.Load(agentModel));
            agent.gameObject.GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize = observationShape;
        }
    }

    private void SetActionSpec(Agent agent)
    {
        agent.GetComponent<BehaviorParameters>().BrainParameters.ActionSpec = new Unity.MLAgents.Actuators.ActionSpec(discreteBranchSizes: new int[] { TasksGameObjects.Length });
    }

    //see GetInputTensors in BarracudaModelExtensions
    private int GetObservationShape(Model model)
    {
        int l = model.inputs[0].shape.Length;

        return model.inputs[0].shape[l - 1];
    }

    private void ConfigurePerformanceMeasurment(Dictionary<Type, List<AITentiveModel>> taskModels, List<AITentiveModel> supervisorAgentModels)
    {
        PerformanceMeasurement performanceMeasurement = GetManagedComponentFor<PerformanceMeasurement>();
        BalancingTaskBehaviourMeasurementBehaviour behaviourMeasurementBehaviour = GetManagedComponentFor<BalancingTaskBehaviourMeasurementBehaviour>();

        if (IsTrainingMode(taskModels, supervisorAgentModels))
        {
            performanceMeasurement.IsTrainingMode = true;
        }
        else
        {
            performanceMeasurement.IsTrainingMode = false;
        }

        performanceMeasurement.IsSupervised = true;
        if (Mode == Mode.GameModeNoSupervisor)
        {
            performanceMeasurement.IsSupervised = false;
        }

        if (behaviourMeasurementBehaviour.SaveBehavioralData && !performanceMeasurement.IsAbcSimulation && !performanceMeasurement.MeasurePerformance)
        {
            performanceMeasurement.MaxNumberEpisodes = 0;
            performanceMeasurement.MinimumScoreForMeasurement = 0;
            if (behaviourMeasurementBehaviour.FileNameForBehavioralData != null || behaviourMeasurementBehaviour.FileNameForBehavioralData == "")
            {
                performanceMeasurement.FileNameForScores = "scores_" + Util.GetCSVFilenameForBehavioralDataConfigString(behaviourMeasurementBehaviour.FileNameForBehavioralData);
            }
            else
            {
                Debug.LogWarning("Filename for behavioral data was not defined: New name generated.");
                GenerateFilename();
            }
        }

        if (behaviourMeasurementBehaviour.SaveBehavioralData)
        {

            if (!SupervisorIsRandomSupervisor())
            {
                behaviourMeasurementBehaviour.NumberOfTimeBins = 1;
            }

            if (!behaviourMeasurementBehaviour.CollectDataForComparison)
            {
                behaviourMeasurementBehaviour.ComparisonFileName = "";
            }

            if (behaviourMeasurementBehaviour.FileNameForBehavioralData != "" && behaviourMeasurementBehaviour.FileNameForBehavioralData == behaviourMeasurementBehaviour.ComparisonFileName)
            {
                throw new ArgumentException("FileNameForBehavioralData and ComparisonFileName must be unequal.");
            }
        }
        else
        {
            behaviourMeasurementBehaviour.CollectDataForComparison = false;
        }
    }

    private string GetCleanName(string name)
    {
        if (name.Contains("<"))
        {
            int pFrom = name.IndexOf("<") + 1;
            int pTo = name.LastIndexOf(">");

            return name.Substring(pFrom, pTo - pFrom);
        }

        return name;
    }
}
