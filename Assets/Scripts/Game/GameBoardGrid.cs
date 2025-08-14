using System;
using System.Collections.Generic;

public class GameBoardGrid
{
    #region Variables

    private int width;
    private int height;
    private ITileObjectProvider tileObjectProvider;
    private Tile rootTile;

    #endregion Variables

    #region Properties

    public int Width => width;
    public int Height => height;
    public Tile this[int i, int j]
    {
        get
        {
            if (i >= 0 && i < width && j >= 0 && j < height)
            {
                return ForEachPoint(i, j, GetPointByIndex);
            }
            else
            {
                return null;
            }
        }
    }

    #endregion Properties

    #region Constructors

    public GameBoardGrid(int width, int height, ITileObjectProvider tileObjectProvider)
    {
        this.width = width;
        this.height = height;
        this.tileObjectProvider = tileObjectProvider;
        this.rootTile = new Tile(0, 0, tileObjectProvider);

        List<Tile> frontTiles = new List<Tile>();

        frontTiles.Add(rootTile);

        CreateReferencePoints(frontTiles);
    }

    #endregion Constructors

    #region Methods

    private void CreateReferencePoints(List<Tile> frontTiles)
    {
        List<Tile> currentTiless = frontTiles;

        while (currentTiless.Count > 0)
        {
            List<Tile> nextTiles = new List<Tile>();

            foreach (Tile rootTile in currentTiless)
            {
                #region Bottom, Left, Top, Right

                if (rootTile.GetReferencePoint(ETileNeighborSide.Bottom) == null)
                {
                    if (rootTile.Position.y - 1 >= 0)
                    {
                        Tile temp_point = new Tile(rootTile.Position.x, rootTile.Position.y - 1, tileObjectProvider);

                        rootTile.AddReferencePoint(ETileNeighborSide.Bottom, temp_point);
                        rootTile.GetReferencePoint(ETileNeighborSide.Bottom).AddReferencePoint(ETileNeighborSide.Top, rootTile);

                        nextTiles.Add(temp_point);
                    }
                }

                if (rootTile.GetReferencePoint(ETileNeighborSide.Top) == null)
                {
                    if (rootTile.Position.y + 1 < height)
                    {

                        Tile temp_point = new Tile(rootTile.Position.x, rootTile.Position.y + 1, tileObjectProvider);

                        rootTile.AddReferencePoint(ETileNeighborSide.Top, temp_point);
                        rootTile.GetReferencePoint(ETileNeighborSide.Top).AddReferencePoint(ETileNeighborSide.Bottom, rootTile);

                        nextTiles.Add(temp_point);
                    }
                }

                if (rootTile.GetReferencePoint(ETileNeighborSide.Left) == null)
                {
                    if (rootTile.Position.x - 1 >= 0)
                    {
                        Tile temp_point = new Tile(rootTile.Position.x - 1, rootTile.Position.y, tileObjectProvider);

                        rootTile.AddReferencePoint(ETileNeighborSide.Left, temp_point);
                        rootTile.GetReferencePoint(ETileNeighborSide.Left).AddReferencePoint(ETileNeighborSide.Right, rootTile);

                        nextTiles.Add(temp_point);
                    }
                }

                if (rootTile.GetReferencePoint(ETileNeighborSide.Right) == null)
                {
                    if (rootTile.Position.x + 1 < width)
                    {
                        Tile temp_point = new Tile(rootTile.Position.x + 1, rootTile.Position.y, tileObjectProvider);

                        rootTile.AddReferencePoint(ETileNeighborSide.Right, temp_point);
                        rootTile.GetReferencePoint(ETileNeighborSide.Right).AddReferencePoint(ETileNeighborSide.Left, rootTile);

                        nextTiles.Add(temp_point);
                    }
                }

                #endregion

                #region Bottom -> <- Left

                if (rootTile.GetReferencePoint(ETileNeighborSide.Bottom) != null && rootTile.GetReferencePoint(ETileNeighborSide.Left) != null)
                {
                    if (rootTile.GetReferencePoint(ETileNeighborSide.Bottom).GetReferencePoint(ETileNeighborSide.Left) == null && rootTile.GetReferencePoint(ETileNeighborSide.Left).GetReferencePoint(ETileNeighborSide.Bottom) == null)
                    {
                        Tile tempTile = new Tile(rootTile.Position.x - 1, rootTile.Position.y - 1, tileObjectProvider);

                        rootTile.GetReferencePoint(ETileNeighborSide.Bottom).AddReferencePoint(ETileNeighborSide.Left, tempTile);
                        rootTile.GetReferencePoint(ETileNeighborSide.Left).AddReferencePoint(ETileNeighborSide.Bottom, tempTile);

                        tempTile.AddReferencePoint(ETileNeighborSide.Right, rootTile.GetReferencePoint(ETileNeighborSide.Bottom));
                        tempTile.AddReferencePoint(ETileNeighborSide.Top, rootTile.GetReferencePoint(ETileNeighborSide.Left));

                        nextTiles.Add(tempTile);
                    }
                }

                #endregion

                #region Left -> <- Top

                if (rootTile.GetReferencePoint(ETileNeighborSide.Left) != null && rootTile.GetReferencePoint(ETileNeighborSide.Top) != null)
                {
                    if (rootTile.GetReferencePoint(ETileNeighborSide.Left).GetReferencePoint(ETileNeighborSide.Top) == null && rootTile.GetReferencePoint(ETileNeighborSide.Top).GetReferencePoint(ETileNeighborSide.Left) == null)
                    {
                        Tile tempTile = new Tile(rootTile.Position.x - 1, rootTile.Position.y + 1, tileObjectProvider);

                        rootTile.GetReferencePoint(ETileNeighborSide.Left).AddReferencePoint(ETileNeighborSide.Top, tempTile);
                        rootTile.GetReferencePoint(ETileNeighborSide.Top).AddReferencePoint(ETileNeighborSide.Left, tempTile);

                        tempTile.AddReferencePoint(ETileNeighborSide.Bottom, rootTile.GetReferencePoint(ETileNeighborSide.Left));
                        tempTile.AddReferencePoint(ETileNeighborSide.Right, rootTile.GetReferencePoint(ETileNeighborSide.Top));

                        nextTiles.Add(tempTile);
                    }
                }


                #endregion

                #region Top -> <- Right


                if (rootTile.GetReferencePoint(ETileNeighborSide.Top) != null && rootTile.GetReferencePoint(ETileNeighborSide.Right) != null)
                {
                    if (rootTile.GetReferencePoint(ETileNeighborSide.Top).GetReferencePoint(ETileNeighborSide.Right) == null && rootTile.GetReferencePoint(ETileNeighborSide.Right).GetReferencePoint(ETileNeighborSide.Top) == null)
                    {
                        Tile tempTile = new Tile(rootTile.Position.x + 1, rootTile.Position.y + 1, tileObjectProvider);

                        rootTile.GetReferencePoint(ETileNeighborSide.Top).AddReferencePoint(ETileNeighborSide.Right, tempTile);
                        rootTile.GetReferencePoint(ETileNeighborSide.Right).AddReferencePoint(ETileNeighborSide.Top, tempTile);

                        tempTile.AddReferencePoint(ETileNeighborSide.Left, rootTile.GetReferencePoint(ETileNeighborSide.Top));
                        tempTile.AddReferencePoint(ETileNeighborSide.Bottom, rootTile.GetReferencePoint(ETileNeighborSide.Right));

                        nextTiles.Add(tempTile);
                    }
                }


                #endregion

                #region Right -> <- Bottom

                if (rootTile.GetReferencePoint(ETileNeighborSide.Right) != null && rootTile.GetReferencePoint(ETileNeighborSide.Bottom) != null)
                {
                    if (rootTile.GetReferencePoint(ETileNeighborSide.Right).GetReferencePoint(ETileNeighborSide.Bottom) == null && rootTile.GetReferencePoint(ETileNeighborSide.Bottom).GetReferencePoint(ETileNeighborSide.Right) == null)
                    {
                        Tile tempTile = new Tile(rootTile.Position.x + 1, rootTile.Position.y - 1, tileObjectProvider);

                        rootTile.GetReferencePoint(ETileNeighborSide.Right).AddReferencePoint(ETileNeighborSide.Bottom, tempTile);
                        rootTile.GetReferencePoint(ETileNeighborSide.Bottom).AddReferencePoint(ETileNeighborSide.Right, tempTile);

                        tempTile.AddReferencePoint(ETileNeighborSide.Top, rootTile.GetReferencePoint(ETileNeighborSide.Right));
                        tempTile.AddReferencePoint(ETileNeighborSide.Left, rootTile.GetReferencePoint(ETileNeighborSide.Bottom));

                        nextTiles.Add(tempTile);
                    }
                }

                #endregion

            }

            currentTiless = nextTiles;
        }

    }

    public bool Clear()
    {
        return ForEachPoint(SetEmptyAndDropTarget);
    }

    #region Apply For Each Eelemnt

    private Tile ForEachPoint(int x, int y, Func<Tile, int, int, Tile> handler)
    {
        return ApplyForEachPoint(rootTile, x, y, handler);
    }

    private bool ForEachPoint(Func<Tile, bool> handler)
    {
        return ApplyForEachPoint(rootTile, handler);
    }

    private Tile ApplyForEachPoint(Tile currentTile, int x, int y, Func<Tile, int, int, Tile> handler)
    {
        if (currentTile == null)
        {
            return null;
        }

        Tile topTile = currentTile.GetReferencePoint(ETileNeighborSide.Top);
        Tile rightTile = currentTile.GetReferencePoint(ETileNeighborSide.Right);
        Tile nextTile = (currentTile.GetReferencePoint(ETileNeighborSide.Top) == null) ? null : currentTile.GetReferencePoint(ETileNeighborSide.Top).GetReferencePoint(ETileNeighborSide.Right);

        if (handler(currentTile, x, y) != null)
        {
            return handler(currentTile, x, y);
        }

        while (topTile != null)
        {
            if (handler(topTile, x, y) != null)
            {
                return handler(topTile, x, y);
            }

            topTile = topTile.GetReferencePoint(ETileNeighborSide.Top);
        }

        while (rightTile != null)
        {
            if (handler(rightTile, x, y) != null)
            {
                return handler(rightTile, x, y);
            }

            rightTile = rightTile.GetReferencePoint(ETileNeighborSide.Right);
        }

        return ApplyForEachPoint(nextTile, x, y, handler);
    }

    private bool ApplyForEachPoint(Tile currentTile, Func<Tile, bool> handler)
    {
        if (currentTile == null)
        {
            return true;
        }

        Tile topTile = currentTile.GetReferencePoint(ETileNeighborSide.Top);
        Tile rightTile = currentTile.GetReferencePoint(ETileNeighborSide.Right);
        Tile nextTile = (currentTile.GetReferencePoint(ETileNeighborSide.Top) == null) ? null : currentTile.GetReferencePoint(ETileNeighborSide.Top).GetReferencePoint(ETileNeighborSide.Right);

        handler(currentTile);

        while (topTile != null)
        {
            handler(topTile);

            topTile = topTile.GetReferencePoint(ETileNeighborSide.Top);
        }

        while (rightTile != null)
        {
            handler(rightTile);

            rightTile = rightTile.GetReferencePoint(ETileNeighborSide.Right);
        }

        return ApplyForEachPoint(nextTile, handler);
    }

    private Tile GetPointByIndex(Tile tile, int x, int y)
    {
        if (tile.Position.x == x && tile.Position.y == y)
        {
            return tile;
        }

        return null;
    }

    private bool SetEmptyAndDropTarget(Tile tile)
    {
        tile.SetEmpty();

        return false;
    }

    #endregion Apply For Each Eelemnt

    #endregion Methods
}