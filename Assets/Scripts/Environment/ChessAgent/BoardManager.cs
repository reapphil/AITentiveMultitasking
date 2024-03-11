using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BoardManager : MonoBehaviour
{
    [field: SerializeField]
    public List<GameObject> ChessFiguresGameObjects { get; set; }

    [field: SerializeField]
    public BoardHighlighting BoardHighlighting { get; set; }

    public ChessFigure[,] ChessFigurePositions { get; set; }

    public List<GameObject> ActiveFigures { get; set; }

    public ChessFigure SelectedFigure { get; set; }

    public Vector2Int SelectedField { get; set; }

    public bool IsWhiteTurn { get; set; }

    public List<ChessFigurePosition> ChessBoard;


    private bool[,] _allowedMoves;

    private const float TILE_SIZE = 1.0f;

    private const float TILE_OFFSET = 0.5f;

    private Dictionary<string, GameObject> _chessFiguresGameObjects;

    private ImageAnimation _correctImageAnimation;

    private ImageAnimation _wrongImageAnimation;


    public bool? HandleInput(int targetX, int targetY)
    {
        if (targetX >= 0 && targetY >= 0)
        {
            if (SelectedFigure == null)
            {
                // Select Figure
                SelectChessFigure(targetX, targetY);
            }
            else
            {
                // Move Figure
                return MoveChessFigure(targetX, targetY);
            }
        }

        return null;
    }

    public void EndGame()
    {
        if (IsWhiteTurn)
        {
            Debug.Log("White team won!");
        }
        else
        {
            Debug.Log("Black team won!");
        }

        SpawnChessBoard();
    }

    public void ShowCorrectImageAnimation()
    {
        _correctImageAnimation.FadeInOut();
    }

    public void ShowWrongImageAnimation()
    {
        _wrongImageAnimation.FadeInOut();
    }

    public void MoveChessFigure(string move)
    {
        int sourceX = move[0] - 'a';
        int sourceY = (int)char.GetNumericValue(move[1])-1;

        int targetX = move[2] - 'a';
        int targetY = (int)char.GetNumericValue(move[3])-1;

        Debug.Log(string.Format("Move from {0}, {1} to {2}, {3}", sourceX, sourceY, targetX, targetY));

        SelectChessFigure(sourceX, sourceY);
        MoveChessFigure(targetX, targetY);
    }

    // Start is called before the first frame update
    public void Start()
    {
        IsWhiteTurn = true;
        ChessFigurePositions = new ChessFigure[8, 8];
        ActiveFigures ??= new List<GameObject>();

        if (ChessBoard == null)
        {
            ChessBoard = FENLoader.LoadChessBoardsFromFEN(Path.Combine(Application.dataPath, "Scripts", "Environment", "ChessAgent", "regular.fen"))[0];
        }

        _correctImageAnimation = transform.GetChildByName("Canvas").GetChildByName("Correct").GetComponent<ImageAnimation>();
        _wrongImageAnimation = transform.GetChildByName("Canvas").GetChildByName("Wrong").GetComponent<ImageAnimation>();

        InstantiateChessFiguresDict();
        SpawnChessBoard();
    }


    // Update is called once per frame
    private void Update()
    {
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        GameObject cameraGameObject = gameObject.transform.GetChildByName("Camera").gameObject;
        Camera camera = cameraGameObject.GetComponent<Camera>();

        RaycastHit hit;
        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit))
        {
            SelectedField = GetSelection(hit);
        }
        else
        {
            SelectedField = new Vector2Int(-1, -1);
        }
    }

    private Vector2Int GetSelection(RaycastHit hit)
    {
        //Debug.Log(string.Format("_selectionX: {0}; _selectionY: {1};", transform.InverseTransformPoint(hit.point).x, transform.InverseTransformPoint(hit.point).z));

        int x = (int)Math.Ceiling(transform.InverseTransformPoint(hit.point).x);
        int y = (int)Math.Ceiling(transform.InverseTransformPoint(hit.point).z);

        return new Vector2Int(x + 3, y + 3);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET - 4;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET - 4;

        return transform.TransformPoint(origin);
    }

    private void SelectChessFigure(int targetX, int targetY)
    {
        if (ChessFigurePositions[targetX, targetY] == null) return;
        if (ChessFigurePositions[targetX, targetY].IsWhite != IsWhiteTurn) return;

        SelectedFigure = ChessFigurePositions[targetX, targetY];

        _allowedMoves = ChessFigurePositions[targetX, targetY].PossibleMove();

        bool hasAtLeastOneMove = HasAtLeastOneMove(_allowedMoves);

        if (!hasAtLeastOneMove)
        {
            SelectedFigure = null;
            return;
        }

        BoardHighlighting.HighlightAllowedMoves(_allowedMoves);
    }

    private bool? MoveChessFigure(int targetX, int targetY)
    {
        bool? isCheckMate = null;

        if (_allowedMoves[targetX, targetY])
        {
            ChessFigure c = ChessFigurePositions[targetX, targetY];
            if (c != null && c.IsWhite != IsWhiteTurn)
            {
                ActiveFigures.Remove(c.gameObject);
                Destroy(c.gameObject);
            }

            ChessFigurePositions[SelectedFigure.CurrentX, SelectedFigure.CurrentY] = null;
            SelectedFigure.transform.position = GetTileCenter(targetX, targetY);
            SelectedFigure.SetPosition(targetX, targetY);
            ChessFigurePositions[targetX, targetY] = SelectedFigure;
            IsWhiteTurn = !IsWhiteTurn;

            if (IsCheckmate())
            {
                return true;
            }
            else
            {
                isCheckMate = false;
            }
        }

        BoardHighlighting.HideHighlights();
        SelectedFigure = null;

        return isCheckMate;
    }

    private bool IsCheckmate()
    {
        King king = GetMatedKing();

        if(king != null)
        {
            SelectedFigure = king;
            if (HasAtLeastOneMove(king.PossibleMove()))
            {
                return false;
            }
            else
            {
                if (!IsMateResolveable(king))
                {
                    return true;
                }
            }
            SelectedFigure = null;
        }

        return false;
    }

    private King GetMatedKing()
    {
        List<GameObject> kingGameobjects = ActiveFigures.FindAll(x => x.GetComponent<ChessFigure>().GetType() == typeof(King));

        foreach (GameObject gameObject in ActiveFigures)
        {
            ChessFigure chessFigure = gameObject.GetComponent<ChessFigure>();

            foreach (GameObject kingGameobject in kingGameobjects)
            {
                King king = kingGameobject.GetComponent<King>();

                if (king.IsWhite != chessFigure.IsWhite && chessFigure.PossibleMove()[king.CurrentX, king.CurrentY])
                {
                    return king;
                }
            }
        }

        return null;
    }

    private bool HasAtLeastOneMove(bool[,] allowedMoves)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (allowedMoves[i, j])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsMateResolveable(King matedKing)
    {
        List<GameObject> teamChessFiguresGameobjects = ActiveFigures.FindAll(x => x.GetComponent<ChessFigure>().IsWhite == matedKing.IsWhite);

        foreach (GameObject teamChessFigureGameobject in teamChessFiguresGameobjects)
        {
            ChessFigure chessFigure = teamChessFigureGameobject.GetComponent<ChessFigure>();

            bool[,] possibleMoves = chessFigure.PossibleMove();

            for (int i = 0; i < possibleMoves.GetLength(0); i++)
            {
                for (int j = 0; j < possibleMoves.GetLength(1); j++)
                {
                    if (possibleMoves[i, j])
                    {
                        int backupX = chessFigure.CurrentX;
                        int backupY = chessFigure.CurrentY;

                        chessFigure.SetPosition(i, j);

                        if (!GetMatedKing())
                        {
                            return true;
                        }

                        chessFigure.SetPosition(backupX, backupY);
                    }
                }
            }
        }   

        return false;
    }

    private void SpawnChessFigure(string name, int x, int y)
    {
        GameObject go = Instantiate(_chessFiguresGameObjects[name], GetTileCenter(x, y), _chessFiguresGameObjects[name].transform.rotation, gameObject.GetSpawnContainer().transform);

        //Debug.Log(string.Format("Spawned {0} at {1}, {2}", name, x, y));
        
        ChessFigurePositions[x, y] = go.GetComponent<ChessFigure>();
        ChessFigurePositions[x, y].SetPosition(x, y);
        ChessFigurePositions[x, y].BoardManager = this;
        ActiveFigures.Add(go);
    }

    private void InstantiateChessFiguresDict() 
    {
        _chessFiguresGameObjects = new Dictionary<string, GameObject>();

        foreach (GameObject chessFigureGameObject in ChessFiguresGameObjects)
        {
            _chessFiguresGameObjects.Add(chessFigureGameObject.name, chessFigureGameObject);
        }
    }

    private void SpawnChessBoard()
    {
        foreach (GameObject go in ActiveFigures)
        {
            Destroy(go);
        }

        ActiveFigures = new List<GameObject>();

        IsWhiteTurn = true;
        BoardHighlighting.HideHighlights();

        foreach (ChessFigurePosition chessFigurePosition in ChessBoard)
        {
            SpawnChessFigure(chessFigurePosition.Name, chessFigurePosition.X, chessFigurePosition.Y);
        }
    }
}
