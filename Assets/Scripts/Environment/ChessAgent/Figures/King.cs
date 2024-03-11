using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class King : ChessFigure
{
    public override bool[,] PossibleMove(bool assignBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];
        int i, j;
        bool[,] opponentsPossibleBeatingMoves = new bool[8, 8];

        // Check is necessary to prevent stack overflow caused by the opponent's king
        if (this == BoardManager.SelectedFigure)
        {
            opponentsPossibleBeatingMoves = GetOpponentsPossibleBeatingMoves();
        }

        // Top
        i = CurrentX - 1;
        j = CurrentY + 1;
        for (int k = 0; k < 3; k++)
        {
            AssignKingMove(i, j, ref possibleMoves, opponentsPossibleBeatingMoves, assignBeatingMovements);
            i++;
        }

        // Bottom
        i = CurrentX - 1;
        j = CurrentY - 1;
        for (int k = 0; k < 3; k++)
        {
            AssignKingMove(i, j, ref possibleMoves, opponentsPossibleBeatingMoves, assignBeatingMovements);
            i++;
        }

        // Left
        AssignKingMove(CurrentX - 1, CurrentY, ref possibleMoves, opponentsPossibleBeatingMoves, assignBeatingMovements);

        // Right
        AssignKingMove(CurrentX + 1, CurrentY, ref possibleMoves, opponentsPossibleBeatingMoves, assignBeatingMovements);

        return possibleMoves;
    }

    private void AssignKingMove(int x, int y, ref bool[,] possibleMoves, bool[,] opponentsPossibleBeatingMoves, bool assingBeatingMovements)
    {
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
        {
            if (!opponentsPossibleBeatingMoves[x, y])
            {
                AssignMove(x, y, ref possibleMoves, assingBeatingMovements);
            }
        }
    }

    private bool[,] GetOpponentsPossibleBeatingMoves()
    {
        bool[,] possibleMoves = new bool[8, 8];

        List<GameObject> chessFigures = BoardManager.ActiveFigures;

        foreach (GameObject chessFigure in chessFigures)
        {
            ChessFigure figure = chessFigure.GetComponent<ChessFigure>();
            if (figure.IsWhite != IsWhite)
            {
                bool[,] figurePossibleMoves = figure.PossibleMove(true);
                possibleMoves = MergePossibleMoves(possibleMoves, figurePossibleMoves);
            }
        }

        return possibleMoves;
    }

    private bool[,] MergePossibleMoves(bool[,] m1, bool[,] m2)
    {
        bool[,] result = new bool[8, 8];

        for (int i = 0; i < m1.GetLength(0); i++)
        {
            for (int j = 0; j < m1.GetLength(1); j++)
            {
                result[i, j] = m1[i, j] || m2[i, j];
            }
        }
        
        return result;
    }
}
