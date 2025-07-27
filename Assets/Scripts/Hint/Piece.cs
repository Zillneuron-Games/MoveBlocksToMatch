using System;

public class Piece
{
    private Vector2Int position;
    private EPawnColor color;

    public Vector2Int Position => position;
    public EPawnColor Color => color;

    public Piece(Vector2Int position, EPawnColor color)
    {
        this.position = position;
        this.color = color;
    }
}
