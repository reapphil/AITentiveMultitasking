using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Bishop : ChessFigure
{
    public override bool[,] PossibleMove(bool assignBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Top Left
        int i = CurrentX;
        int j = CurrentY;
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
