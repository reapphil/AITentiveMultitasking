using Supervisor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Search;
using UnityEngine.UI;


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
    SupervisorAgentRandom,
    NoSupport
}


public class ProjectSettings : MonoBehaviour, IProjectSettings
{
    //Managed by Script and set by SupervisorChoice.Set
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public Supervisor.SupervisorAgent SupervisorAgent { get; private set; }

    //Managed by Script
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public FocusAgent FocusAgent { get; set; }

    //Managed by Script
    [field: SerializeField, ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(False))]
    public Text ProjectSettingsText { get; set; }

    //Could also be displayed via the respective property of the SupervisorAgent and the ProjectAssign attribute but is displayed here for better
    //overview.
    [field: SerializeField, Header("General Settings"), Tooltip("Mode of the supervisor: " +
    "Force -> automatic switch, the user cannot decide if the switch should be performed;   " +
    "Notification -> the user will be notified about upcoming switch and can perform the switch during 1 second " +
    "If the switch was not performed by the user, the switch is performed after expiry of this 1 second;   " +
    "Suggestion: the supervisor only suggestion a switch, the decision remains by the user")]
    public Supervisor.Mode Mode { get; set; }

    public SupervisorChoice SupervisorChoice { 
        get 
        {
            return _supervisorChoice; 
        } 
        set 
        {
            _supervisorChoice = value;
            UpdateSupervisorAgent();
        } 
    }

    [SerializeField]
    private SupervisorChoice _supervisorChoice;

    [field: SerializeField, Tooltip("The user controls the task if true, otherwise the task-agent.") ]
    public bool GameMode { get; set; }

    [field: Header("Generate Bin Data with Raw Data"),
    SerializeField, Tooltip("Path to raw data which should be used for the generation of bin data based on the bin settings.")]
    public string RawData { get; set; }

    [InspectorButton("OnSearchRawDataOnDiskButtonClicked")]
    public bool BrowseRawData;

    [InspectorButton("OnGenerateBinDataButtonClicked")]
    public bool GenerateBinData;

    [field: SerializeField, Tooltip("List of models used in the experiment. The AITentiveModel allows to automatically assign the correct model to " +
        "a specific agent. Usually this list contains a supervisor model. In case of the training of the supervisor, also task agent models are " +
        "defined.")]
    public List<AITentiveModel> AITentiveModels { get; set; }

    [field: SerializeField, Tooltip("List of task prefabs that should be used for the experiment."), SearchContext("Tagstring:Task")]
    public GameObject[] TasksGameObjects { get; set; }

    [field: SerializeField, Tooltip("The index of the input list corresponds to task game object. The configured Input Action Asset is used for the" +
        "corresponding task and allows different input settings on task instance level.")]
    public List<InputActionAsset> Inputs { get; set; }

    public Agent[] Agents { get; private set; }

    public ITask[] Tasks
    {
        get => TasksGameObjects?.ToList().ConvertAll(x => x != null ? x.transform.GetChildByName("Agent").GetComponent<ITask>() : null).ToArray();
    }

    public MeasurementSettings MeasurementSettings => gameObject.GetComponent<MeasurementSettings>();

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

    public bool AllTasksAreAutonomous()
    {
        if (Tasks is not null)
        {
            foreach (ITask task in Tasks)
            {
                if (task != null && !task.IsAutonomous)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool SupervisorIsRandomSupervisor()
    {
        return SupervisorChoice == SupervisorChoice.SupervisorAgentRandom;
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

        return GetProjectAssignFieldsFor(GetSupervisorAgentForSupervisorChoice(), result);
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
    public void UpdateSettings(bool isBuild = false)
    {
        CreateAgents(GetTasks());
        UpdateSupervisorAgent();
        UpdateMode(isBuild);

        ConfigurePerformanceMeasurment(GetTaskModels(), GetSupervisorModels());
    }

    public void GenerateFilename()
    {
        GetManagedComponentFor<PerformanceMeasurement>().FileNameForScores = Util.GenerateScoreFilename();
        GetManagedComponentFor<BehaviorMeasurementBehavior>().FileNameForBehavioralData = Util.GenerateBehavioralFilename();
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
            if(aITentiveModel != null)
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

    public SupervisorAgent GetActiveSupervisor()
    {
        List<SupervisorAgent> supervisorAgents = SupervisorAgent.gameObject.GetComponents<SupervisorAgent>().ToList();

        foreach (SupervisorAgent supervisorAgent in supervisorAgents)
        {
            if (supervisorAgent.isActiveAndEnabled)
            {
                return supervisorAgent;
            }
        }

        return null;
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

    public List<Component> GetManagedComponentsFor(Type t)
    {
        if (SupervisorAgent.gameObject.GetBaseComponent(t) != null)
        {
            return SupervisorAgent.gameObject.GetComponents(t).ToList();
        }

        if (FocusAgent.GetComponent(t) != null)
        {
            return FocusAgent.GetComponents(t).ToList();
        }

        foreach (GameObject task in TasksGameObjects)
        {
            if (task != null && task.GetComponentInChildren(t) != null)
            {
                return task.GetComponentsInChildren(t).ToList();
            }
        }

        return new();
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

        return !isModelProvided && SupervisorChoice != SupervisorChoice.SupervisorAgentRandom && !GameMode;
    }

    public bool IsTrainingModeTasks(Dictionary<Type, List<AITentiveModel>> taskModels = null)
    {
        return (taskModels == null || GetNumberOfDifferentTasks() != taskModels.Count) && !GameMode;
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
            GetManagedComponentFor<PerformanceMeasurement>().IsAbcSimulation = GetManagedComponentFor<BehaviorMeasurementBehavior>().IsAbcSimulation = true;
            SupervisorChoice = SupervisorChoice.SupervisorAgentRandom;
            Core.ExitOnNumberOfCalls = 2;

            SetHumanCognitionParameters(args);
            UpdateSimulationSettings(int.Parse(args["-simulation"]));
            SetBehaviorTypeOfTaskAgents(GetTaskModels());

            ProjectAssignValuesToFields();
        }
        else
        {
            GetManagedComponentFor<PerformanceMeasurement>().IsAbcSimulation = GetManagedComponentFor<BehaviorMeasurementBehavior>().IsAbcSimulation = false;
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
        GetManagedComponentFor<BehaviorMeasurementBehavior>().SimulationId = int.Parse(args["-id"], CultureInfo.InvariantCulture);

        double ObservationProbability = double.Parse(args["-observationProbability"].Replace(',', '.'), CultureInfo.InvariantCulture);

        Assert.IsTrue(ObservationProbability >= 0 && ObservationProbability <= 1);
    }

    private void UpdateSimulationSettings(int sample)
    {
        InitBallAgents();

        PerformanceMeasurement performanceMeasurement = GetManagedComponentFor<PerformanceMeasurement>();
        BehaviorMeasurementBehavior behaviourMeasurementBehaviour = GetManagedComponentFor<BehaviorMeasurementBehavior>();

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

        Hyperparameters hyperparameters = new Hyperparameters
        {
            tasks = SupervisorAgent.TaskNames
        };

        BehaviorMeasurementBehavior behaviourMeasurementBehaviour = GetManagedComponentFor<BehaviorMeasurementBehavior>();

        BehavioralDataCollectionSettings behavioralDataCollectionSettings = new BehavioralDataCollectionSettings
        {
            updateExistingModelBehavior = behaviourMeasurementBehaviour.UpdateExistingModelBehavior,
            fileNameForBehavioralData = behaviourMeasurementBehaviour.FileNameForBehavioralData,
            numberOfTimeBins = behaviourMeasurementBehaviour.NumberOfTimeBins,
        };

        BehaviorMeasurementConverter.ConvertRawToBinData(supervisorSettings, hyperparameters, behavioralDataCollectionSettings, RawData);

        GUIUtility.ExitGUI();
    }
#endif

    private void UpdateSupervisorAgent()
    {
        List<SupervisorAgent> supervisorAgents = new();
        SupervisorAgent.GetComponents(supervisorAgents);
        supervisorAgents.ForEach(i => i.enabled = false);
        SupervisorAgent supervisorAgent = GetSupervisorAgentForSupervisorChoice();

        RestoreSupervisorConfiguration(SupervisorAgent, supervisorAgent);
        SupervisorAgent = supervisorAgent;

        SupervisorAgent.enabled = true;
    }

    private SupervisorAgent GetSupervisorAgentForSupervisorChoice()
    {
        GameObject gameObject = SupervisorAgent.gameObject;

        if (_supervisorChoice == SupervisorChoice.NoSupport)
        {
            return SupervisorAgent;
        }
        else
        {
            string supervisorChoice = "Supervisor." + _supervisorChoice.ToString();
            return (SupervisorAgent)gameObject.GetBaseComponent(Util.GetType(supervisorChoice));
        }
    }

    private void RestoreSupervisorConfiguration(Supervisor.SupervisorAgent source, Supervisor.SupervisorAgent target)
    {
        target.TaskGameObjects = source.TaskGameObjects;
        target.TaskGameObjectsProjectSettingsOrdering = source.TaskGameObjectsProjectSettingsOrdering;
        target.CumulativeRewardText = source.CumulativeRewardText;
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
        if (projectAssignFields == null)
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

        List<GameObject> sortetTaskGameObjects = new List<GameObject>(taskGameObjects);
        //The order of the state information is relevant. Therefore, a supervisor trained on TaskA, TaskB would not work for the order TaskB, TaskA
        sortetTaskGameObjects.Sort((x, y) => string.Compare(x.name, y.name));

        FocusAgent.TaskGameObjects = sortetTaskGameObjects.ToArray();
        FocusAgent.TaskGameObjectsProjectSettingsOrdering = taskGameObjects.ToArray();
        SupervisorAgent.TaskGameObjects = sortetTaskGameObjects.ToArray();
        SupervisorAgent.TaskGameObjectsProjectSettingsOrdering = taskGameObjects.ToArray();

        if (GameMode) 
        {
            AssignInputs(taskGameObjects);
        }
    }

    private void AssignInputs(List<GameObject> taskGameObjects)
    {
        for (int i = 0; i < taskGameObjects.Count; i++)
        {
            PlayerInput playerInput = taskGameObjects[i].transform.GetChildByName("Agent").GetComponent<PlayerInput>();

            try 
            {
                playerInput.actions = Inputs[i];
            }
            catch (ArgumentOutOfRangeException)
            {
                Inputs.Add(Inputs[0]);
                playerInput.actions = Inputs[i];
            }
        }
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

            for (int i = 0; i <= level && parent != null; i++)
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
        int xStart = numberOfTasks % 2 == 0 ? (numberOfTasks / 2) * (-distanceBetweenTasks) + distanceBetweenTasks / 2 : (numberOfTasks / 2) * (-distanceBetweenTasks);
        coordinates[0] = new Vector3(xStart, 0, 5);

        for (int i = 1; i < numberOfTasks; i++)
        {
            coordinates[i] = new Vector3(coordinates[i - 1].x + distanceBetweenTasks, 0, 5);
        }

        return coordinates;
    }

    private void UpdateMode(bool isBuild = false)
    {
        Dictionary<Type, List<AITentiveModel>> taskModels = GetTaskModels();
        List<AITentiveModel> focusAgentModels = GetFocusModels();
        List<AITentiveModel> supervisorAgentModels = GetSupervisorModels();

        SetBehaviorTypeOfTaskAgents(taskModels);
        NNModel focusAgentModel = SetBehaviorTypeOfFocusAgent(taskModels, focusAgentModels);
        NNModel supervisorAgentModel = SetBehaviorTypeOfSupervisorAgent(supervisorAgentModels, isBuild);
        SetBehaviorTypeForAutonomousTaskAgentsTraining(taskModels, supervisorAgentModels);

        SetObservationShape(supervisorAgentModel, SupervisorAgent, SupervisorAgent.VectorObservationSize);
        SetActionSpec(SupervisorAgent);

        SetObservationShape(focusAgentModel, FocusAgent, FocusAgent.VectorObservationSize);
        SetActionSpec(FocusAgent, CRUtil.GetFocusableGameObjectsOfTasks(Tasks.ToList()).Sum(x => x.VisualElements.Count));
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
            if (GameMode)
            {
                Agents[i].GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
            }
            else
            {
                Type baseType = Agents[i].GetType().GetLowestBaseTypeInHierarchyOf(typeof(Agent));

                if (taskModels.ContainsKey(baseType))
                {
                    SetModelForAgent(Agents[i], taskModels[baseType], ((ITask)GetManagedComponentFor(baseType)).DecisionPeriod);
                    Agents[i].GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
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
            else
            {
                FocusAgent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
            }
        }
        else
        {
            FocusAgent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
        }

        return focusAgentModel;
    }

    private NNModel SetBehaviorTypeOfSupervisorAgent(List<AITentiveModel> supervisorAgentModels = null, bool isBuild = false)
    {
        NNModel supervisorAgentModel = null;

        if (!supervisorAgentModels.IsNullOrEmpty())
        {
            supervisorAgentModel = SetModelForAgent(SupervisorAgent, supervisorAgentModels);
        }

        if (!isBuild)
        {
            SupervisorAgent.Mode = Mode;
        }

        if (SupervisorChoice == SupervisorChoice.NoSupport)
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

        //return dummy model for random supervisor
        if (SupervisorIsRandomSupervisor())
        {
            supervisorAgentModel = SupervisorAgent.GetComponent<BehaviorParameters>().Model;
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

    private void SetObservationShape(NNModel agentModel, Agent agent, int shape = 0)
    {
        if (agentModel != null)
        {
            int observationShape = GetObservationShape(ModelLoader.Load(agentModel));
            agent.gameObject.GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize = observationShape;
        }
        else
        {
            if (shape != 0)
            {
                agent.gameObject.GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize = shape;
            }
        }
    }

    private void SetActionSpec(Agent agent, int number = 0)
    {
        agent.GetComponent<BehaviorParameters>().BrainParameters.ActionSpec = new Unity.MLAgents.Actuators.ActionSpec(discreteBranchSizes: new int[] { number == 0 ? TasksGameObjects.Length : number });
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
        BehaviorMeasurementBehavior behaviourMeasurementBehaviour = GetManagedComponentFor<BehaviorMeasurementBehavior>();

        if (IsTrainingMode(taskModels, supervisorAgentModels))
        {
            performanceMeasurement.IsTrainingMode = true;
        }
        else
        {
            performanceMeasurement.IsTrainingMode = false;
        }

        performanceMeasurement.IsSupervised = true;
        if (SupervisorChoice == SupervisorChoice.NoSupport)
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
