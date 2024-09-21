using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Queen : ChessFigure
{
    public override bool[,] PossibleMove(bool assignBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];
        int i, j;

        // From Rook

        // Left
        i = CurrentX;
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

        // From Bishop

        // Top Left
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i--;
            j++;
            if (AssignMove(i, j, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Top Right
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i++;
            j++;
            if (AssignMove(i, j, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Bottom Left
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i--;
            j--;
            if (AssignMove(i, j, ref possibleMoves, assignBeatingMovements)) break;
        }

        // Bottom Right
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i++;
            j--;
            if (AssignMove(i, j, ref possibleMoves, assignBeatingMovements)) break;
        }

        return possibleMoves;
    }
}
