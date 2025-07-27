using System;
using UnityEngine;

public class TargetBlock : ABlock
{
    public TargetBlock(int id, GameObject blockObject, Tile tile) : base(id, blockObject, tile)
    {
        isMovable = false;
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
