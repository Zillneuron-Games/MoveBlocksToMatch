using System;
using UnityEngine;

public class MobileBlock : ABlock
{
    public MobileBlock(int id, GameObject blockObject, Tile tile) : base(id, blockObject, tile)
    {
        isMovable = true;
        tile.SetFull();
    }

    public override void ChangePoint(Tile nextTile)
    {
        tile = nextTile;
    }

    public override void TransitTransform()
    {
        isInTransit = true;
    }

    public override void FinalTransform()
    {
        isInTransit = false;
    }
}