using Algorithms;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static ObjectIn2DGridProbabilitiesUpdateJob;


public class TypingAgentHumanCognition : TypingAgent, ICrTask
{
    [field: SerializeField]
    public VisualStateSpace FocusStateSpace { get; set; }

    [field: SerializeField, Tooltip("Visualizes the current belief finger position."), ProjectAssign]
    public bool ShowBeliefState { get; set; }

    [field: SerializeField, Tooltip("Observation is active for this agent independent of the focus/supervisor agent."), ProjectAssign]
    public bool FullVision { get; set; } = false;

    [field: SerializeField, Tooltip("If active the observations of the driving agents are provided by the focus- instead of the supervisor agent."), ProjectAssign]
    public bool UseFocusAgent { get; set; }

    [field: SerializeField, Tooltip("Defines how much samples should be taken to calculate the probability distributions."), ProjectAssign]
    public int NumberOfSamples { get; set; } = 100;

    [field: SerializeField, Tooltip("Describes O(s,a,o) of the formula b`(s_) = O(s_,a,o) SUM(s_e_S){ T(s,a,s_)*b(s)}."), ProjectAssign]
    public double ObservationProbability { get; set; } = 0.9;

    [field: SerializeField, Tooltip("Specify the number of bins in which the keyboard should be divided."), ProjectAssign]
    public int NumberOfBins { get; set; } = 1000;

    [field: SerializeField]
    public RectTransform KeyboardRectTransform { get; set; }

    public bool IsVisible
    {
        get
        {
            return FullVision;
        }
    }


    protected char _beliefTarget;

    protected string _beliefWrittenAnswer;


    private System.Random _rand;

    private float _movementTime;

    private float _movementTimer = 0.0f;

    private double[] _fingerLocationProbabilities;

    private double[] _fingerLocationProbabilitiesBinSpace;

    private int[] _binOverlabCount;

    private VisualStateSpace _beliefFingerPositionStateSpace;

    private string _previousBeliefWrittenAnswer;

    private int _mouseClicked;

    private Vector2 _previousMousePosition;

    private Vector2 _fingerScreenPosition;

    private int _unverifiedEntriesCount;

    private Vector2 _maxDistanceBetweenButtons;

    private char _beliefClickedButton;

    private Vector2 _mouseVelocity;

    private Vector2 _mouseStartingPosition;

    private bool EpisodeStarted = false;

    private int _stepCount;

    private float _previousTextDistance;

    private Vector2 _lastPerformedAction;

    private int _previousNumberOfCorrectLetters;

    private float _beliefRewardWithoutProof;



    /// <summary>
    /// The sensor input for the focus agent must reflect the current uncertainties of the task. In combination with the reward signal, the focus
    /// agent can learn to focus on the most relevant elements of the task s.t. the reward of the subtasks is maximized.
    /// </summary>
    /// <param name="sensor"></param>
    public void AddBeliefObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation((float)_fingerLocationProbabilities.Max()); // a low max value indicates a strong uncertainty
        sensor.AddObservation(_unverifiedEntriesCount);
        sensor.AddOneHotObservation((int)GetTarget(_currentQnA.Answer, _beliefWrittenAnswer), 128);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        List<dynamic> actions = new();
        var continuousActionsOut = actionBuffers.ContinuousActions;

        Vector2 fingerVelocity = GetFingerVelocity(actionBuffers);
        actions.Add(fingerVelocity);
        float sigma = GetFingerSigma(actionBuffers);
        actions.Add(sigma);
        int actionType = actionBuffers.DiscreteActions[0];
        actions.Add(actionType);

        ITask.InvokeOnAction(actions, this);
        PerformAction(actionBuffers);

        int distance = TextDistance.CalculateLevenshtein(_currentQnA.Answer.ToLower(), AnswerText.text.ToLower());

        AddSupervisorReward(distance);
        CheckEndingConditions(distance);
    }

    public new void Enter()
    {
        //"show" the agent the mistakes he did while typing the answer
        _beliefWrittenAnswer = AnswerText.text;
        float reward = GetRewardAfterProofreading();
        SetReward(reward);

        base.Enter();
    }

    /// <summary>
    /// Heuristic is for testing purpose of the WHo model and the probability updates. Therefore, use the Typing agent for experiments with human 
    /// participants.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;

        Vector2 mouseVelocity = _mouseVelocity;

        continuousActionsOut[0] = mouseVelocity.x;
        continuousActionsOut[1] = mouseVelocity.y;
        
        //random testing value for sigma
        continuousActionsOut[2] = 10f;

        discreteActionsOut[0] = _mouseClicked;
        _mouseClicked = 0;
    }

    public override void Initialize()
    {
        _rand = new System.Random();
        (int, int) dimensions = PositionConverter.GetBinDimensions(KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);
        NumberOfBins = dimensions.Item1 * dimensions.Item2;
        _binOverlabCount = CountBinOverlapOccurrences();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        _stepCount = 0;
        _beliefFingerPositionStateSpace = FingerPositionStateSpace.Copy();
        _previousBeliefWrittenAnswer = _beliefWrittenAnswer = "";
        _previousMousePosition = Input.mousePosition;
        _unverifiedEntriesCount = 0;
        _maxDistanceBetweenButtons = FingerPositionStateSpace.GetMaxScreenDistanceBetweenVisualElements() * 1.2f;

        InitializeFingerLocationProbabilities();

        _mouseStartingPosition = Input.mousePosition;

        EpisodeStarted = true;
        _previousTextDistance = _currentQnA.Answer.Length;
        _lastPerformedAction = Vector2.zero;
        _previousNumberOfCorrectLetters = 0;
        _beliefRewardWithoutProof = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _beliefTarget = GetTarget(_currentQnA.Answer, _beliefWrittenAnswer);
        string buttomName = $"Button ({_beliefTarget.ToEscapedString()})";

        GameObject targetButton = FingerPositionStateSpace.GetGameObjectForName(buttomName);
        Vector2 distanceBetweenTargetAndFinger = GetVelocityInScreenSpace(GetObservableDistanceToFingerPositionInKeyboardCanvasSpace(targetButton));
        Vector2 normDistanceBetweenTargetAndFinger = new(NormalizeValue(distanceBetweenTargetAndFinger.x, -_maxDistanceBetweenButtons.x, _maxDistanceBetweenButtons.x),
                                                         NormalizeValue(distanceBetweenTargetAndFinger.y, -_maxDistanceBetweenButtons.y, _maxDistanceBetweenButtons.y));

        sensor.AddObservation(normDistanceBetweenTargetAndFinger); //same format as the finger velocity performed by the agent
        sensor.AddObservation(_lastPerformedAction);
        sensor.AddOneHotObservation((int)_beliefTarget, 128);
        sensor.AddOneHotObservation(GetButtonCharValue(GetBeliefFingerButton()), 128);
        sensor.AddObservation(GetBeliefFingerPositionKeyboardSpace());

        Debug.Log(string.Format("OBSERVATION: Finger Belief Position: {0} (Button: {1}), Target: {2}, Distance Between Target and Finger: {3} ({4}), Belief Written Answer: {5}", GetBeliefFingerPositionKeyboardSpace(), GetButtonCharValue(GetBeliefFingerButton()).ToEscapedString(), _beliefTarget, distanceBetweenTargetAndFinger, normDistanceBetweenTargetAndFinger, _beliefWrittenAnswer));
    }


    protected float GetBeliefReward()
    {
        float reward = 0;
        int numberOfCorrectLetters = GetNumberOfCorrectLetters(_beliefWrittenAnswer);

        if (_beliefClickedButton == _beliefTarget && _beliefClickedButton != '\x7F')
        {
            reward += 0.5f * (numberOfCorrectLetters + 1);
        }
        else if (_beliefClickedButton == _beliefTarget && _beliefClickedButton == '\x7F')
        {
            reward += 0.5f;
        }
        else if (_beliefClickedButton == '\x7F' && numberOfCorrectLetters < _previousNumberOfCorrectLetters)
        {
            reward += -0.5f * (_previousNumberOfCorrectLetters + 1);
        }
        else
        {
            reward += -0.5f;
        }

        _beliefRewardWithoutProof += reward;
        _previousNumberOfCorrectLetters = numberOfCorrectLetters;

        Debug.Log(string.Format("Belief Reward: {0}, Belief Clicked Button: {1}, Belief Target: {2}", reward, _beliefClickedButton.ToEscapedString(), _beliefTarget.ToEscapedString()));

        return reward;
    }

    protected virtual Vector2 GetFingerVelocity(ActionBuffers actionBuffers)
    {
        float pixelScale = FingerPositionStateSpace.Canvas.scaleFactor;

        if (IsMouseMode())
        {
            return new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
        }
        else
        {
            _lastPerformedAction = new Vector2(Mathf.Clamp(actionBuffers.ContinuousActions[0], -1, 1f), Mathf.Clamp(actionBuffers.ContinuousActions[1], -1, 1f));

            float xVelocity = ScaleAction(_lastPerformedAction.x, -_maxDistanceBetweenButtons.x, _maxDistanceBetweenButtons.x);
            float yVelocity = ScaleAction(_lastPerformedAction.y, -_maxDistanceBetweenButtons.y, _maxDistanceBetweenButtons.y);

            return new Vector2(xVelocity, yVelocity) / pixelScale;
        }
    }

    protected virtual float GetFingerSigma(ActionBuffers actionBuffers)
    {
        if (IsMouseMode())
        {
            return actionBuffers.ContinuousActions[2];
        }
        else
        {
            return Mathf.Clamp01(actionBuffers.ContinuousActions[2]);
        }
    }

    protected virtual bool IsMouseMode()
    {
        return gameObject.GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly;
    }

    protected Vector2 GetObservableDistanceToFingerPositionInKeyboardCanvasSpace(GameObject target)
    {
        Vector2 distanceBetweenTargetAndFinger = new Vector2(-999999, -999999);

        if (FocusStateSpace.IsActiveElement(target) || IsVisible)
        {
            distanceBetweenTargetAndFinger = GetScreenPositionInKeyboardCanvasSpace(FingerPositionStateSpace.GetScreenCoordinatesForGameObject(target)) - GetBeliefFingerPositionKeyboardSpace();
            if ((int)GetButtonCharValue(GetBeliefFingerButton()) == (int)_beliefTarget)
            {
                distanceBetweenTargetAndFinger = Vector2.zero;
            }
        }

        return distanceBetweenTargetAndFinger;
    }

    protected GameObject GetBeliefFingerButton()
    {
        GameObject buttonAtFingerLocation = FingerPositionStateSpace.GetGameObjectForScreenCoordinates(GetKeyboardCanvasSpaceInScreenPosition(GetBeliefFingerPositionKeyboardSpace()));

        return buttonAtFingerLocation;
    }

    protected int GetBeliefFingerBin()
    {
        double maxValue = _fingerLocationProbabilitiesBinSpace.Max();

        return _fingerLocationProbabilitiesBinSpace.ToList().IndexOf(maxValue);
    }

    protected Vector2 GetBeliefFingerPositionKeyboardSpace()
    {
        return PositionConverter.BinToRectangleCoordinates(GetBeliefFingerBin(), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);
    }

    protected bool IsTextEncoded()
    {
        GameObject textGameObject = FocusStateSpace.VisualElements.FirstOrDefault(a => a.name == "TextA");
        return FocusStateSpace.IsActiveElement(textGameObject) || IsVisible;
    }

    protected Vector2 GetScreenPositionInKeyboardCanvasSpace(Vector3 screenPosition)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            KeyboardRectTransform,
            screenPosition,
            FingerPositionStateSpace.Camera,
            out position
        );

        return position;
    }

    protected Vector2 GetKeyboardCanvasSpaceInScreenPosition(Vector3 keyboardCanvasPosition)
    {
        Vector3 worldPosition = KeyboardRectTransform.TransformPoint(keyboardCanvasPosition);

        Vector2 position = RectTransformUtility.WorldToScreenPoint(
            FingerPositionStateSpace.Camera,
            worldPosition
        );

        return position;
    }

    protected Vector2[] GetVelocitiesInKeyboardCanvasSpace(Vector2[] screenNorm)
    {
        Vector2[] keyboardNorm = new Vector2[screenNorm.Length];

        for (int i = 0; i < screenNorm.Length; i++)
        {
            keyboardNorm[i] = GetVelocityInKeyboardCanvasSpace(screenNorm[i]);
            //Assert.AreEqual(screenNorm[i], GetVelocityInScreenSpace(keyboardNorm[i]));
        }

        return keyboardNorm;
    }

    protected Vector2 GetVelocityInKeyboardCanvasSpace(Vector3 screenVelocity)
    {
        return GetScreenPositionInKeyboardCanvasSpace(screenVelocity) - GetScreenPositionInKeyboardCanvasSpace(Vector3.zero);
    }

    protected Vector2 GetVelocityInScreenSpace(Vector3 keyboardCanvasVelocity)
    {
        return GetKeyboardCanvasSpaceInScreenPosition(keyboardCanvasVelocity) - GetKeyboardCanvasSpaceInScreenPosition(Vector3.zero);
    }


    private void PerformAction(ActionBuffers actionBuffers)
    {
        Vector2 fingerVelocity = GetFingerVelocity(actionBuffers);
        float sigma = GetFingerSigma(actionBuffers);
        int actionType = actionBuffers.DiscreteActions[0];

        if (IsActive || IsAutonomous)
        {
            if (actionType == 0)
            {
                if(fingerVelocity != Vector2.zero)
                {
                    MoveFinger(fingerVelocity, sigma);
                    if (ShowFingerPosition) { ProjectImage(FingerPositionStateSpace, "Finger"); }
                }
            }
            else
            {
                ClickButton();
                if (!IsTextEncoded())
                {
                    _unverifiedEntriesCount = +1;
                }
                if (ShowFingerPosition) { ProjectImage(FingerPositionStateSpace, "FingerClick"); }

                float reward = GetBeliefReward();
                TaskRewardForFocusAgent.Enqueue(reward);
                AddReward(reward);
            }

            _stepCount += fingerVelocity != Vector2.zero || actionType == 1 ? 1 : 0;
        }
    }

    private void CheckEndingConditions(float distance)
    {
        if (distance > _currentQnA.Answer.Length + 50 || _stepCount > _currentQnA.Answer.Length * 25)
        {
            Debug.Log(string.Format("End of episode: Levenshtein distance: {0}, step count: {1}", distance, _stepCount));
            SetReward(GetFinalReward());

            if (IsTerminatingTask)
            {
                ITask.InvokeTermination();
            }
            else
            {
                EndEpisode();
            }
        }
    }

    private void AddSupervisorReward(float distance)
    {
        if (distance != _previousTextDistance)
        {
            TaskRewardForSupervisorAgent.Enqueue(_previousTextDistance - distance);
        }

        _previousTextDistance = distance;
    }

    private float GetRewardAfterProofreading()
    {
        float reward = TextDistance.GetRewardAfterProofreading(_beliefRewardWithoutProof, _previousBeliefWrittenAnswer.ToLower(), _beliefWrittenAnswer.ToLower(), _currentQnA.Answer.ToLower());

        if (reward == 0)
        {
            return 0;
        }

        Debug.Log(string.Format("PROOFREADING: Target: {0}, Correct Answer: {1}, Belief Written Answer: {2}, Last Pressed Button: {3}, Last Proofed Text: {4}, Belief Reward Without Proof: {5}, Proofreading Reward: {6}, Total Reward: {7}", _beliefTarget.ToEscapedString(), _currentQnA.Answer.ToLower(), _beliefWrittenAnswer, _beliefClickedButton.ToEscapedString(), _previousBeliefWrittenAnswer, _beliefRewardWithoutProof, reward, GetCumulativeReward() + reward));

        _previousBeliefWrittenAnswer = _beliefWrittenAnswer;
        _beliefRewardWithoutProof = 0;

        return reward;
    }

    private float NormalizeValue(float value, float minRange, float maxRange)
    {
        // Ensure value is within the min and max range to avoid unexpected results
        value = Mathf.Clamp(value, minRange, maxRange);

        // Normalize the value between -1 and 1
        return (value - minRange) / (maxRange - minRange) * 2f - 1f;
    }

    private void InitializeFingerLocationProbabilities()
    {
        _fingerLocationProbabilities = new double[FingerPositionStateSpace.VisualElements.Count];
        _fingerLocationProbabilitiesBinSpace = new double[NumberOfBins];

        for (int i = 0; i < FingerPositionStateSpace.VisualElements.Count; i++)
        {
            if (i == _startingButton)
            {
                _fingerScreenPosition = FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i);
                _fingerLocationProbabilities[i] = 1;
                _fingerLocationProbabilitiesBinSpace[PositionConverter.RectangleCoordinatesToBin(GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins)] = 1;
            }
            else
            {
                _fingerLocationProbabilities[i] = 0;
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && _mouseClicked != 1)
        {
            _mouseClicked = 1;
        }
    }

    private void FixedUpdate()
    {
        if(!EpisodeStarted)
        {
            return;
        }

        _movementTime = _movementTime == float.NaN ? 5 : _movementTime;

        if (_movementTimer >= _movementTime && !IsMouseMode())
        {
            RequestDecision();
            _movementTimer = 0.0f;
        }

        if ((IsMouseMode() && GetMouseMovement() == Vector2.zero && _mouseStartingPosition != (Vector2)Input.mousePosition) || _mouseClicked == 1)
        {
            _mouseVelocity = (Vector2)Input.mousePosition - _mouseStartingPosition;
            _mouseStartingPosition = Input.mousePosition;
            RequestDecision();
        }

        if (ShowBeliefState) 
        {
            ShowProbabilities();
        }

        _movementTimer += Time.fixedDeltaTime;

        UpdateBeliefWrittenAnswer();
        UpdateFingerPosition();

        if (IsTextEncoded())
        {
            float reward = GetRewardAfterProofreading();
            AddReward(reward);
            TaskRewardForFocusAgent.Enqueue(reward);
        }

        _timeOfEpisode += Time.fixedDeltaTime;
    }

    private void MoveFinger(Vector2 fingerScreenVelocity, float sigma)
    {
        Vector2 oldFingerScreenVelocity = fingerScreenVelocity;
        bool wasClipped = ClippScreenVelocity(ref fingerScreenVelocity);

        Vector2[] screenNormal = CRUtil.GetNormalDistributionForVelocity(NumberOfSamples, fingerScreenVelocity, sigma, _rand);

        if (wasClipped)
        {
            LogScreenLeftAllowedArea(oldFingerScreenVelocity);
            AddReward(-1);
        }

        //It must be guaranteed that the screen velocity is inside the normal distribution, otherwise it could be the case that the finger
        //location probability of the bin where the finger is located is 0 and an update in case of vision of this bin would not work.
        int pick = _rand.Next(0, screenNormal.Length);
        fingerScreenVelocity = screenNormal[pick];
        ClippScreenVelocity(ref fingerScreenVelocity);
        screenNormal[pick] = fingerScreenVelocity;

        Vector2[] keyboardNormal = GetVelocitiesInKeyboardCanvasSpace(screenNormal);

        float distance = CRUtil.PixelToCM(fingerScreenVelocity, ScreenWidthPixel, ScreenHightPixel, ScreenDiagonalInch).magnitude * FingerPositionStateSpace.Canvas.scaleFactor; ;
        _movementTime = GetWHoMovementTime(distance, sigma);

        if (_movementTime > 1.5 || _movementTime.Equals(float.NaN))
        {
            Debug.Log(string.Format("Performed unrealistic behavior: movement time of {0}.", _movementTime));
            _movementTime = 0;
            AddReward(-1);
            return;
        }

        LogMovement(fingerScreenVelocity, oldFingerScreenVelocity, sigma);

        UpdateFingerPosition(fingerScreenVelocity, keyboardNormal);
    }

    private void LogMovement(Vector2 fingerScreenVelocity, Vector2 oldFingerScreenVelocity, float sigma)
    {
        if (oldFingerScreenVelocity == Vector2.zero)
        {
            return;
        }

        Vector2 fingerKeyboardPosition = GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition);
        Vector2 fingerKeyboardVelocity = GetVelocityInKeyboardCanvasSpace(fingerScreenVelocity);

        float distance = CRUtil.PixelToCM(fingerScreenVelocity, ScreenWidthPixel, ScreenHightPixel, ScreenDiagonalInch).magnitude * FingerPositionStateSpace.Canvas.scaleFactor; ;

        Debug.Log(string.Format("ACTION: Move finger with velocity {0} ({1}) over a distance of {2} cm and sigma {3} in {4} seconds.", fingerScreenVelocity, fingerKeyboardVelocity, distance, sigma, _movementTime));
    }

    private bool ClippScreenVelocity( ref Vector2 fingerScreenVelocity)
    {
        Vector2 fingerKeyboardPosition = GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition);
        Vector2 fingerKeyboardVelocity = GetVelocityInKeyboardCanvasSpace(fingerScreenVelocity);

        Vector2 clippedFingerKeyboardVelocity = PositionConverter.GetClippedVelocity(KeyboardRectTransform.rect, fingerKeyboardPosition, fingerKeyboardVelocity, 10);
        fingerScreenVelocity = GetVelocityInScreenSpace(clippedFingerKeyboardVelocity);

        return fingerKeyboardVelocity != clippedFingerKeyboardVelocity;
    }

    private void LogScreenLeftAllowedArea(Vector2 fingerScreenVelocity)
    {
        Vector2 fingerKeyboardPosition = GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition);
        Vector2 fingerKeyboardVelocity = GetVelocityInKeyboardCanvasSpace(fingerScreenVelocity);

        Vector2 clippedFingerKeyboardVelocity = PositionConverter.GetClippedVelocity(KeyboardRectTransform.rect, fingerKeyboardPosition, fingerKeyboardVelocity, 10);
        Vector2 clippedFingerScreenVelocity = GetVelocityInScreenSpace(clippedFingerKeyboardVelocity);

        Debug.Log(string.Format("Finger left allowed area: {0} (screen space: {1}). Reset finger position to {2} (screen space: {3}).", fingerKeyboardPosition + fingerKeyboardVelocity, _fingerScreenPosition + fingerScreenVelocity, fingerKeyboardPosition + clippedFingerKeyboardVelocity, _fingerScreenPosition + clippedFingerScreenVelocity));
    }

    private void UpdateFingerPosition(Vector2? fingerVelocity = null, Vector2[] normal = null)
    {
        int fingerScreenPositionBin = PositionConverter.RectangleCoordinatesToBin(GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);
        Vector2 fingerScreenPositionNew = fingerVelocity != null ? TransformScreenCoordinateToBinCenter(_fingerScreenPosition) + fingerVelocity.Value : _fingerScreenPosition;
        int fingerScreenPositionNewBin = PositionConverter.RectangleCoordinatesToBin(GetScreenPositionInKeyboardCanvasSpace(fingerScreenPositionNew), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);

        //Debug.Log(string.Format("Move finger from position {0} (Bin: {1}, Prob: {2}) to {3} (Bin: {4}) {5}", GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition), fingerScreenPositionBin, _fingerLocationProbabilitiesBinSpace[fingerScreenPositionBin], GetScreenPositionInKeyboardCanvasSpace(fingerScreenPositionNew), fingerScreenPositionNewBin, this.GetHashCode()));
        _fingerScreenPosition = fingerScreenPositionNew;

        int maxIndex = _fingerLocationProbabilitiesBinSpace.ToList().IndexOf(_fingerLocationProbabilities.Max());
        Vector2 maxPosition = PositionConverter.BinToRectangleCoordinates(maxIndex, KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);

        GameObject buttonAtFingerLocation = FingerPositionStateSpace.GetGameObjectForScreenCoordinates(_fingerScreenPosition);
        FingerPositionStateSpace.ActivateSingleElement(buttonAtFingerLocation);

        UpdateBeliefState(normal);
    }

    private Vector2 TransformScreenCoordinateToBinCenter(Vector2 screenPosition)
    {
        Vector2 keyboardPosition = GetScreenPositionInKeyboardCanvasSpace(screenPosition);
        Vector2 centeredKeyboardPosition = PositionConverter.RectangleCoordinatesToBinCenter(keyboardPosition, KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);

        return GetKeyboardCanvasSpaceInScreenPosition(centeredKeyboardPosition);
    }

    private void UpdateBeliefState(Vector2[] normal = null)
    {
        UpdateFingerPositionProbabilities(normal);
        UpdateOneHotEncodingBeliefState();
    }

    private void UpdateFingerPositionProbabilities(Vector2[] normal = null)
    {
        double[] currentFingerLocationProbabilitiesBinSpace = (double[])_fingerLocationProbabilitiesBinSpace.Clone();

        normal ??= new Vector2[1];

        NativeArray<Vector2> normalNative = new NativeArray<Vector2>(normal, Allocator.TempJob);
        NativeArray<double> currentFingerLocationProbabilitiesBinSpaceNative = new NativeArray<double>(currentFingerLocationProbabilitiesBinSpace, Allocator.TempJob);
        NativeArray<double> fingerLocationProbabilitiesBinSpaceNative = new NativeArray<double>(_fingerLocationProbabilitiesBinSpace, Allocator.TempJob);

        List<GameObjectPosition> gameObjectPositions = new();
        gameObjectPositions.AddRange(FingerPositionStateSpace.VisualElements.Where(a => a.activeInHierarchy).Select(a => CRUtil.ConvertToGameObjectPosition(a, FingerPositionStateSpace.Camera)).ToArray());
        NativeArray<GameObjectPosition> gameObjectPositionsNative = new NativeArray<GameObjectPosition>(gameObjectPositions.ToArray(), Allocator.TempJob);

        bool isFocusOnFinger = FocusStateSpace.IsActiveElement(_fingerScreenPosition, 10);

        ObjectIn2DRectangleLocationProbabilitiesUpdateJob fingerLocationProbabilitiesUpdateJob = new ObjectIn2DRectangleLocationProbabilitiesUpdateJob
        {
            NormalDistributionForVelocity = normalNative,
            CurrentObjectLocationProbabilities = currentFingerLocationProbabilitiesBinSpaceNative,
            ObjectLocationProbabilities = fingerLocationProbabilitiesBinSpaceNative,
            ObjectPosition = GetScreenPositionInKeyboardCanvasSpace(_fingerScreenPosition),
            IsVisibleInstance = isFocusOnFinger || IsVisible,
            RectangleWidth = KeyboardRectTransform.rect.width,
            RectangleHight = KeyboardRectTransform.rect.height,
            NumberOFBins = NumberOfBins,
            ObservationProbability = ObservationProbability,
            ConsiderEdgeBins = true
        };

        JobHandle jobHandle = fingerLocationProbabilitiesUpdateJob.Schedule(NumberOfBins, 16);
        jobHandle.Complete();

        //direct assignment of the array would overwrite the reference to the original array (e.g. reference of subclass)
        Array.Copy(fingerLocationProbabilitiesBinSpaceNative.ToArray(), _fingerLocationProbabilitiesBinSpace, _fingerLocationProbabilitiesBinSpace.Length);

        //Normalize the updated belief b`(s_)
        double sum = _fingerLocationProbabilitiesBinSpace.Sum();

        if(sum != 0)
        {
            for (int p = 0; p < _fingerLocationProbabilitiesBinSpace.Length; p++)
            {
                _fingerLocationProbabilitiesBinSpace[p] = _fingerLocationProbabilitiesBinSpace[p] / sum;
            }
        }else
        {
            Debug.LogWarning("Sum of belief states is zero.");
        }

        normalNative.Dispose();
        currentFingerLocationProbabilitiesBinSpaceNative.Dispose();
        fingerLocationProbabilitiesBinSpaceNative.Dispose();
        gameObjectPositionsNative.Dispose();

        TransferFingerLocationProbabilitiesFromBinSpaceToVisualSpace();
    }

    private void ClickButton()
    {
        if (IsButtonVisible(GetTarget(_currentQnA.Answer, _beliefWrittenAnswer)))
        {
            _beliefClickedButton = GetButtonCharValue(GetBeliefFingerButton());
            UpdateBeliefWrittenAnswer(_beliefClickedButton.ToEscapedString());
        }

        if (FingerPositionStateSpace.GetFirstActiveElement() != null)
        {
            FingerPositionStateSpace.GetFirstActiveElement().GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            _movementTime = 0.1f;
        }

        Debug.Log(string.Format("ACTION: Click on {0} button", _beliefClickedButton.ToEscapedString()));
    }

    private float GetWHoMovementTime(float distance, float sigma)
    {
        float x0 = 0.092f;
        float y0 = 0.0018f;
        float alpha = 0.6f;
        float kAlpha = 0.12f;

        if (distance == 0)
        {
            return 0;
        }

        if (sigma < 0.01f)
        {
            sigma = 0.01f;
        }

        return (float)(x0 + Math.Pow(kAlpha / Math.Pow(sigma / distance - y0, 1 - alpha), 1 / alpha));
    }

    private void UpdateOneHotEncodingBeliefState()
    {
        double maxValue = _fingerLocationProbabilities.Max();
        int maxIndex = _fingerLocationProbabilities.ToList().IndexOf(maxValue);

        _beliefFingerPositionStateSpace.ActivateSingleElement(maxIndex);
    }

    private void UpdateBeliefWrittenAnswer(string typedText = null)
    {
        if (typedText != null)
        {
            if (_beliefWrittenAnswer.Length > 0 && typedText == "\\x7F")
            {
                _beliefWrittenAnswer = _beliefWrittenAnswer[..^1];
            }
            else if (typedText != "\\x7F")
            {
                _beliefWrittenAnswer += typedText;
            }
        }
        else if (IsTextEncoded())
        {
            _beliefWrittenAnswer = AnswerText.text;
        }
    }

    private void ShowProbabilities()
    {
        if(_beliefFingerPositionStateSpace != null)
        {
            for (int i = 0; i < _beliefFingerPositionStateSpace.VisualElements.Count; i++)
            {
                GameObject button = _beliefFingerPositionStateSpace.VisualElements[i];
                TextMeshProUGUI text = button.transform.GetChildInHierarchyByName("ProbabilityText").GetComponent<TextMeshProUGUI>();

                text.text = string.Format("{0:N2}%", _fingerLocationProbabilities[i] * 100);

                if (_fingerLocationProbabilities[i] == _fingerLocationProbabilities.Max())
                {
                    text.color = Color.red;
                }
                else
                {
                    Color color = Color.white;
                    color.a = _fingerLocationProbabilities[i] != 0 ? (float)_fingerLocationProbabilities[i] * 2f + 0.2f : 0;
                    text.color = color;
                }
            }
        }
    }

    private Vector2 GetMouseMovement()
    {
        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 delta = currentMousePosition - _previousMousePosition;
        _previousMousePosition = currentMousePosition;

        return delta;
    }

    private void TransferFingerLocationProbabilitiesFromBinSpaceToVisualSpace()
    {
        _fingerLocationProbabilities = new double[FingerPositionStateSpace.VisualElements.Count];

        for (int i = 0; i < FingerPositionStateSpace.VisualElements.Count; i++)
        {
            Vector2 rectCenter = GetScreenPositionInKeyboardCanvasSpace(FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i));
            GameObject button = FingerPositionStateSpace.VisualElements[i];
            Vector2 rectSize = button.GetComponent<RectTransform>().rect.size;
            List<int> binsInsideButton = PositionConverter.GetBinsInsideRect(rectCenter, rectSize, KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);

            //foreach (int bin in binsInsideButton.FindAll(x => _fingerLocationProbabilitiesBinSpace[x] != 0)) //TODO: test if I work and if I am faster
            foreach (int bin in binsInsideButton)
            {
                _fingerLocationProbabilities[i] += _fingerLocationProbabilitiesBinSpace[bin] / _binOverlabCount[bin];
            }
        }
    }

    private int[] CountBinOverlapOccurrences()
    {
        int[] binCount = new int[NumberOfBins];

        for (int i = 0; i < FingerPositionStateSpace.VisualElements.Count; i++)
        {
            Vector2 rectCenter = GetScreenPositionInKeyboardCanvasSpace(FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i));
            GameObject button = FingerPositionStateSpace.VisualElements[i];
            Vector2 rectSize = button.GetComponent<RectTransform>().rect.size;
            List<int> binsInsideButton = PositionConverter.GetBinsInsideRect(rectCenter, rectSize, KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins);

            foreach (int bin in binsInsideButton)
            {
                binCount[bin] += 1;
            }
        }

        return binCount;
    }
}