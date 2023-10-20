using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStartData
{
    private const string SoundPrefsKey = "sound";

    private int totalCoins;
    private int gamesCount = 105;
    private int maximumStepsCount = 998;
    private int nextToLoadGameId;
    private int lastGame;
    private GameData[] gamesData;
    private GameDataDynamic[] gamesDataDynamic;

    public bool SoundState
    {
        get
        {
            if (PlayerPrefs.GetInt(SoundPrefsKey) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        set
        {
            if (value)
            {
                PlayerPrefs.SetInt(SoundPrefsKey, 1);
            }
            else
            {
                PlayerPrefs.SetInt(SoundPrefsKey, 0);
            }
        }
    }
    public int NextToLoadGame
    {
        get { return nextToLoadGameId; }
        set
        {
            if (value >= 1 && value <= GamesCount)
                nextToLoadGameId = value;
        }
    }
    public int MaximumStepsCount => maximumStepsCount;
    public int GamesCount => gamesCount;
    public int LastGame => lastGame;

    public Dictionary<ESceneName, string> SceneNames = new Dictionary<ESceneName, string>()
    {
        {ESceneName.Load, "LOAD"},
        {ESceneName.Menu, "MENU"},
        {ESceneName.Game, "GAME"}
    };

    public void UpdateDynamicData(GameDataDynamic gameDataDynamic)
    {
        if (gamesDataDynamic[gameDataDynamic.Id - 1].BestCoins < gameDataDynamic.BestCoins)
        {
            totalCoins = totalCoins + gameDataDynamic.BestCoins - gamesDataDynamic[gameDataDynamic.Id - 1].BestCoins;
        }

        gamesDataDynamic[gameDataDynamic.Id - 1] = gameDataDynamic;

        //DataReader.SetGameDataDynamic(gameDataDynamic);

        if (gameDataDynamic.Id < GamesCount)
        {
            GameDataDynamic nextGameDataDynamic = gamesDataDynamic[gameDataDynamic.Id];

            if (nextGameDataDynamic.GameCount == 0)
            {
                nextGameDataDynamic.GameCount = 1;
                lastGame = nextGameDataDynamic.Id;
                //DataReader.SetGameDataDynamic(nextGameDataDynamic);
            }
        }
    }

    public GameData GetGameData(int gameId)
    {
        return gamesData[gameId - 1];
    }

    public GameDataDynamic GetGameDataDynamic(int gameId)
    {
        return gamesDataDynamic[gameId - 1];
    }
}
