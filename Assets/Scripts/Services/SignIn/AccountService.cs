using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Zillneuron.Utilities;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Coordinates the selected player’s progress, settings, and inventory.
    /// SignInService owns identity; DataStoreService owns PlayerPrefs JSON;
    /// DataSyncService owns Cloud Save requests; ProgressRules owns progression/merge.
    /// Public async methods should be called from the Unity main thread.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class AccountService : MonoSingleton<AccountService>
    {
        private readonly SemaphoreSlim operationGate = new SemaphoreSlim(1, 1);

        private const string LastAccountKey = "account_lastId";
        private const string LastWasGuestKey = "account_wasGuest";
        private const string PendingPrefix = "account_pending_";
        private const string OfflineGuestId = "offline_guest_local_only";
        private const string RegistrationPrefix = "account_registering_";
        private const string HintPurchasePrefix = "hint_purchase_";
        private const string NextLevelId = "account_nextGameId";


        public event Action<GameDataDynamic[]> OnLevelsChanged;
        public event Action<SettingsData> OnSettingsChanged;
        public event Action<InventoryData> OnInventoryChanged;
        public event Action<string> OnAccountChanged;
        public event Action<Exception> OnSynchronizationFailed;


        private SignInService signInService;
        private DataStoreService localStoreService;
        private DataSyncService cloudSyncService;

        private GameDataDynamic[] levels;
        private SettingsData settings;
        private InventoryData inventory;
        private bool ready;
        private bool startupInitialized;
        private bool changingAccount;
        private bool syncing;
        private bool hintPurchasePending;
        private bool pauseSyncAttempted;
        public string PlayerId
        {
            get
            {
                return localStoreService.CurrentPlayerId;
            }
        }
        public bool IsReady
        {
            get
            {
                return ready && !changingAccount;
            }
        }

        // Account type is also known while offline; it comes from the selected
        // local save owner rather than UGS network authorization status.
        public bool IsGuest
        {
            get
            {
                return ready && PlayerPrefs.GetInt(LastWasGuestKey, 0) == 1;
            }
        }

        public bool IsLinked
        {
            get
            {
                return ready && !IsGuest;
            }
        }

        // The Welcome page is needed only when no previous account choice exists.
        public bool NeedsWelcome
        {
            get
            {
                return !ready && !PlayerPrefs.HasKey(LastAccountKey);
            }
        }

        public bool IsCloudAvailable
        {
            get
            {
                return IsReady && IsLinked && IsCloudIdentitySelected();
            }
        }

        public bool HasPendingUpload
        {
            get
            {
                return ready && PlayerPrefs.GetInt(PendingKey(PlayerId), 0) == 1;
            }
        }

        public int LastUnlockedLevel
        {
            get
            {
                return Math.Min(ProgressRules.LastUnlockedLevel(levels), SettingsManager.Instance.Settings.LevelsCount);
            }
        }

        public GameDataDynamic[] Levels
        {
            get
            {
                return ProgressRules.Clone(levels);
            }
        }

        // Defensive copies. Update through the methods below so changes persist.
        public SettingsData Settings
        {
            get
            {
                return ready ? SettingsData.Normalize(settings) : SettingsData.CreateInitial();
            }
        }

        public InventoryData Inventory
        {
            get
            {
                return InventoryData.Normalize(inventory);
            }
        }

        public bool SoundEnabled
        {
            get
            {
                return ready ? settings.SoundEnabled : SettingsManager.Instance.Settings.SoundEnabled;
            }
        }

        public bool MusicEnabled
        {
            get
            {
                return ready ? settings.MusicEnabled : SettingsManager.Instance.Settings.MusicEnabled;
            }
        }

        public ELanguageCode LanguageCode
        {
            get
            {
                return ready ? (ELanguageCode)settings.LanguageCode : SettingsManager.Instance.Settings.Language;
            }
        }

        public int Hints
        {
            get
            {
                return Inventory.HintsCount;
            }
        }

        public int NextLevel
        {
            get
            {
                int lastUnlocked = LastUnlockedLevel;
                return Mathf.Clamp(PlayerPrefs.GetInt(NextLevelId + "." + PlayerId, lastUnlocked), 1, lastUnlocked);
            }
        }

        public GameDataDynamic GetLevelData(int levelId)
        {
            return Levels.FirstOrDefault(m => m.Id == levelId);
        }

        public void SetNextGameId(int gameId)
        {
            PlayerPrefs.SetInt(NextLevelId + "." + PlayerId, gameId);
            PlayerPrefs.Save();
        }

        /// <summary>Restore the last local profile, then reconcile a signed-in account when online.</summary>
        public async Task InitializeAsync()
        {
            await operationGate.WaitAsync();
            try
            {
                if (startupInitialized)
                {
                    return;
                }

                signInService = SignInService.Instance;
                localStoreService = DataStoreService.Instance;
                cloudSyncService = DataSyncService.Instance;
                string lastId = PlayerPrefs.GetString(LastAccountKey, string.Empty);
                if (!string.IsNullOrEmpty(lastId))
                {
                    RestoreLastLocalIdentity();
                }

                if (IsLinked)
                {
                    try
                    {
                        await signInService.RestoreSessionAsync(PlayerId);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Playing from the local account save: " + error.Message);
                    }
                }

                startupInitialized = true;
            }
            finally
            {
                operationGate.Release();
            }

            if (IsCloudAvailable)
            {
                await SyncAsync();
            }
        }

        /// <summary>Open a local guest slot without contacting Unity Services.</summary>
        public async Task PlayAsGuestAsync()
        {
            await operationGate.WaitAsync();
            try
            {
                RequireStartup();
                if (IsLinked)
                {
                    throw new InvalidOperationException("Sign out before playing as a guest.");
                }

                if (!ready)
                {
                    OpenGuestProfile();
                }
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Transfer a guest save on registration or merge only levels into a returning account.</summary>
        public async Task LogInAsync()
        {
            await operationGate.WaitAsync();
            try
            {
                RequireStartup();
                if (IsLinked)
                {
                    return;
                }

                RequireNoHintPurchase();
                changingAccount = true;
                string guestId = PlayerId;
                string accountId;
                GameDataDynamic[] accountLevels;
                SettingsData accountSettings;
                InventoryData accountInventory;
                try
                {
                    bool created = await signInService.SignInAsync();
                    accountId = signInService.PlayerId;
                    if (string.IsNullOrEmpty(accountId))
                    {
                        throw new InvalidOperationException("Sign-in did not return a player ID.");
                    }

                    string registrationKey = RegistrationPrefix + accountId;
                    if (created)
                    {
                        PlayerPrefs.SetInt(registrationKey, 1);
                        PlayerPrefs.Save();
                    }

                    bool registering = created || PlayerPrefs.HasKey(registrationKey);
                    LevelsCloudSnapshot cloudLevels = registering ? null : await cloudSyncService.DownloadAsync();
                    SettingsCloudSnapshot cloudSettings = registering ? null : await cloudSyncService.DownloadSettingsAsync();
                    InventoryCloudSnapshot cloudInventory = registering ? null : await cloudSyncService.DownloadInventoryAsync();

                    localStoreService.SelectAccount(accountId);
                    accountLevels = localStoreService.Load();
                    accountSettings = localStoreService.LoadSettings();
                    accountInventory = localStoreService.LoadInventory();
                    if (registering)
                    {
                        accountSettings = Settings;
                        accountInventory = ready ? Inventory : CreateStartingInventory();
                    }
                    else
                    {
                        accountLevels = ProgressRules.Merge(accountLevels, cloudLevels.Levels);
                        if (cloudSettings.Exists)
                        {
                            accountSettings = SettingsData.Merge(accountSettings, cloudSettings.Data);
                        }

                        if (cloudInventory.Exists)
                        {
                            accountInventory = InventoryData.Merge(accountInventory, cloudInventory.Data);
                        }
                    }

                    accountLevels = ProgressRules.Merge(accountLevels, levels);
                    localStoreService.Save(accountLevels);
                    localStoreService.SaveSettings(accountSettings);
                    localStoreService.SaveInventory(accountInventory);
                    SetPending(accountId, true);
                    PlayerPrefs.DeleteKey(registrationKey);
                }
                catch
                {
                    localStoreService.SelectAccount(guestId);
                    signInService.SignOut();
                    throw;
                }

                SelectProfile(accountId, false, accountLevels, accountSettings, accountInventory);
                if (!string.IsNullOrEmpty(guestId) && !string.Equals(guestId, PlayerId, StringComparison.Ordinal))
                {
                    DeleteGuestSlot(guestId);
                }
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }

            // Upload the merged save at this account checkpoint. A failed upload
            // leaves the local account and its pending flag available for retry.
            await SyncAsync(pendingOnly: true);
        }

        /// <summary>Keep the account save and start a fresh local guest.</summary>
        public async Task LogOutAsync()
        {
            RequireStartup();
            if (IsCloudAvailable && HasPendingUpload)
            {
                await SyncAsync(pendingOnly: true);
            }

            await operationGate.WaitAsync();
            try
            {
                if (!IsLinked)
                {
                    return;
                }

                RequireNoHintPurchase();
                string accountId = PlayerId;
                try
                {
                    DeleteGuestSlot(OfflineGuestId);
                    OpenGuestProfile();
                }
                catch
                {
                    localStoreService.SelectAccount(accountId);
                    throw;
                }

                signInService.SignOut();
            }
            finally
            {
                operationGate.Release();
            }
        }

        private void OpenGuestProfile()
        {
            localStoreService.SelectAccount(OfflineGuestId);
            GameDataDynamic[] guestLevels = localStoreService.Load();
            SettingsData guestSettings = localStoreService.LoadSettings();
            InventoryData guestInventory = localStoreService.HasInventory(OfflineGuestId)
                ? localStoreService.LoadInventory()
                : CreateStartingInventory();
            localStoreService.SaveSettings(guestSettings);
            localStoreService.SaveInventory(guestInventory);
            SelectProfile(OfflineGuestId, true, guestLevels, guestSettings, guestInventory);
        }

        private static InventoryData CreateStartingInventory()
        {
            return new InventoryData { HintsCount = SettingsManager.Instance.Settings.StartingHints };
        }

        private void SelectProfile(string id, bool isGuest, GameDataDynamic[] savedLevels, SettingsData savedSettings, InventoryData savedInventory)
        {
            SaveLastIdentity(id, isGuest);
            levels = savedLevels;
            settings = savedSettings;
            inventory = savedInventory;
            ready = true;
            pauseSyncAttempted = false;

            OnAccountChanged?.Invoke(id);
            OnLevelsChanged?.Invoke(Levels);
            OnSettingsChanged?.Invoke(Settings);
            OnInventoryChanged?.Invoke(Inventory);
        }


        public void RecordAttempt(int levelId, int steps = 0)
        {
            RequirePlayable();
            GameDataDynamic[] updated = ProgressRules.RecordAttempt(levels, levelId, steps);
            PersistCurrent(updated);
        }

        public void SetSoundEnabled(bool enabled)
        {
            RequirePlayable();
            if (settings.SoundEnabled == enabled)
            {
                return;
            }

            SettingsData updated = Settings;
            updated.SoundEnabled = enabled;
            updated.SoundChangedAtUtcTicks = SettingsData.NextTimestamp(updated.SoundChangedAtUtcTicks);
            PersistSettings(updated);
        }

        public void SetMusicEnabled(bool enabled)
        {
            RequirePlayable();
            if (settings.MusicEnabled == enabled)
            {
                return;
            }

            SettingsData updated = Settings;
            updated.MusicEnabled = enabled;
            updated.MusicChangedAtUtcTicks = SettingsData.NextTimestamp(updated.MusicChangedAtUtcTicks);

            PersistSettings(updated);
        }

        public void SetLanguage(ELanguageCode language)
        {
            RequirePlayable();

            SettingsData updated = Settings;
            updated.LanguageCode = (int)language;
            updated.LanguageChangedAtUtcTicks = SettingsData.NextTimestamp(updated.LanguageChangedAtUtcTicks);
            PersistSettings(updated);
        }

        public void SetHints(int value)
        {
            RequirePlayable();

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (inventory.HintsCount == value)
            {
                return;
            }

            InventoryData updated = Inventory;
            updated.HintsCount = value;
            updated.HintsChangedAtUtcTicks = InventoryData.NextTimestamp(updated.HintsChangedAtUtcTicks);
            PersistInventory(updated);
        }


        public void AddHints(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            RequirePlayable();
            SetHints(checked(inventory.HintsCount + amount));
        }


        internal bool TryBeginHintPurchase()
        {
            if (!IsReady || hintPurchasePending)
            {
                return false;
            }

            hintPurchasePending = true;
            return true;
        }

        internal void EndHintPurchase()
        {
            hintPurchasePending = false;
        }

        private void RequireNoHintPurchase()
        {
            if (hintPurchasePending)
            {
                throw new InvalidOperationException("Wait for the hint purchase to finish before switching accounts.");
            }
        }

        /// <summary>Save a store grant before the purchase callback returns to the store.</summary>
        internal void GrantPurchasedHints(int amount, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("A store transaction ID is required.", nameof(transactionId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            string purchaseKey = HintPurchasePrefix + transactionId;
            if (PlayerPrefs.HasKey(purchaseKey))
            {
                return;
            }

            string playerId = PlayerId;
            InventoryData updated = Inventory;
            updated.HintsCount = checked(updated.HintsCount + amount);
            updated.HintsChangedAtUtcTicks = InventoryData.NextTimestamp(updated.HintsChangedAtUtcTicks);

            // Flush the transaction marker and balance together with the pending flag.
            PlayerPrefs.SetInt(purchaseKey, 1);
            PersistInventory(updated);

            if (IsCloudAvailable)
            {
                _ = SyncAsync(pendingOnly: true, expectedPlayerId: playerId);
            }
        }

        public bool TrySpendHints(int amount = 1)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            RequirePlayable();
            if (inventory.HintsCount < amount)
            {
                return false;
            }

            SetHints(inventory.HintsCount - amount);
            return true;
        }


        /// <summary>
        /// Synchronize three independent records: levels, settings and inventory.
        /// Each record uses its own Cloud Save write lock. All three are verified
        /// before the account's pending-upload flag is cleared.
        /// </summary>
        public Task<bool> SyncAsync()
        {
            return SyncAsync(pendingOnly: false);
        }

        private async Task<bool> SyncAsync(bool pendingOnly, string expectedPlayerId = null)
        {
            await operationGate.WaitAsync();

            syncing = true;

            try
            {
                if (!IsCloudAvailable ||
                    (expectedPlayerId != null && !string.Equals(PlayerId, expectedPlayerId, StringComparison.Ordinal)))
                {
                    return false;
                }

                if (pendingOnly && !HasPendingUpload)
                {
                    return true;
                }

                string identity = PlayerId;

                int reconciliationLimit = SettingsManager.Instance.Settings.CloudReconciliationLimit;
                for (int attempt = 0; attempt < reconciliationLimit; attempt++)
                {
                    // 1. Level progress: keep the best valid sequential progress.
                    LevelsCloudSnapshot levelCloud = await cloudSyncService.DownloadAsync();
                    RequireSameCloudIdentity(identity);
                    GameDataDynamic[] mergedLevels = ProgressRules.Merge(levels, levelCloud.Levels);

                    if (!ProgressRules.Same(levels, mergedLevels))
                    {
                        PersistCurrent(mergedLevels);
                    }

                    GameDataDynamic[] sendingLevels = ProgressRules.Clone(levels);
                    bool writeLevels = levelCloud.Exists ? !ProgressRules.Same(levelCloud.Levels, sendingLevels) : ProgressRules.HasProgress(sendingLevels);

                    if (writeLevels)
                    {
                        try
                        {
                            await cloudSyncService.UploadAsync(sendingLevels, levelCloud);
                        }
                        catch (CloudSaveException error) when (string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        RequireSameCloudIdentity(identity);
                    }

                    // 2. Settings: merge sound, music and language independently.
                    SettingsCloudSnapshot settingsCloud = await cloudSyncService.DownloadSettingsAsync();

                    RequireSameCloudIdentity(identity);

                    if (settingsCloud.Exists)
                    {
                        SettingsData mergedSettings = SettingsData.Merge(settings, settingsCloud.Data);
                        if (!SettingsData.Same(settings, mergedSettings))
                        {
                            PersistSettings(mergedSettings);
                        }
                    }

                    SettingsData sendingSettings = Settings;
                    bool writeSettings = settingsCloud.Exists ? !SettingsData.Same(settingsCloud.Data, sendingSettings) : !SettingsData.IsDefault(sendingSettings);

                    if (writeSettings)
                    {
                        try
                        {
                            await cloudSyncService.UploadSettingsAsync(sendingSettings, settingsCloud);
                        }
                        catch (CloudSaveException error) when (
                            string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        RequireSameCloudIdentity(identity);
                    }

                    // 3. Inventory: independent last-change-wins hint balance.
                    InventoryCloudSnapshot inventoryCloud = await cloudSyncService.DownloadInventoryAsync();
                    RequireSameCloudIdentity(identity);
                    if (inventoryCloud.Exists)
                    {
                        // An existing cloud balance takes precedence over an untouched starter grant.
                        InventoryData mergedInventory = inventory.HintsChangedAtUtcTicks == 0
                            ? inventoryCloud.Data
                            : InventoryData.Merge(inventory, inventoryCloud.Data);
                        if (!InventoryData.Same(inventory, mergedInventory))
                        {
                            PersistInventory(mergedInventory);
                        }
                    }

                    InventoryData sendingInventory = Inventory;
                    bool writeInventory = inventoryCloud.Exists ? !InventoryData.Same(inventoryCloud.Data, sendingInventory) : !InventoryData.IsDefault(sendingInventory);

                    if (writeInventory)
                    {
                        try
                        {
                            await cloudSyncService.UploadInventoryAsync(sendingInventory, inventoryCloud);
                        }
                        catch (CloudSaveException error) when (string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        RequireSameCloudIdentity(identity);
                    }

                    // 4. Re-read and verify each key. This also detects non-atomic
                    // first-time creates and local changes during network requests.
                    LevelsCloudSnapshot checkedLevels = await cloudSyncService.DownloadAsync();
                    RequireSameCloudIdentity(identity);
                    bool levelsMatch = checkedLevels.Exists ? ProgressRules.Same(checkedLevels.Levels, sendingLevels) : !ProgressRules.HasProgress(sendingLevels);
                    if (!levelsMatch)
                    {
                        continue;
                    }

                    SettingsCloudSnapshot checkedSettings = await cloudSyncService.DownloadSettingsAsync();
                    RequireSameCloudIdentity(identity);
                    bool settingsMatch = checkedSettings.Exists
                        ? SettingsData.Same(checkedSettings.Data, sendingSettings)
                        : SettingsData.IsDefault(sendingSettings);
                    if (!settingsMatch)
                    {
                        continue;
                    }

                    InventoryCloudSnapshot checkedInventory = await cloudSyncService.DownloadInventoryAsync();
                    RequireSameCloudIdentity(identity);
                    bool inventoryMatch = checkedInventory.Exists
                        ? InventoryData.Same(checkedInventory.Data, sendingInventory)
                        : InventoryData.IsDefault(sendingInventory);
                    if (!inventoryMatch)
                    {
                        continue;
                    }

                    // Gameplay can save while network requests are in flight.
                    // Leave newer edits pending for the next checkpoint.
                    if (!ProgressRules.Same(levels, sendingLevels) ||
                        !SettingsData.Same(settings, sendingSettings) ||
                        !InventoryData.Same(inventory, sendingInventory))
                    {
                        return false;
                    }

                    MarkPending(identity, false);
                    return true;
                }
                Debug.LogWarning("Cloud data kept changing; retry synchronization later.");
                return false;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Player data synchronization failed: " + error);
                OnSynchronizationFailed?.Invoke(error);
                return false;
            }
            finally
            {
                syncing = false;
                operationGate.Release();
            }
        }

        private void RestoreLastLocalIdentity()
        {
            string id = PlayerPrefs.GetString(LastAccountKey, string.Empty);
            bool isGuest = PlayerPrefs.GetInt(LastWasGuestKey, 1) == 1;

            if (string.IsNullOrEmpty(id))
            {
                id = OfflineGuestId;
                isGuest = true;
            }

            localStoreService.SelectAccount(id);
            levels = localStoreService.Load();
            settings = localStoreService.LoadSettings();
            if (!localStoreService.HasSettings(id))
            {
                SaveInitialSettings(settings);
            }

            bool initializeGuestInventory = isGuest && !localStoreService.HasInventory(id);
            inventory = initializeGuestInventory ? CreateStartingInventory() : localStoreService.LoadInventory();
            if (initializeGuestInventory)
            {
                localStoreService.SaveInventory(inventory);
            }
            ready = true;

            SaveLastIdentity(id, isGuest);

            OnAccountChanged?.Invoke(id);
            OnLevelsChanged?.Invoke(Levels);
            OnSettingsChanged?.Invoke(Settings);
            OnInventoryChanged?.Invoke(Inventory);
        }

        private void SaveInitialSettings(SettingsData data)
        {
            if (!SettingsData.IsDefault(data))
            {
                SetPending(localStoreService.CurrentPlayerId, true);
            }

            localStoreService.SaveSettings(data);
        }

        // DataStoreService flushes the record and its pending flag together.
        private void PersistCurrent(GameDataDynamic[] levels)
        {
            SetPending(PlayerId, true);
            localStoreService.Save(levels);
            this.levels = ProgressRules.Clone(levels);

            OnLevelsChanged?.Invoke(Levels);
        }

        private void PersistSettings(SettingsData data)
        {
            SettingsData clean = SettingsData.Normalize(data);
            SetPending(PlayerId, true);
            localStoreService.SaveSettings(clean);
            settings = clean;

            OnSettingsChanged?.Invoke(Settings);
        }

        private void PersistInventory(InventoryData data)
        {
            InventoryData clean = InventoryData.Normalize(data);
            SetPending(PlayerId, true);
            localStoreService.SaveInventory(clean);
            inventory = clean;

            OnInventoryChanged?.Invoke(Inventory);
        }

        private static void SaveLastIdentity(string playerId, bool wasGuest)
        {
            PlayerPrefs.SetString(LastAccountKey, playerId);
            PlayerPrefs.SetInt(LastWasGuestKey, wasGuest ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void DeleteGuestSlot(string playerId)
        {
            localStoreService.Delete(playerId);

            PlayerPrefs.DeleteKey(PendingKey(playerId));
            PlayerPrefs.DeleteKey(NextLevelId + "." + playerId);
            PlayerPrefs.Save();
        }

        private static void MarkPending(string id, bool pending)
        {
            SetPending(id, pending);
            PlayerPrefs.Save();
        }

        private static void SetPending(string id, bool pending)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (pending)
            {
                PlayerPrefs.SetInt(PendingKey(id), 1);
            }
            else
            {
                PlayerPrefs.DeleteKey(PendingKey(id));
            }
        }

        private static string PendingKey(string id)
        {
            return PendingPrefix + id;
        }

        private bool IsCloudIdentitySelected()
        {
            return signInService.IsAuthorized && string.Equals(signInService.PlayerId, PlayerId, StringComparison.Ordinal);
        }

        private void RequireSameCloudIdentity(string id)
        {
            if (!IsCloudIdentitySelected() || !string.Equals(id, PlayerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authenticated account changed during sync.");
            }
        }

        private void RequireStartup()
        {
            if (!startupInitialized)
            {
                throw new InvalidOperationException("Call InitializeAsync at startup first.");
            }
        }

        private void RequireSelected()
        {
            if (!ready || string.IsNullOrEmpty(PlayerId))
            {
                throw new InvalidOperationException("Call InitializeAsync first.");
            }
        }

        private void RequirePlayable()
        {
            RequireSelected();

            if (changingAccount)
            {
                throw new InvalidOperationException("An account change is in progress.");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused || pauseSyncAttempted || syncing || !IsCloudAvailable || !HasPendingUpload)
            {
                return;
            }

            // Mobile suspension can interrupt the request. Local saves and the
            // pending flag let the next launch retry without waiting on shutdown.
            pauseSyncAttempted = true;
            Task<bool> result = SyncAsync(pendingOnly: true);
        }
    }
}
