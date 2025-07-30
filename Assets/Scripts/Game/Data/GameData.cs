using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GameData
{
    public int Id;
    public int StageId;
    public int GameId;
    public int MinimumStepsCount;

    public Vector2Int Red;
    public Vector2Int Blue;
    public Vector2Int Green;

    public Vector2Int RedTarget;
    public Vector2Int BlueTarget;
    public Vector2Int GreenTarget;

    public List<Vector2Int> Mobiles;
    public List<Vector2Int> Statics;

    public GameData()
    {
        Id = 0;
        MinimumStepsCount = 0;

        Red = new Vector2Int();
        Blue = new Vector2Int();
        Green = new Vector2Int();

        RedTarget = new Vector2Int();
        BlueTarget = new Vector2Int();
        GreenTarget = new Vector2Int();

        Mobiles = null;
        Statics = null;
    }

    public GameData(int id, int gameId, int stageId, int stepsMinimum,
                    Vector2 inscriptionBlockRed, Vector2 inscriptionBlockBlue, Vector2 inscriptionBlockGreen,
                    Vector2 targetBlockRed, Vector2 targetBlockBlue, Vector2 targetBlockGreen,
                    List<Vector2> mobileBlocksPositions = null, List<Vector2> staticBlocksositions = null)
    {
        Id = id;
        GameId = gameId;
        StageId = stageId;
        MinimumStepsCount = stepsMinimum;

        Red = (Vector2Int)inscriptionBlockRed;
        Blue = (Vector2Int)inscriptionBlockBlue;
        Green = (Vector2Int)inscriptionBlockGreen;

        RedTarget = (Vector2Int)targetBlockRed;
        BlueTarget = (Vector2Int)targetBlockBlue;
        GreenTarget = (Vector2Int)targetBlockGreen;

        Mobiles = mobileBlocksPositions != null ? mobileBlocksPositions.Select(m => (Vector2Int)m).ToList(): null ;
        Statics = staticBlocksositions != null ? staticBlocksositions.Select(m => (Vector2Int)m).ToList() : null;
    }

    public bool IsDoubleGame
    {
        get { return Green == GreenTarget; }
    }
}

