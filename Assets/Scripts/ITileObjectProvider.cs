using System;
using UnityEngine;

public interface ITileObjectProvider
{
    GameObject GetTileObject(int x, int y);
}
