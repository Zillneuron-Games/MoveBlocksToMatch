using System;
using UnityEngine;
using Zillneuron.Utilities;
using UnityEngine.Scripting.APIUpdating;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// PlayerPrefs JSON persistence ONLY. Each UGS Player ID has its own save slot.
    /// This class does not sign in, merge progress, or call Cloud Save.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class DataStoreService : MonoSingleton<DataStoreService>
    {
        private const string LevelPrefix = "level_data_";
        private const string SettingsPrefix = "settings_data_";
        private const string InventoryPrefix = "inventory_data_";

        public string CurrentPlayerId
        {
            get;
            private set;
        }

        public void SelectAccount(string playerId)
        {
            CurrentPlayerId = string.IsNullOrEmpty(playerId) ? null : playerId;
        }

        // Existing level API and storage key are preserved exactly.
        public GameDataDynamic[] Load()
        {
            string key = ActiveKey(LevelPrefix);
            
            if (!PlayerPrefs.HasKey(key))
            {
                return Array.Empty<GameDataDynamic>();
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Local level JSON is empty.");
            }

            LevelsData envelope = JsonUtility.FromJson<LevelsData>(json);
            if (envelope == null || envelope.Levels == null)
            {
                throw new FormatException("Local level JSON has no Levels array.");
            }

            return ProgressRules.Normalize(envelope.Levels);
        }

        public void Save(GameDataDynamic[] levels)
        {
            string key = ActiveKey(LevelPrefix);

            PlayerPrefs.SetString(key, JsonUtility.ToJson(new LevelsData { Levels = ProgressRules.Normalize(levels) }));
            PlayerPrefs.Save();
        }

        public SettingsData LoadSettings()
        {
            string key = ActiveKey(SettingsPrefix);
            if (!PlayerPrefs.HasKey(key))
            {
                return new SettingsData();
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Local settings JSON is empty.");
            }

            SettingsData value = JsonUtility.FromJson<SettingsData>(json);
            if (value == null)
            {
                throw new FormatException("Local settings JSON is invalid.");
            }

            return SettingsData.Normalize(value);
        }

        public void SaveSettings(SettingsData data)
        {
            PlayerPrefs.SetString(ActiveKey(SettingsPrefix),JsonUtility.ToJson(SettingsData.Normalize(data)));
            PlayerPrefs.Save();
        }

        public InventoryData LoadInventory()
        {
            string key = ActiveKey(InventoryPrefix);
            if (!PlayerPrefs.HasKey(key))
            {
                return new InventoryData();
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Local inventory JSON is empty.");
            }

            InventoryData value = JsonUtility.FromJson<InventoryData>(json);
            if (value == null)
            {
                throw new FormatException("Local inventory JSON is invalid.");
            }

            return InventoryData.Normalize(value);
        }

        public void SaveInventory(InventoryData data)
        {
            PlayerPrefs.SetString(ActiveKey(InventoryPrefix),JsonUtility.ToJson(InventoryData.Normalize(data)));
            PlayerPrefs.Save();
        }

        public bool HasSave(string playerId)
        {
            return PlayerPrefs.HasKey(KeyFor(LevelPrefix, playerId));
        }

        public bool HasSettings(string playerId)
        {
            return PlayerPrefs.HasKey(KeyFor(SettingsPrefix, playerId));
        }

        public bool HasInventory(string playerId)
        {
            return PlayerPrefs.HasKey(KeyFor(InventoryPrefix, playerId));
        }

        /// <summary>Delete ALL save categories for this guest identity.</summary>
        public void Delete(string playerId)
        {
            PlayerPrefs.DeleteKey(KeyFor(LevelPrefix, playerId));
            PlayerPrefs.DeleteKey(KeyFor(SettingsPrefix, playerId));
            PlayerPrefs.DeleteKey(KeyFor(InventoryPrefix, playerId));
            PlayerPrefs.Save();
        }

        private string ActiveKey(string prefix)
        {
            if (string.IsNullOrEmpty(CurrentPlayerId))
            {
                throw new InvalidOperationException("No active account is selected.");
            }

            return KeyFor(prefix, CurrentPlayerId);
        }

        private static string KeyFor(string prefix, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
            }

            return prefix + playerId;
        }
    }
}
