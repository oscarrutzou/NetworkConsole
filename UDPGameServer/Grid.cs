using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGameServer;

[MessagePackObject]
public class Grid
{
    [Key(0)]
    public Character[,] CharacterGrid;
    private static string[] _objectFields = new string[]
    {
        "|          ",
        "|          ",
        "|          ",
        "|          ",
        "|__________"
    };

    public Grid(int x, int y)
    {
        CharacterGrid = new Character[x, y];
    }

    public void AddObject(Character character, int x, int y)
    {
        CharacterGrid[x, y] = character;
    }

    public void MoveObject(int oldX, int oldY, int newX, int newY)
    {
        if (oldX > CharacterGrid.GetLength(0))
        {
            Console.WriteLine($"first input {oldX} is out of bounds!");
            return;
        }
        if (oldY > CharacterGrid.GetLength(1))
        {
            Console.WriteLine($"second input{oldY} is out of bounds!");
            return;
        }
        if (newX > CharacterGrid.GetLength(0))
        {
            Console.WriteLine($"third input{newX} is out of bounds!");
            return;
        }
        if (newY > CharacterGrid.GetLength(0))
        {
            Console.WriteLine($"fourth input{newY} is out of bounds!");
            return;
        }
        if (CharacterGrid[oldX, oldY] == null)
        {
            Console.WriteLine($"nothing to move on position: {oldX},{oldY}");
            return;
        }

        if (CharacterGrid[oldX, oldY] != null)
        {
            CharacterGrid[newX, newY] = CharacterGrid[oldX, oldY];
            CharacterGrid[oldX, oldY] = null;
        }

        DrawGrid();
    }

    public void DrawGrid()
    {
        Console.Clear();
        //add initinal line
        for (int i = 0; i < CharacterGrid.GetLength(0); i++)
        {
            Console.Write("___________");
        }
        Console.Write('\n');

        for (int y = 0; y < CharacterGrid.GetLength(1); y++)
        {
            for (int i = 0; i < _objectFields.Length; i++)
            {
                for (int x = 0; x < CharacterGrid.GetLength(0); x++)
                {
                    if (CharacterGrid[y, x] != null)
                        Console.Write(CharacterGrid[y, x].GetGridData()[i]);
                    else
                        Console.Write(_objectFields[i]);
                }
                //add right side of grid!
                Console.Write("|\n");
            }
        }
    }
}
