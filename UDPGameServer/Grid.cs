﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGameServer;

[MessagePackObject]
public class Grid
{
    [IgnoreMember]
    public Character[,] CharacterGrid { get; set; }

    [Key(0)]
    public Point GridSize { get; set; }

    private static string[] _emptyCharaterFields =
    [
        "|              ",
        "|              ",
        "|              ",
        "|              ",
        "|              ",
        "|______________"
    ];
    private static int _indexOfPosition = 4;
    public Grid(int x, int y)
    {
        GridSize = new Point(x, y);
        CharacterGrid = new Character[x, y];
    }

    public void AddObject(Character character, int x, int y)
    {
        CharacterGrid[x, y] = character;
    }

    public TryMoveData TryMoveObject(Point prevPos, Point newTargetPos, int ownerID)
    {
        TryMoveData tryMoveData = new TryMoveData() { HasMoved = false, ReturnMsg = "" };

        if (prevPos.X >= CharacterGrid.GetLength(0) || prevPos.Y >= CharacterGrid.GetLength(1))
        {
            tryMoveData.ReturnMsg = $"Prev pos is out of bounds!";
            return tryMoveData;
        }
        if (newTargetPos.X >= CharacterGrid.GetLength(0) || newTargetPos.Y >= CharacterGrid.GetLength(1))
        {
            tryMoveData.ReturnMsg = $"New target pos is out of bounds!";
            return tryMoveData;
        }

        if (CharacterGrid[prevPos.X, prevPos.Y] == null)
        {
            tryMoveData.ReturnMsg = $"Nothing to move on position: {prevPos.X},{prevPos.Y}";
            return tryMoveData;
        }

        if (CharacterGrid[prevPos.X, prevPos.Y] != null && CharacterGrid[prevPos.X, prevPos.Y].OwnerID != ownerID)
        {
            tryMoveData.ReturnMsg = $"You dont own this hero: {prevPos.X},{prevPos.Y}";
            return tryMoveData;
        }

        if (CharacterGrid[prevPos.X, prevPos.Y] != null && CharacterGrid[newTargetPos.X, newTargetPos.Y] == null)
        {
            CharacterGrid[newTargetPos.X, newTargetPos.Y] = CharacterGrid[prevPos.X, prevPos.Y];
            CharacterGrid[prevPos.X, prevPos.Y] = null;
            tryMoveData.ReturnMsg = $"Have moved from :{prevPos.X},{prevPos.Y}  to: {newTargetPos.X},{newTargetPos.Y}";
            
            tryMoveData.HasMoved = true;
        }
        else if (CharacterGrid[prevPos.X, prevPos.Y] != null && CharacterGrid[newTargetPos.X, newTargetPos.Y] != null)
        {
            // Grid deal damage to other character
            Character attacker = CharacterGrid[prevPos.X, prevPos.Y];
            Character target = CharacterGrid[newTargetPos.X, newTargetPos.Y];
            
            if (attacker.OwnerID == ownerID && target.OwnerID == ownerID)
            {
                tryMoveData.ReturnMsg = "Both attacker and target belong to the user...";
                return tryMoveData;
            }

            int dmg = attacker.Damage;

            bool isStillAlive = attacker.DealDamage(target);

            if (isStillAlive)
            {
                tryMoveData.ReturnMsg = $"Has dealth {dmg} to {target.Name} with {attacker.Name}";
                tryMoveData.HasDealtDamage = true;
            }
            else
            {
                CharacterGrid[newTargetPos.X, newTargetPos.Y] = CharacterGrid[prevPos.X, prevPos.Y];
                CharacterGrid[prevPos.X, prevPos.Y] = null;
                tryMoveData.HasMoved = true;

                tryMoveData.ReturnMsg = $"Has killed enemy {target.Name} with {attacker.Name}";
            }
        }

        return tryMoveData;
    }

    public void DrawGrid()
    {
        //Add the initinal line
        for (int i = 0; i < CharacterGrid.GetLength(0); i++)
        {
            Console.Write("_______________");
        }

        Console.Write("\n");

        for (int y = 0; y < CharacterGrid.GetLength(1); y++)
        {
            for (int i = 0; i < _emptyCharaterFields.Length; i++)
            {
                for (int x = 0; x < CharacterGrid.GetLength(0); x++)
                {
                    if (i == _indexOfPosition)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        string coordinates = $"{y}, {x}";
                        Console.Write($"|{coordinates.PadLeft(14, ' ')}"); // Sets the coordinates to the right
                    }else
                    {
                        Console.ResetColor();
                        if (CharacterGrid[y, x] != null)

                            Console.Write(CharacterGrid[y, x].GetGridData()[i]);
                        else
                            Console.Write(_emptyCharaterFields[i]);
                    }
                }
                //add right side of grid!
                Console.Write("|\n");
            }
        }
    }
}

public struct TryMoveData
{
    public bool HasMoved { get; set; }
    public bool HasDealtDamage { get; set; }
    public string ReturnMsg { get; set; }
}
