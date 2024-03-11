using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ChessFigurePosition
{
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public static class FENLoader
{
    public static List<List<ChessFigurePosition>> LoadChessBoardsFromFEN(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        List<List<ChessFigurePosition>> chessBoards = new();

        foreach (string line in lines)
        {
            chessBoards.Add(ReadChessBoardFromFEN(line));
        }

        return chessBoards;
    }

    public static List<(List<ChessFigurePosition>, string)> LoadChessBoardsInclusiveMovesFromLichess(string filePath)
    {
        List<(List<ChessFigurePosition>, string)> chessBoards = new();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                string fen = csv.GetField("FEN");
                string moves = csv.GetField("Moves");

                chessBoards.Add((ReadChessBoardFromFEN(fen), moves));
            }
        }

        return chessBoards;
    }


    private static List<ChessFigurePosition> ReadChessBoardFromFEN(string fen)
    {
        List<ChessFigurePosition> chessFigures = new();
        List<int> chessFigurePositions = new();

        try
        {
            fen = fen.Split(' ')[0];

            int y = 7;
            int x = 0;

            for(int i = 0; i < fen.Length; i++)
            {
                char c = fen[i];

                if (c == '/')
                {
                    y--;
                    x = 0;
                }
                else if (Char.IsDigit(c))
                {
                    int emptySpaces = (int)Char.GetNumericValue(c);
                    x += emptySpaces;
                }
                else
                {
                    string pieceName = $"Chess {GetPieceType(c)} {(Char.IsUpper(c) ? "White" : "Black")}";
                    chessFigures.Add(new ChessFigurePosition { Name = pieceName, X = x, Y = y });

                    if (chessFigurePositions.Contains(PositionConverter.DiscreteVectorToBin(new Vector2Int(x, y), 8)))
                    {
                        Debug.LogError("Duplicate figure position in FEN: " + fen);
                    }
                    else
                    {
                        chessFigurePositions.Add(PositionConverter.DiscreteVectorToBin(new Vector2Int(x, y), 8));
                    }

                    x++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading file: " + ex.Message);
        }

        return chessFigures;
    }

    static string GetPieceType(char c)
    {
        switch (Char.ToUpper(c))
        {
            case 'P': return "Pawn";
            case 'R': return "Rook";
            case 'N': return "Knight";
            case 'B': return "Bishop";
            case 'Q': return "Queen";
            case 'K': return "King";
            default: return "";
        }
    }
}
