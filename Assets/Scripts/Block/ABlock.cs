using System;
using UnityEngine;

public abstract class ABlock
{
    #region Variables

    protected int id;
    protected bool isMovable;
    protected bool isInTransit;
    protected RectTransform rectTransform;
    protected GameObject blockObject;
    protected Tile tile;

    #endregion Variables

    #region Properties

    public int Id
    {
        get { return id; }
    }

    public bool IsInTransit
    {
        get { return isInTransit; }
    }

    public Tile CurrentTile
    {
        get { return tile; }
    }

    public Vector3 AnchoredPosition
    {
        get { return rectTransform.anchoredPosition; }
        set { rectTransform.anchoredPosition = value; }
    }

    #endregion Properties

    public ABlock(int id, GameObject blockObject, Tile tile)
    {
        this.id = id;
        this.blockObject = blockObject;
        this.tile = tile;
        this.rectTransform = blockObject.GetComponent<RectTransform>();
        this.isInTransit = false;
    }

    public void Show()
    {
        blockObject.SetActive(true);
    }

    public void Hide()
    {
        blockObject.SetActive(true);
    }

    #region Methods

    public abstract void ChangePoint(Tile nextTile);

    public abstract void TransitTransform();

    public abstract void FinalTransform();

    #endregion Methods
}