using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Bishop : ChessFigure
{
    public override bool[,] PossibleMove(bool assingBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Top Left
        int i = CurrentX;
        int j = CurrentY;
        while (true)
        {
            i--;
            j++;
            if (AssignMove(i, j, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Top Right
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i++;
            j++;
            if (AssignMove(i, j, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Bottom Left
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i--;
            j--;
            if (AssignMove(i, j, ref possibleMoves, assingBeatingMovements)) break;
        }

        // Bottom Right
        i = CurrentX;
        j = CurrentY;
        while (true)
        {
            i++;
            j--;
            if (AssignMove(i, j, ref possibleMoves, assingBeatingMovements)) break;
        }

        return possibleMoves;
    }
}
