
using System;
using UnityEngine;

public class StaticBlock : ABlock
{
    public StaticBlock(int id, GameObject blockObject, Tile tile) : base(id, blockObject, tile)
    {
        isMovable = false;
        tile.SetFull();
    }

    public override void ChangePoint(Tile nextTile)
    {

    }

    public override void TransitTransform()
    {

    }

    public override void FinalTransform()
    {

    }
}