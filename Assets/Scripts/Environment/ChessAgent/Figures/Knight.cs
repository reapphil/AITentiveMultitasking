using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Knight : ChessFigure
{
    public override bool[,] PossibleMove(bool assingBeatingMovements = false)
    {
        bool[,] possibleMoves = new bool[8, 8];

        // Up / Left
        AssignMove(CurrentX - 1, CurrentY + 2, ref possibleMoves, assingBeatingMovements);
        AssignMove(CurrentX - 2, CurrentY + 1, ref possibleMoves, assingBeatingMovements);

        // Up / Right
        AssignMove(CurrentX + 1, CurrentY + 2, ref possibleMoves, assingBeatingMovements);
        AssignMove(CurrentX + 2, CurrentY + 1, ref possibleMoves, assingBeatingMovements);

        // Down / Left
        AssignMove(CurrentX - 1, CurrentY - 2, ref possibleMoves, assingBeatingMovements);
        AssignMove(CurrentX - 2, CurrentY - 1, ref possibleMoves, assingBeatingMovements);

        // Down / Right
        AssignMove(CurrentX + 1, CurrentY - 2, ref possibleMoves, assingBeatingMovements);
        AssignMove(CurrentX + 2, CurrentY - 1, ref possibleMoves, assingBeatingMovements);

        return possibleMoves;
    }
}
