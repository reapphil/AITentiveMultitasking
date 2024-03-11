using UnityEngine;

public abstract class ChessFigure : MonoBehaviour
{
    public int CurrentX { get; set; }
    
    public int CurrentY { get; set; }

    public BoardManager BoardManager { get; set; }

    [field: SerializeField]
    public bool IsWhite { get; set; }


    public void SetPosition(int x, int y)
    {
        CurrentX = x;
        CurrentY = y;
    }

    public virtual bool[,] PossibleMove(bool beatingMovements = false)
    {
        return new bool[8, 8];
    }

    public bool AssignMove(int x, int y, ref bool[,] possibleMoves, bool assingBeatingMovements)
    {
        bool obstacleEncountered = true;

        ChessFigure FigureOnTargetPosition;
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
        {
            FigureOnTargetPosition = BoardManager.ChessFigurePositions[x, y];
            if (FigureOnTargetPosition == null)
            {
                possibleMoves[x, y] = true;
                obstacleEncountered = false;
            }
            else
            {
                if (assingBeatingMovements || FigureOnTargetPosition.IsWhite != IsWhite)
                {
                    possibleMoves[x, y] = true;
                }
            }
        }

        return obstacleEncountered;
    }
}