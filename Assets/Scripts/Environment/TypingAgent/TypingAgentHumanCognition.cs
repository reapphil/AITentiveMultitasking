using Algorithms;
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
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using static ObjectIn2DGridProbabilitiesUpdateJob;


public class TypingAgentHumanCognition : TypingAgent, ICrTask
{
    [field: SerializeField]
    public VisualStateSpace FocusStateSpace { get; set; }

    [field: SerializeField, Tooltip("Visualizes the current belief finger position."), ProjectAssign]
    public bool ShowBeliefState { get; set; }

    [field: SerializeField, Tooltip("Observation is active for this agent independent of the focus/supervisor agent."), ProjectAssign]
    public bool FullVision { get; set; } = false;

    public bool UseFocusAgent { get => true; set {} }

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


    private System.Random _rand;

    private float _movementTime;

    private float _movementTimer = 0.0f;

    private double[] _fingerLocationProbabilities;

    private double[] _fingerLocationProbabilitiesBinSpace;

    private int[] _binOverlabCount;

    private VisualStateSpace _beliefFingerPositionStateSpace;

    private string _beliefWrittenAnswer;

    private int _mouseClicked;

    private Vector2 _previousMousePosition;

    private Vector2 _fingerScreenPosition;

    private int _unverifiedEntriesCount;

    private Vector2 _maxDistanceBetweenButtons;

    private char _beliefTarget;

    private char _beliefClickedButton;

    private int _initDistance;

    private Vector2 _mouseVelocity;

    private Vector2 _mouseStartingPosition;

    private bool EpisodeStarted = false;


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
        ITask.InvokeOnAction(actionBuffers, this);

        Vector2 fingerVelocity = GetFingerVelocity(actionBuffers);
        float sigma = GetFingerSigma(actionBuffers);
        int actionType = actionBuffers.DiscreteActions[0];

        //Debug.Log(string.Format("Finger Velocity: {0}, Sigma: {1}, Action Type: {2}", fingerVelocity, sigma, actionType));

        if (IsActive || IsAutonomous)
        {
            if (actionType == 0)
            {
                MoveFinger(fingerVelocity, sigma);
                if (ShowFingerPosition) { ProjectImage(FingerPositionStateSpace, "Finger"); }
            }
            else
            {
                ClickButton();
                if (!IsTextEncoded())
                {
                    _unverifiedEntriesCount =+ 1;
                }
                if (ShowFingerPosition) { ProjectImage(FingerPositionStateSpace, "FingerClick"); }

                float reward = GetReward();

                TaskRewardForFocusAgent.Enqueue(reward);
                TaskRewardForSupervisorAgent.Enqueue(reward);
                SetReward(reward);
            }
        }

        if (LevenshteinDistance.Calculate(_currentQnA.Answer.ToLower(), AnswerText.text.ToLower()) > _initDistance + 50)
        {
            Debug.Log("End of episode: Levenshtein distance is to high.");
            SetReward(-50);

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
        continuousActionsOut[2] = 3f;

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

        _beliefFingerPositionStateSpace = FingerPositionStateSpace.Copy();
        _beliefWrittenAnswer = "";
        _previousMousePosition = Input.mousePosition;
        _unverifiedEntriesCount = 0;
        _maxDistanceBetweenButtons = FingerPositionStateSpace.GetMaxScreenDistanceBetweenVisualElements();
        _initDistance = LevenshteinDistance.Calculate(_currentQnA.Answer.ToLower(), AnswerText.text.ToLower());

        InitializeFingerLocationProbabilities();

        _mouseStartingPosition = Input.mousePosition;

        EpisodeStarted = true;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _beliefTarget = GetTarget(_currentQnA.Answer, _beliefWrittenAnswer);

        sensor.AddOneHotObservation((int)GetButtonCharValue(GetBeliefFingerButton()), 128);
        sensor.AddOneHotObservation((int)_beliefTarget, 128);
        sensor.AddObservation(GetBeliefFingerPosition());

        //Debug.Log(string.Format("Current Value: {0} ({1}); Target Value: {2} ({3}); Belief Finger Position: {4}", GetButtonCharValue(GetBeliefFingerButton()), (int)GetButtonCharValue(GetBeliefFingerButton()), _beliefTarget, (int)_beliefTarget, GetBeliefFingerPosition()));
    }

    //Reward based on belief state.
    protected new float GetReward()
    {
        int reward;

        //Debug.Log(string.Format("Target: {0}, Current Answer: {1}, Belief Written Answer: {2}, Last Pressed Button: {3}: ", _beliefTarget, _currentQnA.Answer.ToLower(), _beliefWrittenAnswer.ToLower(), _beliefClickedButton));

        if (_beliefClickedButton == _beliefTarget)
        {
            reward = 1;
        }
        else
        {
            reward = -1;
        }

        return reward;
    }


    private Vector2 GetFingerVelocity(ActionBuffers actionBuffers)
    {
        if (IsHeuristicMode())
        {
            return new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
        }
        else
        {
            float xVelocity = ScaleAction(Mathf.Clamp(actionBuffers.ContinuousActions[0], -1, 1f), -_maxDistanceBetweenButtons.x, _maxDistanceBetweenButtons.x);
            float yVelocity = ScaleAction(Mathf.Clamp(actionBuffers.ContinuousActions[1], -1, 1f), -_maxDistanceBetweenButtons.y, _maxDistanceBetweenButtons.y);

            return new Vector2(xVelocity, yVelocity);
        }
    }

    private float GetFingerSigma(ActionBuffers actionBuffers)
    {
        if (IsHeuristicMode())
        {
            return actionBuffers.ContinuousActions[2];
        }
        else
        {
            return Mathf.Clamp01(actionBuffers.ContinuousActions[2]);
        }
    }

    private void InitializeFingerLocationProbabilities()
    {
        _fingerLocationProbabilities = new double[FingerPositionStateSpace.VisualElements.Count];
        _fingerLocationProbabilitiesBinSpace = new double[NumberOfBins];

        for (int i = 0; i < FingerPositionStateSpace.VisualElements.Count; i++)
        {
            if (i == 30) //Enter button
            {
                _fingerScreenPosition = FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i);
                _fingerLocationProbabilities[i] = 1;
                _fingerLocationProbabilitiesBinSpace[PositionConverter.RectangleCoordinatesToBin(GetPositionInKeyboardCanvasSpace(_fingerScreenPosition), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins)] = 1;
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

        if (_movementTimer >= _movementTime && !IsHeuristicMode())
        {
            RequestDecision();
            _movementTimer = 0.0f;
        }

        if (IsHeuristicMode() && GetMouseMovement() == Vector2.zero)
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
    }

    private void MoveFinger(Vector2 fingerScreenVelocity, float sigma)
    {
        Vector2 fingerKeyboardPosition = GetPositionInKeyboardCanvasSpace(_fingerScreenPosition);
        Vector2 fingerKeyboardVelocity = GetVelocityInKeyboardCanvasSpace(_fingerScreenPosition, fingerScreenVelocity);

        Vector2 clippedFingerKeyboardVelocity = PositionConverter.GetClippedVelocity(KeyboardRectTransform.rect, fingerKeyboardPosition, fingerKeyboardVelocity);
        Vector2 clippedFingerScreenVelocity = GetVelocityInScreenSpace(fingerKeyboardPosition + clippedFingerKeyboardVelocity, fingerKeyboardPosition);
        Vector2[] screenNormal = CRUtil.GetNormalDistributionForVelocity(NumberOfSamples, clippedFingerScreenVelocity, sigma, _rand);
        Vector2[] keyboardNormal = GetVelocitiesInKeyboardCanvasSpace(_fingerScreenPosition, screenNormal);

        if (fingerKeyboardVelocity != clippedFingerKeyboardVelocity)
        {
            Debug.Log(string.Format("Finger left allowed area: {0} (screen space: {1}). Reset finger position to {2} (screen space: {3}).", fingerKeyboardPosition + fingerKeyboardVelocity, _fingerScreenPosition + fingerScreenVelocity, fingerKeyboardPosition + clippedFingerKeyboardVelocity, _fingerScreenPosition + clippedFingerScreenVelocity));
            SetReward(-1);
        }
        else
        {
            clippedFingerScreenVelocity = IsHeuristicMode() ? clippedFingerScreenVelocity : screenNormal[_rand.Next(0, screenNormal.Length)];
        }

        float distance = CRUtil.PixelToCM(clippedFingerScreenVelocity, ScreenWidthPixel, ScreenHightPixel, ScreenDiagonalInch).magnitude;
        _movementTime = GetWHoMovementTime(distance, sigma);

        if (_movementTime > 3 || _movementTime.Equals(float.NaN))
        {
            Debug.Log(string.Format("Performed unrealistic behavior: movement time of {0}.", _movementTime));
            _movementTime = 0;
            SetReward(-1);
            return;
        }

        UpdateFingerPosition(clippedFingerScreenVelocity, keyboardNormal);
    }

    private void UpdateFingerPosition(Vector2? fingerVelocity = null, Vector2[] normal = null)
    {
        _fingerScreenPosition = fingerVelocity != null ? _fingerScreenPosition + fingerVelocity.Value : _fingerScreenPosition;

        GameObject buttonAtFingerLocation = FingerPositionStateSpace.GetGameObjectForScreenCoordinates(_fingerScreenPosition);
        FingerPositionStateSpace.ActivateSingleElement(buttonAtFingerLocation);

        UpdateBeliefState(normal);
    }

    private void UpdateBeliefState(Vector2[] normal = null)
    {
        UpdateFingerPositionProbabilities(normal);
        UpdateOneHotEncodingBeliefState();
    }

    private void UpdateFingerPositionProbabilities(Vector2[] normal = null)
    {
        double[] currentFingerLocationProbabilitiesBinSpace = (double[])_fingerLocationProbabilitiesBinSpace.Clone();

        normal ??= new Vector2[NumberOfSamples];

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
            ObjectPosition = GetPositionInKeyboardCanvasSpace(_fingerScreenPosition),
            IsVisibleInstance = isFocusOnFinger || IsVisible,
            RectangleWidth = KeyboardRectTransform.rect.width,
            RectangleHight = KeyboardRectTransform.rect.height,
            NumberOFBins = NumberOfBins,
            ObservationProbability = ObservationProbability
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
            UpdateBeliefWrittenAnswer(_beliefClickedButton.ToString());
        }

        if (FingerPositionStateSpace.GetFirstActiveElement() != null)
        {
            FingerPositionStateSpace.GetFirstActiveElement().GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            _movementTime = 0.1f;
        }
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
            _beliefWrittenAnswer += typedText;
        }

        if (IsTextEncoded())
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

    private GameObject GetBeliefFingerButton() 
    {
        GameObject buttonAtFingerLocation = FingerPositionStateSpace.GetGameObjectForScreenCoordinates(GetBeliefFingerPosition());

        return buttonAtFingerLocation;
    }

    private int GetBeliefFingerBin()
    {
        double maxValue = _fingerLocationProbabilitiesBinSpace.Max();

        return _fingerLocationProbabilitiesBinSpace.ToList().IndexOf(maxValue);
    }

    private Vector2 GetBeliefFingerPosition()
    {
        double maxValue = _fingerLocationProbabilitiesBinSpace.Max();

        return GetPositionInScreenSpace(PositionConverter.BinToRectangleCoordinates(GetBeliefFingerBin(), KeyboardRectTransform.rect.width, KeyboardRectTransform.rect.height, NumberOfBins));
    }

    private bool IsTextEncoded()
    {
        GameObject textGameObject = FocusStateSpace.VisualElements.FirstOrDefault(a => a.name == "TextA");
        return FocusStateSpace.IsActiveElement(textGameObject) || IsVisible;
    }

    private bool IsHeuristicMode()
    {
        return gameObject.GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly;
    }

    private Vector2 GetMouseMovement()
    {
        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 delta = currentMousePosition - _previousMousePosition;
        _previousMousePosition = currentMousePosition;

        return delta;
    }

    private Vector2 GetPositionInKeyboardCanvasSpace(Vector3 screenPosition)
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

    private Vector2[] GetVelocitiesInKeyboardCanvasSpace(Vector3 screenPosition, Vector2[] screenNorm)
    {
        Vector2[] keyboardPosition = new Vector2[screenNorm.Length];

        for (int i = 0; i < screenNorm.Length; i++)
        {
            keyboardPosition[i] = GetVelocityInKeyboardCanvasSpace(screenPosition, screenNorm[i]);
        }

        return keyboardPosition;
    }

    private Vector2 GetVelocityInKeyboardCanvasSpace(Vector3 screenPosition, Vector3 screenVelocity)
    {
        Vector3 keyboardPosition = GetPositionInKeyboardCanvasSpace(screenPosition);
        Vector3 newKeyboardPosition = GetPositionInKeyboardCanvasSpace(screenPosition + screenVelocity);

        return newKeyboardPosition - keyboardPosition;
    }

    private Vector2 GetPositionInScreenSpace(Vector3 keyboardCanvasPosition)
    {
        Vector3 worldPosition = KeyboardRectTransform.TransformPoint(keyboardCanvasPosition);

        return VisualStateSpace.GetScreenPosition(FingerPositionStateSpace.Camera, worldPosition);
    }

    private Vector2 GetVelocityInScreenSpace(Vector3 newKeyboardCanvasPosition, Vector3 oldKeyboardCanvasPosition)
    {
        return GetPositionInScreenSpace(newKeyboardCanvasPosition) - GetPositionInScreenSpace(oldKeyboardCanvasPosition);
    }

    private void TransferFingerLocationProbabilitiesFromBinSpaceToVisualSpace()
    {
        _fingerLocationProbabilities = new double[FingerPositionStateSpace.VisualElements.Count];

        for (int i = 0; i < FingerPositionStateSpace.VisualElements.Count; i++)
        {
            Vector2 rectCenter = GetPositionInKeyboardCanvasSpace(FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i));
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
            Vector2 rectCenter = GetPositionInKeyboardCanvasSpace(FingerPositionStateSpace.GetScreenCoordinatesForGameObjectIndex(i));
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