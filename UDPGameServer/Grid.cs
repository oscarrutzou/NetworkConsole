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
    public Character[,] grid;
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
        grid = new Character[x, y];
    }

    public void AddObject(Character character, int x, int y)
    {
        grid[x, y] = character;
    }

    public void MoveObject(int oldX, int oldY, int newX, int newY)
    {
        if (oldX > grid.GetLength(0))
        {
            Console.WriteLine($"first input {oldX} is out of bounds!");
            return;
        }
        if (oldY > grid.GetLength(1))
        {
            Console.WriteLine($"second input{oldY} is out of bounds!");
            return;
        }
        if (newX > grid.GetLength(0))
        {
            Console.WriteLine($"third input{newX} is out of bounds!");
            return;
        }
        if (newY > grid.GetLength(0))
        {
            Console.WriteLine($"fourth input{newY} is out of bounds!");
            return;
        }
        if (grid[oldX, oldY] == null)
        {
            Console.WriteLine($"nothing to move on position: {oldX},{oldY}");
            return;
        }

        if (grid[oldX, oldY] != null)
        {
            grid[newX, newY] = grid[oldX, oldY];
            grid[oldX, oldY] = null;
        }

        DrawGrid();
    }

    public void DrawGrid()
    {
        Console.Clear();
        //add initinal line
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            Console.Write("___________");
        }
        Console.Write('\n');

        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int i = 0; i < _objectFields.Length; i++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[y, x] != null)
                        Console.Write(grid[y, x].GetGridData()[i]);
                    else
                        Console.Write(_objectFields[i]);
                }
                //add right side of grid!
                Console.Write("|\n");
            }
        }
    }
}
