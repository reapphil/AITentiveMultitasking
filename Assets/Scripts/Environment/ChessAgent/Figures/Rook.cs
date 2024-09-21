using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Rook : ChessFigure
{
    public override bool[,] PossibleMove(bool assignBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Left
        int i = CurrentX;
        while (true)
        {
            i--;
            if (AssignMove(i, CurrentY, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Right
        i = CurrentX;
        while (true)
        {
            i++;
            if (AssignMove(i, CurrentY, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Forward
        i = CurrentY;
        while (true)
        {
            i++;
            if (AssignMove(CurrentX, i, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Back
        i = CurrentY;
        while (true)
        {
            i--;
            if (AssignMove(CurrentX, i, ref possibleMoves, assignBeatingMovements)) break;
        }

        return possibleMoves;
    }
}
