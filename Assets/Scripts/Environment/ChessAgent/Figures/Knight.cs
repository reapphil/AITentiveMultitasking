using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Knight : ChessFigure
{
    public override bool[,] PossibleMove(bool assignBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Up / Left
        AssignMove(CurrentX - 1, CurrentY + 2, ref possibleMoves, assignBeatingMovements);
        AssignMove(CurrentX - 2, CurrentY + 1, ref possibleMoves, assignBeatingMovements);

        // Up / Right
        AssignMove(CurrentX + 1, CurrentY + 2, ref possibleMoves, assignBeatingMovements);
        AssignMove(CurrentX + 2, CurrentY + 1, ref possibleMoves, assignBeatingMovements);

        // Down / Left
        AssignMove(CurrentX - 1, CurrentY - 2, ref possibleMoves, assignBeatingMovements);
        AssignMove(CurrentX - 2, CurrentY - 1, ref possibleMoves, assignBeatingMovements);

        // Down / Right
        AssignMove(CurrentX + 1, CurrentY - 2, ref possibleMoves, assignBeatingMovements);
        AssignMove(CurrentX + 2, CurrentY - 1, ref possibleMoves, assignBeatingMovements);

        return possibleMoves;
    }
}
