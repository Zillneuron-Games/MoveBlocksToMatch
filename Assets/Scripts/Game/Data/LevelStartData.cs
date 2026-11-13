using System;
using System.Collections.Generic;
using UnityEngine;
using Zillneuron.Blockmates.Services.SignIn;

public class LevelStartData
{
    private int levelsCount;
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
            if (value >= 1 && value <= levelsCount)
            {
                nextToLoadGameId = value;
            }
        }
    }
    public int LastGame => lastGame;
    public int TotalCoins => totalCoins;
    public Dictionary<ESceneName, string> SceneNames => sceneNames;

    public static LevelStartData CreateInstance()
    {
        GlobalSettings settings = SettingsManager.Instance.Settings;
        int levelsCount = settings.LevelsCount;
        AccountService account = AccountService.Instance;
        int nextGameId = account.NextLevel;
        LevelData[] gameData = new LevelData[levelsCount];
        GameDataDynamic[] gameDataDynamic = account.Levels;

        for (int i = 0; i < levelsCount; i++)
        {
            gameData[i] = LevelDataReader.GetGameData(i + 1);
        }
        return new LevelStartData(levelsCount, nextGameId, gameData, gameDataDynamic);
    }

    public LevelStartData(int levelsCount, int nextGameId, LevelData[] gamesData, GameDataDynamic[] gamesDataDynamic)
    {
        this.levelsCount = levelsCount;
        this.totalCoins = 0;
        this.NextToLoadGame = nextGameId;
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
        gamesDataDynamic[gameDataDynamic.Id - 1] = gameDataDynamic;

        AccountService.Instance.RecordAttempt(gameDataDynamic.Id, gameDataDynamic.BestAttempt);

        if (gameDataDynamic.Id < levelsCount)
        {
            GameDataDynamic nextGameDataDynamic = gamesDataDynamic[gameDataDynamic.Id];

            AccountService.Instance.SetNextGameId(nextGameDataDynamic.Id);

            if (nextGameDataDynamic.NumberOfGamesPlayed == 0)
            {                
                lastGame = nextGameDataDynamic.Id;
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
