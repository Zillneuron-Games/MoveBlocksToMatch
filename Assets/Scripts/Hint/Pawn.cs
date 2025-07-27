using System;

public class Pawn : Block
{
    protected EPawnColor color;

    public EPawnColor Color => color;

    public Pawn(Vector2Int position, EPawnColor color) : base(position)
    {
        this.color = color;
    }
}
