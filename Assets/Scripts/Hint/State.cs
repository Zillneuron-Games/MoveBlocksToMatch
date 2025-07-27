using System;
using System.Linq;

public class State
{
    public Vector2Int[] Pawns;
    public int CostSoFar; // g
    public int EstimatedTotalCost; // f = g + h
    public State Parent;
    public Vector2Int MoveDirection;

    public State(int cost, int estimated, Vector2Int[] pawns, Vector2Int moveDir, State parent)
    {
        Pawns = pawns;
        CostSoFar = cost;
        EstimatedTotalCost = estimated;
        Parent = parent;
        MoveDirection = moveDir;
    }

    public override bool Equals(object obj)
    {
        if (obj is not State other)
        {
            return false;
        }

        // Both null?
        if (Pawns == null && other.Pawns == null)
        {
            return true;
        }

        // One null?
        if (Pawns == null || other.Pawns == null)
        {
            return false;
        }

        // Compare array content
        return Pawns.SequenceEqual(other.Pawns);
    }

    public override int GetHashCode()
    {
        if (Pawns == null)
        {
            return 0;
        }

        // Combine hash codes of all points
        unchecked
        {
            int hash = 17;
            foreach (var block in Pawns)
            {
                hash = hash * 31 + block.GetHashCode();
            }
            return hash;
        }
    }
}
