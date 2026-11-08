using UnityEngine;

public class SettingsCloudSnapshot
{
    public SettingsData Data { get; }
    public string WriteLock { get; }
    public bool Exists { get; }

    public SettingsCloudSnapshot(SettingsData data, string writeLock, bool exists)
    {
        Data = SettingsData.Normalize(data);
        WriteLock = writeLock;
        Exists = exists;
    }
}