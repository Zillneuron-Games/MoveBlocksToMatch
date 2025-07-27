using System;

public class Board
{
    private int width;
    private int height;
    private Pawn[] pawns;
    private Block[] blocks;
    private Piece[] targets;

    public int Width => width;
    public int Height => height;
    public Pawn[] Pawns => pawns;
    public Block[] Blocks => blocks;
    public Piece[] Targets => targets;

    public Board(int width, int height, Pawn[] pawns, Block[] blocks, Piece[] targets)
    {
        this.width = width;
        this.height = height;
        this.pawns = pawns;
        this.blocks = blocks;
        this.targets = targets;
    }
}