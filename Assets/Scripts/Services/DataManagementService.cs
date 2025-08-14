using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using Zillneuron.Utilities;

public class DataManagementService : MonoSingleton<DataManagementService>
{
    public const string Key_NextGameId = "NextGameId";

    public async Task DeleteAllAsync()
    {
        await CloudSaveService.Instance.Data.Player.DeleteAllAsync();
    }

    public async Task<List<string>> GetAllKeys()
    {
        var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
        return keys.Select(m => m.Key).ToList();
    }

    public async Task<Dictionary<string, string>> SaveData(Dictionary<string, object> data)
    {
        return await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public async Task<Dictionary<string, string>> LoadData(HashSet<string> keys)
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
        return data.ToDictionary(m => m.Key, m => m.Value.Value.GetAsString());
    }
}
