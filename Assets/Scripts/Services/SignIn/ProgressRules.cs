using System;
using System.Collections.Generic;

/// <summary>
/// Rules for GameDataDynamic[]: levels are 1-based, sequential and gap-free.
/// BestAttempt == 0 means the level has not yet been completed.
/// No extra last-unlocked-level field is stored: it is derived from the array.
/// </summary>
public static class ProgressRules
{
    public static GameDataDynamic[] Normalize(GameDataDynamic[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<GameDataDynamic>();

        var byId = new SortedDictionary<int, GameDataDynamic>();
        foreach (GameDataDynamic entry in source)
        {
            if (entry == null || entry.Id < 1 || entry.BestAttempt < 0 ||
                entry.NumberOfGamesPlayed < 0)
                throw new FormatException("Invalid level progress entry.");

            if (!byId.TryGetValue(entry.Id, out GameDataDynamic existing))
            {
                byId.Add(entry.Id, new GameDataDynamic(
                    entry.Id, entry.BestAttempt, entry.NumberOfGamesPlayed));
            }
            else
            {
                existing.BestAttempt = BestPositive(existing.BestAttempt, entry.BestAttempt);
                existing.NumberOfGamesPlayed = Math.Max(
                    existing.NumberOfGamesPlayed, entry.NumberOfGamesPlayed);
            }
        }

        var result = new List<GameDataDynamic>(byId.Count);
        int expectedId = 1;
        bool foundIncomplete = false;
        foreach (GameDataDynamic value in byId.Values)
        {
            if (value.Id != expectedId++)
                throw new FormatException("Level data contains a gap in sequential progression.");
            if (foundIncomplete)
                throw new FormatException("No level may follow an uncompleted level.");
            if (value.BestAttempt > 0 && value.NumberOfGamesPlayed == 0)
                throw new FormatException("Completed levels must have been played at least once.");
            if (value.BestAttempt == 0)
            {
                foundIncomplete = true;
                if (value.NumberOfGamesPlayed == 0)
                {
                    // An empty trailing entry conveys no information.
                    continue;
                }
            }
            result.Add(value);
        }
        return result.ToArray();
    }

    public static int LastUnlockedLevel(GameDataDynamic[] data)
    {
        GameDataDynamic[] clean = Normalize(data);
        return clean.Length > 0 && clean[clean.Length - 1].BestAttempt == 0
            ? clean.Length : clean.Length + 1;
    }

    public static bool CanPlay(GameDataDynamic[] data, int levelId) =>
        levelId >= 1 && levelId <= LastUnlockedLevel(data);

    /// <summary>
    /// Record ONE attempt. "completed" sets/improves the best step count.
    /// A failure increments NumberOfGamesPlayed but does not unlock the next level.
    /// Only already unlocked levels can be attempted.
    /// </summary>
    public static GameDataDynamic[] RecordAttempt(GameDataDynamic[] data,
        int levelId, int steps = 0)
    {
        if (levelId < 1 || steps <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelId),
                "Invalid level or completed step count.");
        GameDataDynamic[] clean = Normalize(data);
        if (!CanPlay(clean, levelId))
            throw new InvalidOperationException("Cannot play a locked level.");

        var list = new List<GameDataDynamic>(clean);
        if (levelId == clean.Length + 1)
            list.Add(new GameDataDynamic(levelId, 0, 0));

        var level = list[levelId - 1];
        level.NumberOfGamesPlayed = checked(level.NumberOfGamesPlayed + 1);
        level.BestAttempt = BestPositive(level.BestAttempt, steps);
        return list.ToArray();
    }

    /// <summary>
    /// Monotonic merge: retain completions and best scores from either side.
    /// Counts use MAX (not SUM): both devices can share earlier attempts.
    /// Offline increments on independent devices cannot be totaled exactly
    /// without per-device counters, which are outside GameDataDynamic.
    /// </summary>
    public static GameDataDynamic[] Merge(GameDataDynamic[] local, GameDataDynamic[] cloud)
    {
        GameDataDynamic[] a = Normalize(local);
        GameDataDynamic[] b = Normalize(cloud);
        int size = Math.Max(a.Length, b.Length);
        var merged = new GameDataDynamic[size];
        for (int i = 0; i < size; i++)
        {
            var l = i < a.Length ? a[i] : null;
            var c = i < b.Length ? b[i] : null;
            merged[i] = new GameDataDynamic(i + 1,
                BestPositive(l == null ? 0 : l.BestAttempt,
                             c == null ? 0 : c.BestAttempt),
                Math.Max(l == null ? 0 : l.NumberOfGamesPlayed,
                         c == null ? 0 : c.NumberOfGamesPlayed));
        }
        return Normalize(merged);
    }

    public static GameDataDynamic[] Clone(GameDataDynamic[] data) => Normalize(data);

    public static bool Same(GameDataDynamic[] left, GameDataDynamic[] right)
    {
        GameDataDynamic[] a = Normalize(left);
        GameDataDynamic[] b = Normalize(right);
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i].Id != b[i].Id || a[i].BestAttempt != b[i].BestAttempt ||
                a[i].NumberOfGamesPlayed != b[i].NumberOfGamesPlayed)
                return false;
        return true;
    }

    private static int BestPositive(int a, int b)
    {
        if (a <= 0) return b;
        if (b <= 0) return a;
        return Math.Min(a, b);
    }
}