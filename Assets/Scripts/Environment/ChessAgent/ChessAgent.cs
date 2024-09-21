using NSubstitute.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ChessAgent : Agent, ITask
{
    public bool IsActive { get; set; }
    public bool IsFocused { get; set; }

    [field: SerializeField, ProjectAssign]
    public bool IsAutonomous { get; set; }

    [field: SerializeField, Tooltip("Determines if the supervisor should end its episode if the episode of the task ends"), ProjectAssign]
    public bool IsTerminatingTask { get; set; } = false;

    [field: SerializeField, ProjectAssign]
    public int DecisionPeriod { get; set; }

    [field: SerializeField]
    public BoardManager BoardManager { get; set; }

    public IStateInformation StateInformation {
        get
        {
            _chessStateInformation ??= new ChessStateInformation();

            return _chessStateInformation;
        }
        set
        {
            _chessStateInformation = value as ChessStateInformation;
        }
    }

    public Dictionary<string, double> Performance => new();

    public Queue<float> TaskRewardForSupervisorAgent { get; private set; }

    public Queue<float> TaskRewardForFocusAgent { get; private set; }


    private List<(List<ChessFigurePosition>, string)> _puzzle;

    private Vector2Int _selectedField;

    private bool _isNewSelection;

    private ChessStateInformation _chessStateInformation;

    public void OnMove(InputValue value)
    {
        //Not necessary since mouse input is used.
    }

    public void AddTrueObservationsToSensor(VectorSensor sensor)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessFigure figure = BoardManager.ChessFigurePositions[x, y];
                sensor.AddOneHotObservation(ChessFigureToIndex(figure), 12);
            }
        }

        sensor.AddObservation(PositionConverter.DiscreteVectorToBin(BoardManager.SelectedField, 8));
    }

    public void AddPerceivedObservationsToSensor(VectorSensor sensor)
    {
        AddTrueObservationsToSensor(sensor);
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void UpdateDifficultyLevel()
    {
        //throw new System.NotImplementedException();
    }

    public override void Initialize()
    {
        TaskRewardForSupervisorAgent = new();
        TaskRewardForFocusAgent = new();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessFigure figure = BoardManager.ChessFigurePositions[x, y];
                sensor.AddOneHotObservation(ChessFigureToIndex(figure), 12);
            }
        }   

        sensor.AddObservation(PositionConverter.DiscreteVectorToBin(BoardManager.SelectedField, 8));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (_isNewSelection)
        {
            discreteActionsOut[0] = _selectedField.x;
            discreteActionsOut[1] = _selectedField.y;
            _isNewSelection = false;
        }
        else
        {
            discreteActionsOut[0] = -1;
            discreteActionsOut[1] = -1;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        ITask.InvokeOnAction(actionBuffers, this);

        int targetX = actionBuffers.DiscreteActions[0];
        int targetY = actionBuffers.DiscreteActions[1];

        bool? isCheckMade = null;

        if (targetX >= 0 && targetY >= 0)
        {
            isCheckMade = BoardManager.HandleInput(targetX, targetY);
        }
        
        if (isCheckMade.HasValue)
        {
            if (isCheckMade.Value)
            {
                SetReward(1f);
                BoardManager.ShowCorrectImageAnimation();
            }
            else
            {
                SetReward(-1f);
                BoardManager.ShowWrongImageAnimation();
            }

            BoardManager.EndGame();
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        InitBoardManagerWithLichessMateIn1Format();
    }

    public void ResetPerformance() { }


    private int ChessFigureToIndex(ChessFigure chessFigure)
    {
        if (chessFigure == null)
        {
            return 0;
        }

        string type = chessFigure.GetType().Name;
        int i = chessFigure.IsWhite ? 0 : 6;

        return type switch
        {
            "King" => i + 1,
            "Queen" => i + 2,
            "Rook" => i + 3,
            "Bishop" => i + 4,
            "Knight" => i + 5,
            "Pawn" => i + 6,
            _ => -1,
        };
    }

    private void InitBoardManagerWithLichessMateIn1Format()
    {
        var random = new System.Random();
        int index = random.Next(_puzzle.Count);

        BoardManager.ChessBoard = _puzzle[index].Item1;

        BoardManager.Start();

        //only perform the fist move according to the outstanding move of the lichess mateIn1 puzzle (see https://database.lichess.org/#puzzles)
        BoardManager.IsWhiteTurn = false;
        BoardManager.MoveChessFigure(_puzzle[index].Item2.Split(' ')[0]);
    }

    private void Start()
    {
        _puzzle = FENLoader.LoadChessBoardsInclusiveMovesFromLichess(Path.Combine(Application.dataPath, "Scripts", "Environment", "ChessAgent", "mateInOne.csv"));
    }

    // Update is called once per frame
    private void Update()
    {
        if (IsActive)
        {
            HandleInput();
            RequestDecision();
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _selectedField = BoardManager.SelectedField;
            _isNewSelection = true;
        }
    }
}
