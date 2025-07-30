using System;
using UnityEngine;

[Serializable]
public struct Vector2Int
{
    public int x;
    public int y;

    public Vector2Int(int x, int y) 
    { 
        this.x = x; 
        this.y = y; 
    }

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    public static bool operator ==(Vector2Int a, Vector2Int b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Vector2Int a, Vector2Int b)
    {
        return !a.Equals(b);
    }
    
    public static implicit operator Vector2(Vector2Int source)
    {
        return new Vector2(source.x, source.y);
    }

    public static explicit operator Vector2Int(Vector2 source)
    {
        return new Vector2Int((int)source.x, (int)source.y);
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector2Int other)
        {
            return x == other.x && y == other.y;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return x ^ y << 2;
    }
}
