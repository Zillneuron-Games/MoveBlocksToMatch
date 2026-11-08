using System;

/// <summary>
/// Independent player preferences. Change timestamps are sync metadata;
/// update values using AccountManager rather than editing returned copies.
/// </summary>
[Serializable]
public sealed class SettingsData
{
    public bool SoundEnabled = true;
    public bool MusicEnabled = true;
    public int LanguageCode = 0;

    public long SoundChangedAtUtcTicks;
    public long MusicChangedAtUtcTicks;
    public long LanguageChangedAtUtcTicks;

    public static SettingsData Normalize(SettingsData source)
    {
        if (source == null) return new SettingsData();
        if (source.SoundChangedAtUtcTicks < 0 ||
            source.MusicChangedAtUtcTicks < 0 ||
            source.LanguageChangedAtUtcTicks < 0)
            throw new FormatException("Invalid settings timestamps.");
        if (source.LanguageCode < 0 || source.LanguageCode >= Enum.GetNames(typeof(ELanguageCode)).Length)
            throw new FormatException("Invalid language identifier.");

        return new SettingsData
        {
            SoundEnabled = source.SoundEnabled,
            MusicEnabled = source.MusicEnabled,
            LanguageCode = source.LanguageCode,
            SoundChangedAtUtcTicks = source.SoundChangedAtUtcTicks,
            MusicChangedAtUtcTicks = source.MusicChangedAtUtcTicks,
            LanguageChangedAtUtcTicks = source.LanguageChangedAtUtcTicks
        };
    }

    public static bool Same(SettingsData left, SettingsData right)
    {
        SettingsData a = Normalize(left);
        SettingsData b = Normalize(right);
        return a.SoundEnabled == b.SoundEnabled &&
               a.MusicEnabled == b.MusicEnabled &&
               a.LanguageCode == b.LanguageCode &&
               a.SoundChangedAtUtcTicks == b.SoundChangedAtUtcTicks &&
               a.MusicChangedAtUtcTicks == b.MusicChangedAtUtcTicks &&
               a.LanguageChangedAtUtcTicks == b.LanguageChangedAtUtcTicks;
    }

    public static bool IsDefault(SettingsData value) => Same(value, new SettingsData());

    /// <summary>Merge each preference independently using local change timestamps.</summary>
    public static SettingsData Merge(SettingsData left, SettingsData right)
    {
        SettingsData a = Normalize(left);
        SettingsData b = Normalize(right);
        SettingsData result = Normalize(a);

        if (PreferRight(a.SoundChangedAtUtcTicks, b.SoundChangedAtUtcTicks,
            a.SoundEnabled ? 1 : 0, b.SoundEnabled ? 1 : 0))
        {
            result.SoundEnabled = b.SoundEnabled;
            result.SoundChangedAtUtcTicks = b.SoundChangedAtUtcTicks;
        }
        if (PreferRight(a.MusicChangedAtUtcTicks, b.MusicChangedAtUtcTicks,
            a.MusicEnabled ? 1 : 0, b.MusicEnabled ? 1 : 0))
        {
            result.MusicEnabled = b.MusicEnabled;
            result.MusicChangedAtUtcTicks = b.MusicChangedAtUtcTicks;
        }
        if (b.LanguageChangedAtUtcTicks > a.LanguageChangedAtUtcTicks ||
            (b.LanguageChangedAtUtcTicks == a.LanguageChangedAtUtcTicks &&
             b.LanguageCode != a.LanguageCode))
        {
            result.LanguageCode = b.LanguageCode;
            result.LanguageChangedAtUtcTicks = b.LanguageChangedAtUtcTicks;
        }
        return result;
    }

    public static long NextTimestamp(long previousTicks) =>
        Math.Max(DateTime.UtcNow.Ticks, checked(previousTicks + 1));

    private static bool PreferRight(long leftTime, long rightTime, int left, int right) =>
        rightTime > leftTime || (rightTime == leftTime && right > left);
}
