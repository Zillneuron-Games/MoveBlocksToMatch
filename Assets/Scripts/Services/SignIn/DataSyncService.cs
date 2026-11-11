using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Authentication;
using UnityEngine;
using Zillneuron.Utilities;
using UnityEngine.Scripting.APIUpdating;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Reads and writes the selected player’s records in Unity Cloud Save.
    /// Cloud key holds exactly one JSON envelope with GameDataDynamic[] Levels.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class DataSyncService : MonoSingleton<DataSyncService>
    {
        public const string CloudKey = "level_data";
        public const string SettingsCloudKey = "settings_data";
        public const string InventoryCloudKey = "inventory_data";

        public async Task<LevelsCloudSnapshot> DownloadAsync()
        {
            RequireAuthenticated();
            var values = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { CloudKey });

            if (!values.TryGetValue(CloudKey, out var item))
            {
                return new LevelsCloudSnapshot(Array.Empty<GameDataDynamic>(), null, false);
            }

            string json = item.Value.GetAs<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Cloud progress JSON is empty.");
            }

            LevelsData envelope = JsonUtility.FromJson<LevelsData>(json);
            if (envelope == null || envelope.Levels == null)
            {
                throw new FormatException("Cloud progress JSON has no Levels array.");
            }

            return new LevelsCloudSnapshot(envelope.Levels, item.WriteLock, true);
        }

        public Task<string> UploadAsync(GameDataDynamic[] levels, LevelsCloudSnapshot baseline)
        {
            RequireAuthenticated();

            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }

            string json = JsonUtility.ToJson(new LevelsData
            {
                Levels = ProgressRules.Normalize(levels)
            });

            return UploadStringAsync(CloudKey, json, baseline.WriteLock, baseline.Exists);
        }

        public async Task<SettingsCloudSnapshot> DownloadSettingsAsync()
        {
            RequireAuthenticated();

            var values = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { SettingsCloudKey });
            if (values.TryGetValue(SettingsCloudKey, out var item))
            {
                string json = item.Value.GetAs<string>();
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new FormatException("Cloud settings JSON is empty.");
                }

                SettingsData data = JsonUtility.FromJson<SettingsData>(json);
                
                if (data == null)
                {
                    throw new FormatException("Cloud settings JSON is invalid.");
                }

                return new SettingsCloudSnapshot(data, item.WriteLock, true);
            }

            return new SettingsCloudSnapshot(new SettingsData(), null, false);
        }

        public Task<string> UploadSettingsAsync(SettingsData data, SettingsCloudSnapshot baseline)
        {
            RequireAuthenticated();

            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }

            string json = JsonUtility.ToJson(SettingsData.Normalize(data));

            return UploadStringAsync(SettingsCloudKey, json, baseline.WriteLock, baseline.Exists);
        }

        public async Task<InventoryCloudSnapshot> DownloadInventoryAsync()
        {
            RequireAuthenticated();

            var values = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { InventoryCloudKey });
            
            if (values.TryGetValue(InventoryCloudKey, out var item))
            {
                string json = item.Value.GetAs<string>();

                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new FormatException("Cloud inventory JSON is empty.");
                }

                InventoryData data = JsonUtility.FromJson<InventoryData>(json);

                if (data == null)
                {
                    throw new FormatException("Cloud inventory JSON is invalid.");
                }

                return new InventoryCloudSnapshot(data, item.WriteLock, true);
            }
            return new InventoryCloudSnapshot(new InventoryData(), null, false);
        }

        public Task<string> UploadInventoryAsync(InventoryData data, InventoryCloudSnapshot baseline)
        {
            RequireAuthenticated();
            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }

            string json = JsonUtility.ToJson(InventoryData.Normalize(data));
            return UploadStringAsync(InventoryCloudKey, json, baseline.WriteLock, baseline.Exists);
        }

        private static async Task<string> UploadStringAsync(string key, string json, string writeLock, bool exists)
        {
            Dictionary<string, string> updatedLocks;
            if (exists)
            {
                if (string.IsNullOrEmpty(writeLock))
                {
                    throw new InvalidOperationException("Existing cloud record has no write lock: " + key);
                }

                var items = new Dictionary<string, SaveItem>
                    {
                        { key, new SaveItem(json, writeLock) }
                    };
                updatedLocks = await CloudSaveService.Instance.Data.Player.SaveAsync(items);
            }
            else
            {
                // Cloud Save does not provide an atomic create-if-absent write
                // through the standard client API. AccountService re-verifies.
                var items = new Dictionary<string, object> { { key, json } };
                updatedLocks = await CloudSaveService.Instance.Data.Player.SaveAsync(items);
            }
            return updatedLocks.TryGetValue(key, out string value) ? value : null;
        }

        private static void RequireAuthenticated()
        {
            if (!AuthenticationService.Instance.IsAuthorized)
            {
                throw new InvalidOperationException("Unity Authentication is not authorized.");
            }
        }
    }
}
