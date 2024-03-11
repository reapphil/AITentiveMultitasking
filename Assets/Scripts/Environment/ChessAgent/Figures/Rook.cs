using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Rook : ChessFigure
{
    public override bool[,] PossibleMove(bool assingBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Left
        int i = CurrentX;
        while (true)
        {
            i--;
            if (AssignMove(i, CurrentY, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Right
        i = CurrentX;
        while (true)
        {
            i++;
            if (AssignMove(i, CurrentY, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Forward
        i = CurrentY;
        while (true)
        {
            i++;
            if (AssignMove(CurrentX, i, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Back
        i = CurrentY;
        while (true)
        {
            i--;
            if (AssignMove(CurrentX, i, ref possibleMoves, assingBeatingMovements)) break;
        }

        return possibleMoves;
    }
}
