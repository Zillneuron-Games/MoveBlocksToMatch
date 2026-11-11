using System;
using System.Collections.Generic;
using UnityEngine;
using Zillneuron.Blockmates.Services.SignIn;

public class LevelStartData
{
    public const int GamesCount = 105;

    private int totalCoins;
    private int nextToLoadGameId;
    private int lastGame;
    private LevelData[] gamesData;
    private GameDataDynamic[] gamesDataDynamic;
    private Dictionary<ESceneName, string> sceneNames;

    public int NextToLoadGame
    {
        get { return nextToLoadGameId; }
        set
        {
            if (value >= 1 && value <= GamesCount)
                nextToLoadGameId = value;
        }
    }
    public int LastGame => lastGame;
    public int TotalCoins => totalCoins;
    public Dictionary<ESceneName, string> SceneNames => sceneNames;

    public static LevelStartData CreateInstance()
    {
        int nextGameId = AccountService.Instance.NextLevel;
        LevelData[] gameData = new LevelData[GamesCount];
        GameDataDynamic[] gameDataDynamic = new GameDataDynamic[GamesCount];

        for (int i = 0; i < GamesCount; i++)
        {
            gameData[i] = LevelDataReader.GetGameData(i + 1);
            gameDataDynamic[i] = AccountService.Instance.GetLevelData(i + 1);
        }
        return new LevelStartData(nextGameId, gameData, gameDataDynamic);
    }

    public LevelStartData(int nextGameId, LevelData[] gamesData, GameDataDynamic[] gamesDataDynamic)
    {
        this.totalCoins = 0;
        this.nextToLoadGameId = 1;
        this.lastGame = 0;
        this.gamesData = gamesData;
        this.gamesDataDynamic = gamesDataDynamic;
        this.sceneNames = new Dictionary<ESceneName, string>() { {ESceneName.Load, "LOAD"},
                                                                 {ESceneName.Menu, "MENU"},
                                                                 {ESceneName.Game, "GAME"}
                                                               };
    }

    public void UpdateDynamicData(GameDataDynamic gameDataDynamic)
    {
        //if (gamesDataDynamic[gameDataDynamic.Id - 1].BestCoins < gameDataDynamic.BestCoins)
        //{
        //    totalCoins = totalCoins + gameDataDynamic.BestCoins - gamesDataDynamic[gameDataDynamic.Id - 1].BestCoins;
        //}

        gamesDataDynamic[gameDataDynamic.Id - 1] = gameDataDynamic;

        AccountService.Instance.RecordAttempt(gameDataDynamic.Id, gameDataDynamic.BestAttempt);

        if (gameDataDynamic.Id < GamesCount)
        {
            GameDataDynamic nextGameDataDynamic = gamesDataDynamic[gameDataDynamic.Id];

            AccountService.Instance.SetNextGameId(nextGameDataDynamic.Id);

            if (nextGameDataDynamic.NumberOfGamesPlayed == 0)
            {                
                nextGameDataDynamic.NumberOfGamesPlayed = 1;
                lastGame = nextGameDataDynamic.Id;
                AccountService.Instance.RecordAttempt(nextGameDataDynamic.Id, nextGameDataDynamic.BestAttempt);
            }
        }
    }

    public LevelData GetGameData(int gameId)
    {
        return gamesData[gameId - 1];
    }

    public GameDataDynamic GetGameDataDynamic(int gameId)
    {
        return gamesDataDynamic[gameId - 1];
    }
}
