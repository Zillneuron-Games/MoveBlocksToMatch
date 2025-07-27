using System;

public class Block
{
    protected Vector2Int position;

    public Vector2Int Position => position;

    public Block(Vector2Int position)
    {
        this.position = position;
    }
}
