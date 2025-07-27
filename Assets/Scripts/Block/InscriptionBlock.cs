using System;
using UnityEngine;

public class InscriptionBlock : ABlock
{
    public InscriptionBlock(int id, GameObject blockObject, Tile tile) : base(id, blockObject, tile)
    {
        isMovable = true;
        tile.SetFull();

        SetActive(true);
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

    public void StartStoneMatchEffects()
    {
        SetActive(false);
    }

    private void SetActive(bool value)
    {
        Debug.LogError($"Inscription Block -> SetActive : {value}");
    }
}
