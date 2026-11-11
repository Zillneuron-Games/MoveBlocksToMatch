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
        private bool hooked;
        private bool changingAccount;
        private bool syncing;
        private bool startupSyncAttempted;
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
                return ready && !changingAccount && !signInService.HasPendingLocalAccountChange;
            }
        }

        // Unused by the current project.
        // public bool IsSyncing
        // {
        //     get
        //     {
        //         return syncing;
        //     }
        // }

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
                return IsReady && !signInService.IsBusy && signInService.IsAuthorized && string.Equals(signInService.PlayerId, PlayerId, StringComparison.Ordinal);
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
                return ProgressRules.LastUnlockedLevel(levels);
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
                return inventory.HintsCount;
            }
        }

        public int NextLevel
        {
            get
            {
                return PlayerPrefs.GetInt(NextLevelId, LastUnlockedLevel);
            }
        }

        public GameDataDynamic GetLevelData(int levelId)
        {
            return Levels.FirstOrDefault(m => m.Id == levelId);
        }

        public void SetNextGameId(int gameId)
        {
            PlayerPrefs.SetInt(NextLevelId, gameId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Startup: restore a cached identity or a previously chosen local guest.
        /// A fresh installation stays unselected (NeedsWelcome = true) until a
        /// button explicitly calls PlayAsGuestAsync or LogInAsync.
        /// </summary>
        public async Task InitializeAsync()
        {
            await operationGate.WaitAsync();
            changingAccount = true;
            try
            {
                signInService = SignInService.Instance;
                localStoreService = DataStoreService.Instance;
                cloudSyncService = DataSyncService.Instance;
                HookSignInOnce();
                if (signInService.HasPendingLocalAccountChange)
                {
                    signInService.ApplyPendingLocalChange();
                }

                Exception authError = null;
                try
                {
                    await signInService.InitializeAsync();
                }
                catch (Exception error)
                {
                    authError = error;
                }

                if (authError == null && !string.IsNullOrEmpty(signInService.PlayerId))
                {
                    SelectIdentity(signInService.PlayerId, signInService.IsGuest);
                }
                else if (PlayerPrefs.HasKey(LastAccountKey))
                {
                    string lastId = PlayerPrefs.GetString(LastAccountKey, string.Empty);
                    if (authError != null && signInService.IsAuthorized && !string.Equals(signInService.PlayerId, lastId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Authenticated a different player before account restoration finished.", authError);
                    }

                    // Local saves can still be loaded if UGS is unavailable,
                    // or when a previous offline guest has no UGS session yet.
                    if (authError != null)
                    {
                        Debug.LogWarning("UGS unavailable, loading local account: " + authError.Message);
                    }

                    RestoreLastLocalIdentity();
                }
                else
                {
                    // Crucial: do NOT create a guest before the welcome choice.
                    localStoreService.SelectAccount(null);
                    levels = Array.Empty<GameDataDynamic>();
                    settings = SettingsData.CreateInitial();
                    inventory = InventoryData.CreateInitial();
                    ready = false;
                }

                startupInitialized = true;
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }

            // Reconcile before the loading view reads the selected player's settings.
            if (!startupSyncAttempted && IsCloudAvailable)
            {
                startupSyncAttempted = true;
                await SyncAsync();
            }
        }

        /// <summary>
        /// Welcome: Play as Guest. Creates a UGS anonymous account when available,
        /// but never blocks offline play if Unity Services cannot connect.
        /// </summary>
        public async Task PlayAsGuestAsync(CancellationToken cancellationToken = default)
        {
            await operationGate.WaitAsync(cancellationToken);
            changingAccount = true;
            try
            {
                RequireStartup();
                if (ready)
                {
                    if (IsGuest)
                    {
                        return;
                    }

                    throw new InvalidOperationException("A linked account must log out first.");
                }

                try
                {
                    await signInService.InitializeAsync();
                    if (signInService.IsLinked)
                    {
                        throw new InvalidOperationException("A linked account is already active.");
                    }

                    if (signInService.IsGuest)
                    {
                        SelectIdentity(signInService.PlayerId, nextIsGuest: true);
                    }
                    else
                    {
                        await signInService.StartNewGuestAsync();
                    }
                }
                catch (Exception error) when (!signInService.IsAuthorized && !signInService.HasPendingLocalAccountChange)
                {
                    Debug.LogWarning("Offline guest selected: " + error.Message);
                    SelectIdentity(OfflineGuestId, nextIsGuest: true);
                }
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }
        }

        /// <summary>
        /// The ONE Log In method for Welcome and Settings. Guest progress stays
        /// attached to the same Player ID if linking succeeds. If the provider
        /// already belongs to another UGS account, SignInService invokes the
        /// confirmation callback BEFORE replacing the current guest identity.
        /// On confirmation the previous guest slot is discarded, never merged.
        /// The callback should open a popup and asynchronously return true/false.
        /// </summary>
        public async Task<LoginOutcome> LogInAsync(Func<Task<bool>> confirmReplaceGuestProgress = null, CancellationToken cancellationToken = default)
        {
            await operationGate.WaitAsync(cancellationToken);
            changingAccount = true;
            LoginOutcome result;
            try
            {
                RequireStartup();
                if (IsLinked)
                {
                    throw new InvalidOperationException("Log out before logging into another account.");
                }

                // On a device with no UGS session (Welcome or offline guest),
                // initialize first. For an offline guest, create a UGS guest
                // identity and migrate that local slot BEFORE linking Google/Apple.
                await signInService.InitializeAsync();
                if (ready && PlayerId == OfflineGuestId && !signInService.IsGuest)
                {
                    await signInService.StartNewGuestAsync();
                }

                // A freshly created guest with no levels, preferences, or hints
                // does not need a destructive-change dialog. Meaningful guest
                // data always requires the caller-provided confirmation.
                Func<Task<bool>> confirmation = confirmReplaceGuestProgress;
                if (IsGuest && levels.Length == 0 &&
                    SettingsData.Same(settings, SettingsData.CreateInitial()) &&
                    inventory.HintsChangedAtUtcTicks == 0 &&
                    inventory.HintsCount == SettingsManager.Instance.Settings.StartingHints)
                {
                    confirmation = () =>
                    {
                        return Task.FromResult(true);
                    };
                }

                result = await signInService.LogInAsync(confirmation, cancellationToken);
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }

            // Await the first sync so an existing account's cloud levels/settings/
            // inventory are loaded BEFORE the UI navigates to Home. Failure does
            // not erase the local save and SyncAsync reports it via its event.
            if (result == LoginOutcome.SignedIn || result == LoginOutcome.SwitchedToExisting)
            {
                await SyncAsync();
            }
            else if (result == LoginOutcome.GuestLinked)
            {
                await SyncAsync(pendingOnly: true);
            }
            return result;
        }

        /// <summary>
        /// Settings: Log Out (linked accounts only). Keep that account's local
        /// records and pending changes, clear its credentials, then start a fresh
        /// guest. Offline fallback creates a fresh local-only guest.
        /// </summary>
        public async Task LogOutAsync()
        {
            RequireStartup();
            if (!IsLinked)
            {
                throw new InvalidOperationException("Log Out is only available for linked accounts.");
            }

            // Best-effort flush, without blocking logout if cloud is unavailable.
            // Unsynced changes remain stored under the old linked Player ID.
            if (IsCloudAvailable && HasPendingUpload)
            {
                await SyncAsync(pendingOnly: true);
            }

            await operationGate.WaitAsync();
            changingAccount = true;
            try
            {
                RequireSelected();
                if (!IsLinked)
                {
                    throw new InvalidOperationException("The selected account changed.");
                }

                signInService.SignOut(); // Clear UGS and Player Accounts tokens.
                try
                {
                    await signInService.StartNewGuestAsync();
                }
                catch (Exception error) when (!signInService.HasPendingLocalAccountChange && !signInService.IsAuthorized)
                {
                    // The linked session was already cleared successfully. Play
                    // can continue offline; a real UGS guest is created later.
                    Debug.LogWarning("New anonymous UGS account unavailable: " + error.Message);
                    localStoreService.Delete(OfflineGuestId); // Truly fresh guest, not a prior offline save.
                    MarkPending(OfflineGuestId, false);
                    SelectIdentity(OfflineGuestId, nextIsGuest: true);
                }
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }
        }

        public void RecordAttempt(int levelId, int steps = 0)
        {
            RequirePlayable();
            GameDataDynamic[] updated = ProgressRules.RecordAttempt(levels, levelId, steps);
            PersistCurrent(updated);
        }

        // Unused by the current project.
        // public bool CanPlay(int levelId)
        // {
        //     return IsReady && ProgressRules.CanPlay(levels, levelId);
        // }

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
        /// Link the current anonymous UGS identity. Same Player ID; local data retained.
        /// </summary>
        public async Task LinkGuestAsync(CancellationToken cancellationToken = default)
        {
            await operationGate.WaitAsync(cancellationToken);
            changingAccount = true;
            try
            {
                RequireSelected();
                if (!IsCloudIdentitySelected())
                {
                    throw new InvalidOperationException("Reconnect authentication first.");
                }

                await signInService.LinkGuestAsync(cancellationToken);
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }
            await SyncAsync(pendingOnly: true);
        }


        /// <summary>
        /// Switch to EXISTING Unity Player Account, without transferring guest data.
        /// SignInService triggers a synchronous save-slot switch through its callback.
        /// </summary>
        public async Task<bool> SwitchAccountAsync(bool confirmedGuestLoss, CancellationToken cancellationToken = default)
        {
            await operationGate.WaitAsync(cancellationToken);
            changingAccount = true;
            bool changed;
            try
            {
                RequireSelected();
                if (!IsCloudIdentitySelected())
                {
                    throw new InvalidOperationException("Reconnect authentication first.");
                }

                changed = await signInService.SwitchAccountAsync(confirmedGuestLoss, cancellationToken);
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }
            if (changed)
            {
                await SyncAsync();
            }

            return changed;
        }


        /// <summary>
        /// Logs out and immediately starts a fresh guest identity.
        /// Local linked-account saves are retained in separate slots.
        /// Guest progress is deleted after confirmation.
        /// </summary>
        public async Task SignOutAndStartGuestAsync(bool confirmedGuestLoss)
        {
            if (IsLinked)
            {
                await LogOutAsync();
                return;
            }

            // Legacy API: explicitly resetting an existing guest is destructive.
            if (!confirmedGuestLoss)
            {
                throw new InvalidOperationException("Confirm guest progress loss first.");
            }

            await operationGate.WaitAsync();
            changingAccount = true;
            try
            {
                RequireSelected();
                signInService.SignOut(confirmedGuestLoss: true);
                try
                {
                    await signInService.StartNewGuestAsync();
                }
                catch (Exception error) when (!signInService.HasPendingLocalAccountChange && !signInService.IsAuthorized)
                {
                    Debug.LogWarning("New guest is offline: " + error.Message);
                    localStoreService.Delete(OfflineGuestId);
                    MarkPending(OfflineGuestId, false);
                    SelectIdentity(OfflineGuestId, nextIsGuest: true);
                }
            }
            finally
            {
                changingAccount = false;
                operationGate.Release();
            }
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

        private async Task<bool> SyncAsync(bool pendingOnly)
        {
            await operationGate.WaitAsync();

            syncing = true;
            
            try
            {
                if (!IsCloudAvailable)
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
                    bool writeLevels = levelCloud.Exists ? !ProgressRules.Same(levelCloud.Levels, sendingLevels) : sendingLevels.Length != 0;
                    
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
                    bool levelsMatch = checkedLevels.Exists ? ProgressRules.Same(checkedLevels.Levels, sendingLevels) : sendingLevels.Length == 0;
                    if (!levelsMatch)
                    {
                        continue;
                    }

                    SettingsCloudSnapshot checkedSettings = await cloudSyncService.DownloadSettingsAsync();
                    RequireSameCloudIdentity(identity);
                    bool settingsMatch = checkedSettings.Exists ? SettingsData.Same(checkedSettings.Data, sendingSettings) : SettingsData.IsDefault(sendingSettings) && SettingsData.IsDefault(checkedSettings.Data);
                    if (!settingsMatch)
                    {
                        continue;
                    }

                    InventoryCloudSnapshot checkedInventory = await cloudSyncService.DownloadInventoryAsync();
                    RequireSameCloudIdentity(identity);
                    bool inventoryMatch = checkedInventory.Exists ? InventoryData.Same(checkedInventory.Data, sendingInventory) : InventoryData.IsDefault(sendingInventory) && InventoryData.IsDefault(checkedInventory.Data);
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

        private void HookSignInOnce()
        {
            if (hooked)
            {
                return;
            }

            if (signInService.ApplyLocalAccountChange != null)
            {
                throw new InvalidOperationException("Another component already owns SignInService.ApplyLocalAccountChange. Remove the old progress/authentication integration.");
            }

            signInService.ApplyLocalAccountChange = OnIdentityChanging;
            signInService.GuestLinked += OnGuestLinked;

            hooked = true;
        }

        private void OnGuestLinked(string playerId)
        {
            if (!string.Equals(PlayerId, playerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Linked account does not match local slot.");
            }

            SaveLastIdentity(playerId, wasGuest: false);
            
            OnAccountChanged?.Invoke(playerId);
        }

        private void OnIdentityChanging(PlayerIdentityChange change)
        {
            // This synchronous callback executes only during the serialized
            // AccountService account-operation, before gameplay resumes.
            if (!changingAccount)
            {
                throw new InvalidOperationException("Use AccountService to switch accounts, not SignInService directly.");
            }

            if (string.IsNullOrEmpty(change.CurrentPlayerId))
            {
                // Explicit sign-out: discard previous guest, keep linked saves.
                if (change.DiscardPreviousGuestProgress && !string.IsNullOrEmpty(change.PreviousPlayerId))
                {
                    DeleteGuestSlot(change.PreviousPlayerId);
                }

                localStoreService.SelectAccount(null);
                levels = Array.Empty<GameDataDynamic>();
                settings = SettingsData.CreateInitial();
                inventory = InventoryData.CreateInitial();
                ready = false;

                PlayerPrefs.DeleteKey(LastAccountKey);
                PlayerPrefs.DeleteKey(LastWasGuestKey);
                PlayerPrefs.Save();

                OnAccountChanged?.Invoke(null);
                OnLevelsChanged?.Invoke(Levels);
                OnSettingsChanged?.Invoke(Settings);
                OnInventoryChanged?.Invoke(Inventory);

                return;
            }

            SelectIdentity(change.CurrentPlayerId, change.CurrentIsGuest);
        }

        private void SelectIdentity(string nextId, bool nextIsGuest)
        {
            if (string.IsNullOrEmpty(nextId))
            {
                throw new ArgumentException("Cannot select an empty Player ID.", nameof(nextId));
            }

            string lastId = PlayerPrefs.GetString(LastAccountKey, string.Empty);
            bool lastWasGuest = PlayerPrefs.GetInt(LastWasGuestKey, 0) == 1;
            bool different = !string.Equals(lastId, nextId, StringComparison.Ordinal);

            // Offline-only first launch may contain attempts created before UGS
            // assigned a guest Player ID. Carry only that temporary guest forward.
            GameDataDynamic[] offlineToMigrate = null;
            SettingsData offlineSettingsToMigrate = null;
            InventoryData offlineInventoryToMigrate = null;
            
            if (different && lastId == OfflineGuestId && nextIsGuest &&
                (localStoreService.HasSave(OfflineGuestId) ||
                 localStoreService.HasSettings(OfflineGuestId) ||
                 localStoreService.HasInventory(OfflineGuestId)))
            {
                localStoreService.SelectAccount(OfflineGuestId);
                if (localStoreService.HasSave(OfflineGuestId))
                {
                    offlineToMigrate = localStoreService.Load();
                }

                if (localStoreService.HasSettings(OfflineGuestId))
                {
                    offlineSettingsToMigrate = localStoreService.LoadSettings();
                }

                if (localStoreService.HasInventory(OfflineGuestId))
                {
                    offlineInventoryToMigrate = localStoreService.LoadInventory();
                }
            }

            localStoreService.SelectAccount(nextId);

            bool hasSettings = localStoreService.HasSettings(nextId);
            bool initializeInventory = different && lastId != OfflineGuestId && !localStoreService.HasSave(nextId) && !hasSettings && !localStoreService.HasInventory(nextId);
            GameDataDynamic[] destination = localStoreService.Load();
            SettingsData settingsDestination = localStoreService.LoadSettings();
            InventoryData inventoryDestination = localStoreService.LoadInventory();
            
            if (offlineToMigrate != null)
            {
                destination = ProgressRules.Merge(destination, offlineToMigrate);
                localStoreService.Save(destination);
                MarkPending(nextId, true);
            }
            
            if (offlineSettingsToMigrate != null)
            {
                settingsDestination = SettingsData.Merge(settingsDestination, offlineSettingsToMigrate);
                localStoreService.SaveSettings(settingsDestination);
                MarkPending(nextId, true);
            }
            
            if (offlineInventoryToMigrate != null)
            {
                inventoryDestination = InventoryData.Merge(inventoryDestination, offlineInventoryToMigrate);
                localStoreService.SaveInventory(inventoryDestination);
                MarkPending(nextId, true);
            }
            else if (initializeInventory)
            {
                inventoryDestination.HintsCount = SettingsManager.Instance.Settings.StartingHints;
                if (!InventoryData.IsDefault(inventoryDestination))
                {
                    SetPending(nextId, true);
                }

                localStoreService.SaveInventory(inventoryDestination);
            }

            if (!hasSettings && offlineSettingsToMigrate == null)
            {
                SaveInitialSettings(settingsDestination);
            }

            // Guest -> existing account: NEVER import guest progress into destination.
            // The same cleanup also covers an interrupted switch after relaunch.
            if (different && lastWasGuest && !string.IsNullOrEmpty(lastId))
            {
                DeleteGuestSlot(lastId);
            }

            levels = destination;
            settings = settingsDestination;
            inventory = inventoryDestination;
            ready = true;

            SaveLastIdentity(nextId, nextIsGuest);
            
            OnAccountChanged?.Invoke(nextId);
            OnLevelsChanged?.Invoke(Levels);
            OnSettingsChanged?.Invoke(Settings);
            OnInventoryChanged?.Invoke(Inventory);
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

            inventory = localStoreService.LoadInventory();
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
            return signInService.IsAuthorized && !signInService.HasPendingLocalAccountChange && string.Equals(signInService.PlayerId, PlayerId, StringComparison.Ordinal);
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
            
            if (changingAccount || signInService.IsBusy || signInService.HasPendingLocalAccountChange)
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
