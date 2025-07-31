using UnityEngine;

public static class DataReader
{
    private const string gameDataPath = @"Data/";

    public static void SetGameDataDynamic(GameDataDynamic gameDataDynamic)
    {
        if (gameDataDynamic.Id > GameStartData.GamesCount || gameDataDynamic.Id < 1)
        {
            return;
        }

        string gameDataDynamicJson = JsonUtility.ToJson(gameDataDynamic);
        string dataPathFull = string.Format("{0}{1}", GameStartData.GameDataPrefsKey, gameDataDynamic.Id.ToString());

        PlayerPrefs.SetString(dataPathFull, gameDataDynamicJson);
    }

    public static GameData GetGameData(int gameId)
    {
        if (gameId > GameStartData.GamesCount || gameId < 1)
        {
            return null;
        }

        string dataPathFull = string.Format("{0}{1}", gameDataPath, gameId.ToString());

        TextAsset gameInfoJson = Resources.Load(dataPathFull, typeof(TextAsset)) as TextAsset;
        GameData gameData = JsonUtility.FromJson<GameData>(gameInfoJson.text);

        return gameData;
    }

    public static GameDataDynamic GetGameDataDynamic(int gameId)
    {
        if (gameId > GameStartData.GamesCount || gameId < 1)
        {
            return null;
        }

        string dataPathFull = string.Format("{0}{1}", GameStartData.GameDataPrefsKey, gameId.ToString());

        string gameInfoJson = PlayerPrefs.GetString(dataPathFull);

        if(string.IsNullOrEmpty(gameInfoJson))
        {
            SetUpGameDataDynamic();
            return GetGameDataDynamic(gameId);
        }

        GameDataDynamic gameDataDynamic = JsonUtility.FromJson<GameDataDynamic>(gameInfoJson);

        return gameDataDynamic;
    }

    private static void SetUpGameDataDynamic()
    {
        for (int i = 0; i < GameStartData.GamesCount; i++)
        {
            int gamePlayedCount = 0;

            if (i == 0)
            {
                gamePlayedCount = 1;
            }

            GameDataDynamic gameDataDynamic = new GameDataDynamic(i + 1, GameStartData.MaximumStepsCount, 0, gamePlayedCount);
            SetGameDataDynamic(gameDataDynamic);
        }
    }
}
