using System;

[Serializable]
public class GameDataDynamic
{
    public int Id;
    public int BestAttempt;
    public int NumberOfGamesPlayed;

    public GameDataDynamic()
    {
        Id = 0;
        BestAttempt = 0;
        NumberOfGamesPlayed = 0;
    }

    public GameDataDynamic(int id, int bestAttempt, int numberOfGamesPlayed)
    {
        Id = id;
        BestAttempt = bestAttempt;
        NumberOfGamesPlayed = numberOfGamesPlayed;
    }

    public void UpdateBestStep(int bestAttempt)
    {
        if (bestAttempt > 0 && (BestAttempt == 0 || bestAttempt < BestAttempt))
        {
            BestAttempt = bestAttempt;
        }
    }
}

