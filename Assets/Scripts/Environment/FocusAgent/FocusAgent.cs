using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;

public class FocusAgent : Agent
{
    [field: SerializeField, Tooltip("Defines the reward function of the focus agent. \"r_{number}\" is interpreted as the values returned by " +
        "the \"GetTaskReward\" function of the specific tasks. {number} start with 0 and enumerates the tasks displayed from left to right. All " +
        "function of the Math library can be used (without Math. prefix)."), ProjectAssign]
    public string RewardFunction { get; set; } = "r_0";

    [field: SerializeField, Tooltip("Shows the current focused and encoded objects."), ProjectAssign]
    public bool ShowFocusedObject { get; set; } = false;

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public float ScreenWidthPixel { get; set; } = 0;

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public float ScreenHightPixel { get; set; } = 0;

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public float ScreenDiagonalInch { get; set; } = 0;

    [field: SerializeField, Tooltip("The distance between the task displays in CM."), ProjectAssign]
    public float DistanceBetweenTasksDisplays { get; set; } = 0;

    [field: SerializeField, Tooltip("Must be defined for the training. For all other modes, the size is determined by the provided model.")]
    public int VectorObservationSize { get; set; }

    [field: SerializeField]
    public GameObject[] TaskGameObjects { get; set; }

    [field: SerializeField]
    public GameObject[] TaskGameObjectsProjectSettingsOrdering { get; set; }

    public ITask[] Tasks
    {
        get
        {
            return ITask.GetTasksFromGameObjects(TaskGameObjects);
        }
    }

    public ITask[] TasksProjectSettingsOrdering
    {
        get
        {
            return ITask.GetTasksFromGameObjects(TaskGameObjectsProjectSettingsOrdering);
        }
    }


    private const float PREPERATIONTIME = 0.135f;
    private const float EXECUTIONTIMEEMMA = 0.07f;
    private const float SACCADETIMEEMMA = 0.002f;


    private List<VisualStateSpace> FocusableObjects { get; set; }

    private Supervisor.SupervisorAgent _supervisorAgent;

    private float[] _encodingTimer;

    private float _saccadeTimer = 0.0f;

    private float[] _encodingTime;

    private GameObject[] _eyeCanvases;

    private float[] _completedTime;

    private float _saccadeTime;

    private int _targetIndex;

    private int _currentFixationIndex;


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        _currentFixationIndex = _targetIndex;
        _targetIndex = actionBuffers.DiscreteActions[0];
        (_encodingTime, _saccadeTime) = CalculateEMMAFixationTime(_targetIndex); 
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_supervisorAgent.GetActiveTaskNumber());

        foreach (ITask task in Tasks)
        {
            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                ((ICrTask)task).AddBeliefObservationsToSensor(sensor);
            }
            else
            {
                task.AddTrueObservationsToSensor(sensor);
            }
        }
    }

    public override void Initialize()
    {
        _supervisorAgent = gameObject.transform.root.GetComponent<Supervisor.SupervisorAgent>();

        FocusableObjects = CRUtil.GetFocusableGameObjectsOfTasks(Tasks.ToList());

        if (ShowFocusedObject) InitVisualization();

        Encode(0);

        int count = GetFocusableObjectsCount();
        _encodingTimer = new float[count];
        _encodingTime = new float[count];
        _completedTime = new float[count];

        _saccadeTime = 0;
        _saccadeTimer = 0;
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += CatchEndEpisode;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= CatchEndEpisode;
    }


    private void FixedUpdate()
    {
        for (int i = 0; i < _encodingTimer.Length; i++)
        {
            _encodingTimer[i] += Time.fixedDeltaTime;

            if (_encodingTimer[i] > _encodingTime[i])
            {
                Encode(i);
            }
            else
            {
                Decode(i);
            }
        }

        _saccadeTimer += Time.fixedDeltaTime;

        if (_saccadeTimer > _saccadeTime)
        {
            RequestDecision();
            _saccadeTimer = 0;
        }

        SetReward(GetReward());
    }

    private float GetReward()
    {
        Dictionary<string, object> parameters = new();

        for (int i = 0; i < Tasks.Length; i++)
        {
            parameters.Add($"r_{i}", TasksProjectSettingsOrdering[i].TaskRewardForFocusAgent.IsNullOrEmpty() ? 0 : TasksProjectSettingsOrdering[i].TaskRewardForFocusAgent.DequeueAll().Sum());
        }

        float reward = (float)FunctionInterpreter.Interpret(RewardFunction, parameters);

        return reward;
    }

    /// <summary>
    /// Calculates the needed time for the EMMA model to fixate on the target object.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>(encodingTime, saccadeTime)</returns>
    private Tuple<float[], float> CalculateEMMAFixationTime(int targetIndex)
    {
        float[] newEncodingTimes = new float[_encodingTime.Length];

        float saccadeTime = 0;

        for (int i = 0; i < _encodingTime.Length; i++)
        {
            if(i == targetIndex)
            {
                (newEncodingTimes[i], _completedTime[i]) = CaclulateEncodingTimes(_currentFixationIndex, targetIndex);
                saccadeTime = _completedTime[i];
            }
            else
            {
                (newEncodingTimes[i], _completedTime[i]) = CaclulateEncodingTimes(_currentFixationIndex, i);
            }
        }

        if (newEncodingTimes[targetIndex] < PREPERATIONTIME)
        {
            //new decision after encoding is done
            return new Tuple<float[], float>(newEncodingTimes, newEncodingTimes[targetIndex]);
        }
        else
        {
            return new Tuple<float[], float>(newEncodingTimes, saccadeTime);
        }
    }

    private Tuple<float, float> CaclulateEncodingTimes(int indexSource, int indexTaget)
    {
        (int taskIndexSource, int indexObjectSource) = ActionTo2DIndex(indexSource);
        (int taskIndexTarget, int indexObjectTarget) = ActionTo2DIndex(indexTaget);

        float distance = Vector3.Distance(FocusableObjects[taskIndexSource].GetScreenCoordinatesForGameObjectIndex(indexObjectSource), FocusableObjects[taskIndexTarget].GetScreenCoordinatesForGameObjectIndex(indexObjectTarget));

        distance = CRUtil.PixelToCM(distance, ScreenWidthPixel, ScreenHightPixel, ScreenDiagonalInch) / 100;
        distance = taskIndexSource == taskIndexTarget ? distance : distance + DistanceBetweenTasksDisplays;

        float eccentricity = VisualDistance(distance);
        float encodingTime = CalculateEMMAEncodingTime(eccentricity);

        float executionTime = EXECUTIONTIMEEMMA + SACCADETIMEEMMA * eccentricity;
        float completedTime = PREPERATIONTIME + executionTime;

        if (encodingTime < _encodingTime[indexTaget])
        {
            encodingTime = (1 - (_completedTime[indexTaget] / _encodingTime[indexTaget])) * encodingTime;
        }
        else if(encodingTime > _encodingTime[indexTaget])
        {
            //resets the timer if the encoding time increases --> the fixation moved away from the object
            _encodingTimer[indexTaget] = 0;
        }

        //Debug.Log("indexSource: " + indexSource + "; indexTaget: " + indexTaget + "; distance: " + distance + "encodingTime: " + encodingTime);

        return new Tuple<float, float>(encodingTime, completedTime);
    }

    private float VisualDistance(float distance)
    {
        //in meter
        float userDistance = 0.3f;

        return 180 * (Mathf.Atan(distance / userDistance) / Mathf.PI);
    }

    private float CalculateEMMAEncodingTime(float eccentricity, float frequency = 0.1f)
    {
        float K = 0.006f;
        float k = 0.4f;

        return K * -Mathf.Log(frequency) * Mathf.Exp(k * eccentricity);
    }

    private void Encode(int index)
    {
        (int targetTaskIndex, int targetIndex) = ActionTo2DIndex(index);
        FocusableObjects[targetTaskIndex].ActivateElement(targetIndex);

        if (Tasks[targetTaskIndex].GetType().GetInterfaces().Contains(typeof(ICrTask)))
        {
            VisualStateSpace visualStateSpace = ((ICrTask)Tasks[targetTaskIndex]).FocusStateSpace;
            visualStateSpace.ActivateElement(targetIndex);
        }

        if (ShowFocusedObject) VisualizeEncoding(index);
    }

    private void Decode(int index)
    {
        (int targetTaskIndex, int targetIndex) = ActionTo2DIndex(index);
        FocusableObjects[targetTaskIndex].DeactivateElement(targetIndex);

        if (Tasks[targetTaskIndex].GetType().GetInterfaces().Contains(typeof(ICrTask)))
        {
            VisualStateSpace visualStateSpace = ((ICrTask)Tasks[targetTaskIndex]).FocusStateSpace;
            visualStateSpace.DeactivateElement(targetIndex);
        }

        if (ShowFocusedObject) VisualizeDecoding(index);
    }

    private void VisualizeEncoding(int index)
    {
        Transform eyeImage = _eyeCanvases[index].transform.GetChildByName("Image");

        if (index != _currentFixationIndex)
        {
            eyeImage.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            eyeImage.GetComponent<Image>().color = new Color(255 / 255f, 255 / 255f, 50 / 255f, 150 / 255f);
        }
        else
        {
            eyeImage.localScale = new Vector3(1f, 1f, 1f);
            eyeImage.GetComponent<Image>().color = new Color(0, 0, 0, 255 / 255f);
        }

        _eyeCanvases[index].SetActive(true);
    }

    private void VisualizeDecoding(int index)
    {
        _eyeCanvases[index].SetActive(false);
    }

    private void CatchEndEpisode(object sender, bool aborted)
    {
        foreach (ITask task in Tasks)
        {
            while (task.TaskRewardForFocusAgent.Count > 0)
            {
                SetReward(task.TaskRewardForFocusAgent.DequeueAll().Sum());
            }
        }

        EndEpisode();
    }

    private Tuple<int, int> ActionTo2DIndex(int action)
    {
        Tuple<int, int> result;

        foreach (VisualStateSpace visualStateSpace in FocusableObjects)
        {
            if (action < visualStateSpace.VisualElements.Count)
            {
                result = new Tuple<int, int>(FocusableObjects.IndexOf(visualStateSpace), action);
                return result;
            }

            action -= visualStateSpace.VisualElements.Count;
        }

        throw new ArgumentException("Action is out of range");
    }

    private int GetFocusableObjectsCount() 
    {
        int count = 0;

        foreach (VisualStateSpace visualStateSpace in FocusableObjects)
        {
            count += visualStateSpace.VisualElements.Count;
        }

        return count;
    }

    private void InitVisualization()
    {
        _eyeCanvases = new GameObject[GetFocusableObjectsCount()];
        int i = 0;

        foreach (ITask task in Tasks)
        {
            GameObject eyeCanvas = task.GetGameObject().transform.parent.transform.GetChildByName("Camera").GetChildByName("Eye_Canvas").gameObject;

            if (task.GetType().GetInterfaces().Contains(typeof(ICrTask)))
            {
                VisualStateSpace visualStateSpace = ((ICrTask)task).FocusStateSpace;

                if (visualStateSpace.VisualElements.Count > 1)
                {
                    foreach (GameObject visualElement in visualStateSpace.VisualElements)
                    {
                        _eyeCanvases[i] = Instantiate(eyeCanvas, visualElement.transform.position, Quaternion.identity, visualElement.transform);
                        _eyeCanvases[i].transform.localScale = new Vector3(1f, 1f, 1f);
                        _eyeCanvases[i].SetActive(false);
                        _eyeCanvases[i].transform.GetChildByName("Image").localPosition = new Vector3(0, 25, 0);

                        i++;
                    }
                }
                else
                {
                    _eyeCanvases[i] = eyeCanvas;

                    i++;
                }
            }
        }
    }
}
