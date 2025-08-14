using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    #region Variables

    private Vector2Int position;
    protected RectTransform rectTransform;
    private ETileState state;
    private Dictionary<ETileNeighborSide, Tile> neighborTiles;

    #endregion Variables

    #region Properties

    public Vector2Int Position => position;
    public ETileState State => state;
    public Vector3 AnchoredPosition => rectTransform.anchoredPosition;

    #endregion Properties

    #region Constructors

    public Tile(int x, int y, ITileObjectProvider tileObjectProvider)
    {
        this.position = new Vector2Int(x, y);
        this.state = ETileState.Empty;
        this.neighborTiles = new Dictionary<ETileNeighborSide, Tile>();
        this.rectTransform = tileObjectProvider.GetTileObject(x, y).GetComponent<RectTransform>();
    }

    #endregion Constructors

    #region Methods

    public Tile GetReferencePoint(ETileNeighborSide neighborTileSide)
    {
        if (neighborTiles.ContainsKey(neighborTileSide))
        {
            return neighborTiles[neighborTileSide];
        }

        return null;
    }

    public void AddReferencePoint(ETileNeighborSide neighborTileSide, Tile tile)
    {
        neighborTiles.Add(neighborTileSide, tile);
    }

    public void SetFull()
    {
        state = ETileState.Full;
    }

    public void SetEmpty()
    {
        state = ETileState.Empty;
    }

    #endregion Methods

    #region Override

    public static bool operator ==(Tile tileFirst, Tile tileSecond)
    {
        if ((object)tileFirst == null && (object)tileSecond == null)
        {
            return true;
        }

        if ((object)tileFirst == null)
        {
            return false;
        }

        if ((object)tileSecond == null)
        {
            return false;
        }

        if (tileFirst.position == tileSecond.position)
        {
            return true;
        }

        return false;
    }

    public static bool operator !=(Tile tileFirst, Tile tileSecond)
    {
        if ((object)tileFirst == null && (object)tileSecond == null)
        {
            return false;
        }

        if ((object)tileFirst == null)
        {
            return true;
        }

        if ((object)tileSecond == null)
        {
            return true;
        }

        if (tileFirst.position == tileSecond.position)
        {
            return false;
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        Tile tile = obj as Tile;

        if ((object)tile != null)
        {
            return position == tile.position;
        }
        else
        {
            return false;
        }

    }

    public bool Equals(Tile tile)
    {
        if ((object)tile == null)
        {
            return false;
        }

        return position == tile.position;
    }

    public override int GetHashCode()
    {
        return position.x ^ position.y;
    }

    #endregion
}