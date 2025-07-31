using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hint
{
    private int width;
    private int height;
    private Board board;
    private Vector2Int[] pawns;
    private Vector2Int[] blocks;

    public Hint(Board board)
    {
        this.board = board;
        this.pawns = board.Pawns.Select(m => m.Position).ToArray();
        this.blocks = board.Blocks.Select(m => m.Position).ToArray();
        this.width = board.Width;
        this.height = board.Height;
    }

    public Vector2Int GetAStarHint()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Direction2D.Up, Direction2D.Down, Direction2D.Left, Direction2D.Right
        };

        var start = new State(0, Heuristic(pawns), pawns, Direction2D.None, null);
        var openSet = new SortedSet<State>(Comparer<State>.Create((a, b) =>
        {
            int cmp = a.EstimatedTotalCost.CompareTo(b.EstimatedTotalCost);
            return cmp != 0 ? cmp : a.GetHashCode().CompareTo(b.GetHashCode());
        }));

        var visited = new HashSet<State>();

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            var current = openSet.Min;
            openSet.Remove(current);

            if (IsGoal(current))
            {
                return ReconstructFirstMove(current);
            }

            visited.Add(current);

            foreach (var direction in directions)
            {
                var nextBlocks = SimulateMove(blocks, current.Pawns, direction);
                var next = new State(current.CostSoFar + 1, current.CostSoFar + 1 + Heuristic(nextBlocks), nextBlocks, direction, current);

                if (visited.Contains(next))
                {
                    continue;
                }

                openSet.Add(next);
            }
        }

        return Direction2D.None; // No solution found
    }

    private bool IsGoal(State state)
    {
        int matchCount = 0;

        for (int i = 0; i < board.Targets.Length; i++)
        {
            if (board.Targets[i].Position.x == state.Pawns[i].x
             && board.Targets[i].Position.y == state.Pawns[i].y)
            {
                matchCount++;
            }
        }

        return matchCount == board.Targets.Length;
    }

    private Vector2Int ReconstructFirstMove(State state)
    {
        while (state.Parent != null && state.Parent.Parent != null)
        {
            state = state.Parent;
        }

        return state.MoveDirection;
    }

    private int Heuristic(Vector2Int[] blocks)
    {
        int total = 0;
        for (int i = 0; i < board.Targets.Length; i++)
        {
            total += Manhattan(blocks[i], board.Targets[i].Position);
        }
        return total;
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int[] SimulateMove(Vector2Int[] staticBlocks, Vector2Int[] movablePawns, Vector2Int direction)
    {
        Vector3Int emptyVector = new Vector3Int(-1, -1, -1);
        Vector3Int[,] grid = new Vector3Int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = emptyVector;
            }
        }

        for (int i = 0; i < movablePawns.Length; i++)
        {
            grid[movablePawns[i].x, movablePawns[i].y] = new Vector3Int(movablePawns[i].x, movablePawns[i].y, i + 1);
        }

        for (int i = 0; i < staticBlocks.Length; i++)
        {
            grid[staticBlocks[i].x, staticBlocks[i].y] = new Vector3Int(staticBlocks[i].x, staticBlocks[i].y, 0);
        }

        if (Direction2D.Left == direction)
        {
            for (int x = 1; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Move(grid, x, y, direction, emptyVector);
                }
            }
        }
        else if (Direction2D.Right == direction)
        {
            for (int x = width - 2; x >= 0; x--)
            {
                for (int y = 0; y < height; y++)
                {
                    Move(grid, x, y, direction, emptyVector);
                }
            }
        }
        else if (Direction2D.Up == direction)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = height - 2; y >= 0; y--)
                {
                    Move(grid, x, y, direction, emptyVector);
                }
            }
        }
        else if (Direction2D.Down == direction)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height; y++)
                {
                    Move(grid, x, y, direction, emptyVector);
                }
            }
        }

        var result = grid.Cast<Vector3Int>()
                         .Where(m => m.z > 0)
                         .OrderBy(m => m.z)
                         .Select(m => new Vector2Int(m.x, m.y))
                         .ToArray();
        return result;
    }

    private void Move(Vector3Int[,] grid, int x, int y, Vector2Int direction, Vector3Int emptyVector)
    {
        Vector3Int block = grid[x, y];

        if (block.z > 0)
        {
            Vector2Int position = new Vector2Int(block.x, block.y);
            Vector2Int nextPosition = position + direction;

            if (IsValid(grid, nextPosition))
            {
                grid[position.x, position.y] = emptyVector;
                grid[nextPosition.x, nextPosition.y] = new Vector3Int(nextPosition.x, nextPosition.y, block.z);
            }
        }
    }

    private bool IsValid(Vector3Int[,] grid, Vector2Int position)
    {
        if (position.x < 0 || position.x >= width || position.y < 0 || position.y >= height)
        {
            return false;
        }

        if (grid[position.x, position.y].z >= 0)
        {
            return false;
        }

        return true;
    }
}

