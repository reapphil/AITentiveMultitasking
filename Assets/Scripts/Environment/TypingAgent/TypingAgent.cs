using Algorithms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;


[Serializable, JsonObject]
public class TypingAgent : Agent, ITask
{
    [field: SerializeField, Tooltip("Determines if the supervisor should end its episode if the episode of the task ends"), ProjectAssign]
    public bool IsTerminatingTask { get; set; }

    [field: SerializeField, ProjectAssign]
    public bool IsAutonomous { get; set; }

    [field: SerializeField, ProjectAssign]
    public bool ShowFingerPosition { get; set; }

    [field: SerializeField, ProjectAssign]
    public string QuizName { get; set; } = "quiz1.csv";

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public int ScreenWidthPixel { get; set; } = 0;

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public int ScreenHightPixel { get; set; } = 0;

    [field: SerializeField, Tooltip("Used to determine the ppi of the screen needed to accurately calculate the distances of the finger movement."), ProjectAssign]
    public float ScreenDiagonalInch { get; set; } = 0;

    public bool IsActive { get; set; }

    public Queue<float> TaskRewardForSupervisorAgent { get; private set; }

    public Queue<float> TaskRewardForFocusAgent { get; private set; }

    public TextMeshProUGUI QuestionText;

    public TextMeshProUGUI AnswerText;

    public VisualStateSpace FingerPositionStateSpace;

    public KeyboardScript KeyboardScript;

    public int DecisionPeriod { get; set; }

    public Dictionary<string, double> Performance => new();

    public IStateInformation StateInformation
    {
        get
        {
            _typingStateInformation ??= new TypingStateInformation();

            return _typingStateInformation;
        }
        set
        {
            _typingStateInformation = value as TypingStateInformation;
        }
    }


    protected QnA _currentQnA;

    protected string _previousAnswer;

    protected int _startingButton;
    
    protected float _timeOfEpisode;


    private List<QnA> _qnAs;

    private static System.Random _rnd = new System.Random();

    private FadeImageAnimation _correctImageAnimation;

    private FadeImageAnimation _wrongImageAnimation;

    private TypingStateInformation _typingStateInformation;

    


    /// <summary>
    /// Will not be called in heuristic mode
    /// </summary>
    /// <param name="actionBuffers"></param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        List<dynamic> actions = new();
        var discreteActionsOut = actionBuffers.DiscreteActions;

        int index = actionBuffers.DiscreteActions[0];
        actions.Add(index);

        ITask.InvokeOnAction(actions, this);

        if (IsActive || IsAutonomous)
        {
            FingerPositionStateSpace.ActivateSingleElement(index);
            FingerPositionStateSpace.VisualElements[index].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

            if (ShowFingerPosition) { ProjectImage(FingerPositionStateSpace, "FingerClick"); }


            float reward = GetReward();

            TaskRewardForSupervisorAgent.Enqueue(reward);
            TaskRewardForFocusAgent.Enqueue(reward);
            SetReward(reward);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        AddTrueObservationsToSensor(sensor);
    }

    public void AddTrueObservationsToSensor(VectorSensor sensor) 
    {
        sensor.AddOneHotObservation((int)GetButtonCharValue(FingerPositionStateSpace.GetFirstActiveElement()), 128);
        sensor.AddOneHotObservation((int)GetTarget(_currentQnA.Answer, AnswerText.text), 128);
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnMove(InputValue value) { }

    public void ResetPerformance() { }

    public void UpdateDifficultyLevel() { }

    //reward b
    public void Enter()
    {
        float reward = GetFinalReward();
        
        SetReward(reward);
        TaskRewardForFocusAgent.Enqueue(reward);
        TaskRewardForSupervisorAgent.Enqueue(reward);

        Debug.Log($" Finale Reward: {reward}, Total reward of episode: {GetCumulativeReward()}.");

        if (IsTerminatingTask)
        {
            ITask.InvokeTermination();
        }
        else
        {
            EndEpisode();
        }

        Debug.Log("End Episode: Enter button was pressed.");
    }

    public float GetFinalReward()
    {
        float reward;

        float levenshteinDistance = TextDistance.CalculateLevenshtein(_currentQnA.Answer.ToLower(), AnswerText.text.ToLower());
        float frequencyDistance = TextDistance.CalculateLetterFrequencyDistance(AnswerText.text.ToLower(), _currentQnA.Answer.ToLower());

        reward = _currentQnA.Answer.Length - levenshteinDistance/2 - frequencyDistance;
        //distance to answer length reward dependent on time
        reward *= reward < 0 ? MaximizeAtC(AnswerText.text.Length, 0, 3f, 0.1f) : MaximizeAtC(AnswerText.text.Length, _currentQnA.Answer.Length, _currentQnA.Answer.Length/_timeOfEpisode, 0.02f) * GetNumberOfCorrectLetters(AnswerText.text);
        reward = _currentQnA.Answer.ToLower() == AnswerText.text.ToLower() ? reward * 2 : reward;

        return reward;
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        Debug.Log("Begin Episode");

        int r = _rnd.Next(_qnAs.Count);

        _currentQnA = _qnAs[r];

        _previousAnswer = "";
        AnswerText.text = "";
        QuestionText.text = _currentQnA.Question;
        _timeOfEpisode = 0;
    }


    protected int GetNumberOfCorrectLetters(string writtenAnswer)
    {
        int correctLetters = 0;

        for (int i = 0; i < writtenAnswer.Length && i < _currentQnA.Answer.Length; i++)
        {
            if (writtenAnswer.ToLower()[i] == _currentQnA.Answer.ToLower()[i])
            {
                correctLetters += 1;
            }
            else
            {
                break;
            }
        }

        return correctLetters;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Supervisor.SupervisorAgent.EndEpisodeEvent += CatchEndEpisode;
        TaskRewardForFocusAgent = new();
        TaskRewardForSupervisorAgent = new();
        _startingButton = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly ? 30 : 14;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Supervisor.SupervisorAgent.EndEpisodeEvent -= CatchEndEpisode;
    }

    protected void ProjectImage(VisualStateSpace visualStateSpace, string imageName= "Finger")
    {
        Transform fingerCanvas = transform.GetChildByName("FingerCanvas");

        foreach (Transform child in fingerCanvas)
        {
            child.gameObject.SetActive(false);
        }

        if(visualStateSpace.GetFirstActiveElement() != null)
        {
            Vector3 position = visualStateSpace.GetFirstActiveElement().transform.position;
            GameObject fingerImage = fingerCanvas.GetChildByName(imageName).gameObject;
            fingerImage.SetActive(true);
            fingerImage.transform.position = position;
        }
    }

    protected char GetTarget(string targetString, string currentString)
    {
        if (currentString.Length <= targetString.Length && (currentString.Length == 0 || currentString.ToLower() == targetString[..(currentString.Length)].ToLower()))
        {
            if (currentString.Length == targetString.Length)
            {
                return '\n';
            }
            else
            {
                return Char.ToLower(targetString[currentString.Length]);
            }
        }
        else
        {
            return '\x7F';
        }
    }

    protected GameObject GetTargetButton(string targetString, string currentString)
    {
        char target = GetTarget(targetString, currentString);

        foreach (GameObject gameObject in FingerPositionStateSpace.VisualElements)
        {
            if (GetButtonStringValue(gameObject) == target.ToString())
            {
                return gameObject;
            }
        }

        return null;
    }

    protected bool IsButtonVisible(char button)
    {
        foreach (GameObject gameObject in FingerPositionStateSpace.VisualElements)
        {
            if (GetButtonCharValue(gameObject) == Char.ToLower(button))
            {
                if (gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected float GetReward()
    {
        int reward;

        if (KeyboardScript.LastPressedButton == GetTarget(_currentQnA.Answer.ToLower(), _previousAnswer.ToLower()))
        {
            reward = 1;
        }
        else
        {
            reward = -1;
        }

        _previousAnswer = AnswerText.text;

        return reward;
    }


    private void Update()
    {
        if (KeyboardScript.ButtonWasPressed)
        {
            KeyboardScript.ButtonWasPressed = false;
            float reward = GetReward();
            TaskRewardForFocusAgent.Enqueue(reward);
            TaskRewardForSupervisorAgent.Enqueue(reward);
        }
    }

    private void FixedUpdate()
    {
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly)
        {
            RequestSimpleDecision();
        }

        _timeOfEpisode += Time.fixedDeltaTime;
    }

    private void RequestSimpleDecision()
    {
        if (IsAutonomous || IsActive)
        {
            RequestDecision();
        }
    }

    protected GameObject GetButton(char button)
    {
        foreach (GameObject gameObject in FingerPositionStateSpace.VisualElements)
        {
            if (GetButtonCharValue(gameObject) == Char.ToLower(button))
            {
                return gameObject;
            }
        }

        return null;
    }

    protected string GetButtonStringValue(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return "\x00";
        }

        string st = gameObject.name;
        string start = "Button (";

        int pFrom = st.IndexOf(start) + start.Length;
        int pTo = st.LastIndexOf(")");

        return st.Substring(pFrom, pTo - pFrom);
    }

    protected char GetButtonCharValue(GameObject gameObject)
    {
        string st = GetButtonStringValue(gameObject);

        return char.Parse(Regex.Unescape(st));
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        _qnAs = Util.ReadDatafromCSV<QnA>(Path.Combine(Application.streamingAssetsPath, QuizName));
        _correctImageAnimation = transform.GetChildInHierarchyByName("Correct").GetComponent<FadeImageAnimation>();
        _wrongImageAnimation = transform.GetChildInHierarchyByName("Wrong").GetComponent<FadeImageAnimation>();
        FingerPositionStateSpace.ActivateElement(_startingButton);
    }

    private void CatchEndEpisode(object sender, bool aborted)
    {
        EndEpisode();
    }

    /// <summary>
    /// Function to maximize a function at a certain point c
    /// </summary>
    /// <param name="x"></param>
    /// <param name="c">Point of maximum</param>
    /// <param name="C">Maximum value of c</param>
    /// <param name="k">Slope</param>
    /// <returns>The computed function value, which is a maximum at 'c' and converges to 0 as x moves away from 'c'.</returns>
    private float MaximizeAtC(float x, float c, float C, float k)
    {
        return C / (1 + k * (float)Math.Pow(x - c, 2));
    }
}


public class QnA
{
    public QnA() { }

    public string Question { get; set; }

    public string Answer { get; set; }
}
