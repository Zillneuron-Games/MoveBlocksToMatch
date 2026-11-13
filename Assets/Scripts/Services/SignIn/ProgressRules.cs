using System;
using System.Collections.Generic;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Rules for GameDataDynamic[]: levels are 1-based, sequential and gap-free.
    /// Zero and int.MaxValue are both uncompleted scores.
    /// No extra last-unlocked-level field is stored: it is derived from the array.
    /// </summary>
    public static class ProgressRules
    {
        public static GameDataDynamic[] Normalize(GameDataDynamic[] source)
        {
            return Normalize(source, 0);
        }

        public static GameDataDynamic[] Normalize(GameDataDynamic[] source, int minimumLevelCount)
        {
            var byId = new SortedDictionary<int, GameDataDynamic>();
            foreach (GameDataDynamic entry in source ?? Array.Empty<GameDataDynamic>())
            {
                if (entry == null || entry.Id < 1 || entry.BestAttempt < 0 || entry.NumberOfGamesPlayed < 0)
                {
                    throw new FormatException("Invalid level progress entry.");
                }

                if (!byId.TryGetValue(entry.Id, out GameDataDynamic existing))
                {
                    byId.Add(entry.Id, new GameDataDynamic(entry.Id, entry.BestAttempt, entry.NumberOfGamesPlayed));
                }
                else
                {
                    existing.BestAttempt = BestPositive(existing.BestAttempt, entry.BestAttempt);
                    existing.NumberOfGamesPlayed = Math.Max(existing.NumberOfGamesPlayed, entry.NumberOfGamesPlayed);
                }
            }

            var result = new List<GameDataDynamic>(Math.Max(byId.Count, minimumLevelCount));
            int expectedId = 1;
            bool foundIncomplete = false;

            foreach (GameDataDynamic value in byId.Values)
            {
                if (value.Id != expectedId++)
                {
                    throw new FormatException("Level data contains a gap in sequential progression.");
                }

                bool hasActivity = value.BestAttempt != 0 || value.NumberOfGamesPlayed != 0;
                if (foundIncomplete && hasActivity)
                {
                    throw new FormatException("No level may follow an uncompleted level.");
                }

                if (value.BestAttempt == int.MaxValue && value.Id != 1 ||
                    value.BestAttempt > 0 && value.NumberOfGamesPlayed == 0)
                {
                    throw new FormatException("Invalid best attempt for this level.");
                }

                if (value.Id == 1 && value.BestAttempt == 0 && minimumLevelCount > 0)
                {
                    value.BestAttempt = int.MaxValue;
                    value.NumberOfGamesPlayed = Math.Max(1, value.NumberOfGamesPlayed);
                }

                foundIncomplete = foundIncomplete || !IsCompleted(value);
                result.Add(value);
            }

            for (int i = result.Count; i < minimumLevelCount; i++)
            {
                result.Add(i == 0 ? new GameDataDynamic(1, int.MaxValue, 1) : new GameDataDynamic(i + 1, 0, 0));
            }

            return result.ToArray();
        }

        public static int LastUnlockedLevel(GameDataDynamic[] data)
        {
            GameDataDynamic[] clean = Normalize(data);
            return FindLastUnlockedLevel(clean);
        }

        private static int FindLastUnlockedLevel(GameDataDynamic[] levels)
        {
            foreach (GameDataDynamic level in levels)
            {
                if (!IsCompleted(level))
                {
                    return level.Id;
                }
            }

            return levels.Length + 1;
        }

        private static bool IsCompleted(GameDataDynamic level)
        {
            return level.BestAttempt > 0 && level.BestAttempt < int.MaxValue;
        }

        public static bool HasProgress(GameDataDynamic[] levels)
        {
            foreach (GameDataDynamic level in levels)
            {
                if (IsCompleted(level) || level.NumberOfGamesPlayed > (level.Id == 1 ? 1 : 0))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanPlay(GameDataDynamic[] data, int levelId)
        {
            return levelId >= 1 && levelId <= LastUnlockedLevel(data);
        }

        /// <summary>
        /// Record a completion and update the best step count for an unlocked level.
        /// </summary>
        public static GameDataDynamic[] RecordAttempt(GameDataDynamic[] data, int levelId, int steps = 0)
        {
            if (levelId < 1 || steps <= 0 || steps == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(levelId), "Invalid level or completed step count.");
            }

            GameDataDynamic[] clean = Normalize(data);
            if (levelId > FindLastUnlockedLevel(clean))
            {
                throw new InvalidOperationException("Cannot play a locked level.");
            }

            var list = new List<GameDataDynamic>(clean);
            if (levelId == clean.Length + 1)
            {
                list.Add(new GameDataDynamic(levelId, 0, 0));
            }

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
                merged[i] = new GameDataDynamic(i + 1, BestPositive(l == null ? 0 : l.BestAttempt, c == null ? 0 : c.BestAttempt), Math.Max(l == null ? 0 : l.NumberOfGamesPlayed, c == null ? 0 : c.NumberOfGamesPlayed));
            }
            return Normalize(merged);
        }

        public static GameDataDynamic[] Clone(GameDataDynamic[] data)
        {
            return Normalize(data);
        }

        public static bool Same(GameDataDynamic[] left, GameDataDynamic[] right)
        {
            GameDataDynamic[] a = Normalize(left);
            GameDataDynamic[] b = Normalize(right);
            
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Id != b[i].Id || a[i].BestAttempt != b[i].BestAttempt || a[i].NumberOfGamesPlayed != b[i].NumberOfGamesPlayed)
                {
                    return false;
                }
            }

            return true;
        }

        private static int BestPositive(int a, int b)
        {
            if (a <= 0)
            {
                return b;
            }

            if (b <= 0)
            {
                return a;
            }

            return Math.Min(a, b);
        }
    }
}
