using UnityEngine;

public class InventoryCloudSnapshot
{
    public InventoryData Data { get; }
    public string WriteLock { get; }
    public bool Exists { get; }

    public InventoryCloudSnapshot(InventoryData data, string writeLock, bool exists)
    {
        Data = InventoryData.Normalize(data);
        WriteLock = writeLock;
        Exists = exists;
    }
}
