using UnityEngine;

public static class LevelDataReader
{
    //private const string SoundPrefsKey = "sound";
    //private const string MusicPrefsKey = "music";
    //private const string PlayerPrefsKey_LanguageCode = "language_code";
    //private const string PlayerPrefsKey_FirstLoadOnDevice = "first_load_on_device";
    //private const string GameDataPrefsKey = "gamedata";
    //private const string PlayerPrefsKey_NextGameId = "next_game_id";
    private const string LevelDataPath = @"Data/";

    //public static void SetSound(bool on)
    //{
    //    PlayerPrefs.SetInt(SoundPrefsKey, on ? 1 : 0);
    //}

    //public static bool GetSound()
    //{
    //    return PlayerPrefs.GetInt(SoundPrefsKey, 1) == 1;
    //}

    //public static void SetMusic(bool on)
    //{
    //    PlayerPrefs.SetInt(MusicPrefsKey, on ? 1 : 0);
    //}

    //public static bool GetMusic()
    //{
    //    return PlayerPrefs.GetInt(MusicPrefsKey, 1) == 1;
    //}

    //public static void SetLanguage(ELanguageCode languageCode)
    //{
    //    PlayerPrefs.SetInt(PlayerPrefsKey_LanguageCode, (int)languageCode);
    //}

    //public static ELanguageCode GetLanguage()
    //{
    //    return (ELanguageCode)PlayerPrefs.GetInt(PlayerPrefsKey_LanguageCode, 0);
    //}

    //public static void SetFirstLoadOnDevice()
    //{
    //    PlayerPrefs.SetInt(PlayerPrefsKey_FirstLoadOnDevice, 1);
    //}

    //public static bool GetFirstLoadOnDevice()
    //{
    //    return PlayerPrefs.GetInt(PlayerPrefsKey_FirstLoadOnDevice, 0) == 0;
    //}

    //public static void SetNextGameId(int gameId)
    //{
    //    PlayerPrefs.SetInt(PlayerPrefsKey_NextGameId, gameId);
    //}

    //public static int GetNextGameId()
    //{
    //    return PlayerPrefs.GetInt(PlayerPrefsKey_NextGameId, 1);
    //}

    public static LevelData GetGameData(int gameId)
    {
        if (gameId > SettingsManager.Instance.Settings.LevelsCount || gameId < 1)
        {
            return null;
        }

        string dataPathFull = string.Format("{0}{1}", LevelDataPath, gameId.ToString());

        TextAsset gameInfoJson = Resources.Load(dataPathFull, typeof(TextAsset)) as TextAsset;
        LevelData gameData = JsonUtility.FromJson<LevelData>(gameInfoJson.text);

        return gameData;
    }

    //public static void SetGameDataDynamic(GameDataDynamic gameDataDynamic)
    //{
    //    if (gameDataDynamic.Id > GameStartData.GamesCount || gameDataDynamic.Id < 1)
    //    {
    //        return;
    //    }

    //    string gameDataDynamicJson = JsonUtility.ToJson(gameDataDynamic);
    //    string dataPathFull = string.Format("{0}{1}", GameDataPrefsKey, gameDataDynamic.Id.ToString());

    //    PlayerPrefs.SetString(dataPathFull, gameDataDynamicJson);
    //}

    //public static GameDataDynamic GetGameDataDynamic(int gameId)
    //{
    //    if (gameId > GameStartData.GamesCount || gameId < 1)
    //    {
    //        return null;
    //    }

    //    string dataPathFull = string.Format("{0}{1}", GameDataPrefsKey, gameId.ToString());

    //    string gameInfoJson = PlayerPrefs.GetString(dataPathFull);

    //    if(string.IsNullOrEmpty(gameInfoJson))
    //    {
    //        SetUpGameDataDynamic();
    //        return GetGameDataDynamic(gameId);
    //    }

    //    GameDataDynamic gameDataDynamic = JsonUtility.FromJson<GameDataDynamic>(gameInfoJson);

    //    return gameDataDynamic;
    //}

    //private static void SetUpGameDataDynamic()
    //{
    //    for (int i = 0; i < GameStartData.GamesCount; i++)
    //    {
    //        int gamePlayedCount = 0;

    //        if (i == 0)
    //        {
    //            gamePlayedCount = 1;
    //        }

    //        GameDataDynamic gameDataDynamic = new GameDataDynamic(i + 1/*, GameStartData.MaximumStepsCount*/, 0, gamePlayedCount);
    //        SetGameDataDynamic(gameDataDynamic);
    //    }
    //}
}
