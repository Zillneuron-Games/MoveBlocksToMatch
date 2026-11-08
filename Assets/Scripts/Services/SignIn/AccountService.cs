using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Zillneuron.Utilities;
using System.Linq;

/// <summary>
/// Single GAME-facing facade for account-owned progress.
/// SignInService owns identity; LocalDataManager owns PlayerPrefs JSON;
/// SyncDataManager owns Cloud Save requests; ProgressRules owns progression/merge.
/// Public async methods should be called from the Unity main thread.
/// </summary>
public class AccountService : MonoSingleton<AccountService>
{
    private const string LastAccountKey = "game.account.v3.lastId";
    private const string LastWasGuestKey = "game.account.v3.wasGuest";
    private const string PendingPrefix = "game.account.v3.pending.";
    private const string OfflineGuestId = "offline-guest-local-only";
    private const string NextLevelId = "game.account.v3.nextGameId";
    private const int MaxCloudAttempts = 4;

    private SignInService _signIn;
    private DataStoreService _local;
    private DataSyncService _cloud;
    private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);

    private GameDataDynamic[] _levels = Array.Empty<GameDataDynamic>();
    private SettingsData _settings = new SettingsData();
    private InventoryData _inventory = new InventoryData();
    private bool _ready;
    private bool _startupInitialized;
    private bool _hooked;
    private bool _changingAccount;
    private bool _syncing;

    public string PlayerId => _local.CurrentPlayerId;
    public bool IsReady => _ready && !_changingAccount &&
                           !_signIn.HasPendingLocalAccountChange;
    public bool IsSyncing => _syncing;
    // Account type is also known while offline; it comes from the selected
    // local save owner rather than UGS network authorization status.
    public bool IsGuest => _ready &&
        PlayerPrefs.GetInt(LastWasGuestKey, 0) == 1;
    public bool IsLinked => _ready && !IsGuest;
    // The Welcome page is needed only when no previous account choice exists.
    public bool NeedsWelcome => !_ready && !PlayerPrefs.HasKey(LastAccountKey);
    public bool IsCloudAvailable => IsReady && !_signIn.IsBusy &&
        _signIn.IsAuthorized &&
        string.Equals(_signIn.PlayerId, PlayerId, StringComparison.Ordinal);
    public bool HasPendingUpload => _ready && PlayerPrefs.GetInt(PendingKey(PlayerId), 0) == 1;
    public int LastUnlockedLevel => ProgressRules.LastUnlockedLevel(_levels);
    public GameDataDynamic[] Levels => ProgressRules.Clone(_levels);

    // Defensive copies. Update through the methods below so changes persist.
    public SettingsData Settings => SettingsData.Normalize(_settings);
    public InventoryData Inventory => InventoryData.Normalize(_inventory);
    public bool SoundEnabled => _settings.SoundEnabled;
    public bool MusicEnabled => _settings.MusicEnabled;
    public ELanguageCode LanguageCode => (ELanguageCode)_settings.LanguageCode;
    public int Hints => _inventory.HintsCount;
    public int NextLevel => PlayerPrefs.GetInt(NextLevelId, LastUnlockedLevel);
    public event Action<GameDataDynamic[]> OnLevelsChanged;
    public event Action<SettingsData> OnSettingsChanged;
    public event Action<InventoryData> OnInventoryChanged;
    public event Action<string> OnAccountChanged;
    public event Action<Exception> OnSynchronizationFailed;

    public GameDataDynamic GetLevelData(int levelId)
    {
        return Levels.FirstOrDefault(m => m.Id == levelId);
    }

    public void SetNextGameId(int gameId)
    {
        PlayerPrefs.SetInt(NextLevelId, gameId);
    }

    /// <summary>
    /// Startup: restore a cached identity or a previously chosen local guest.
    /// A fresh installation stays unselected (NeedsWelcome = true) until a
    /// button explicitly calls PlayAsGuestAsync or LogInAsync.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _operationGate.WaitAsync();
        _changingAccount = true;
        try
        {
            HookSignInOnce();
            if (_signIn.HasPendingLocalAccountChange)
                _signIn.ApplyPendingLocalChange();

            Exception authError = null;
            try { await _signIn.InitializeAsync(); }
            catch (Exception error) { authError = error; }

            if (authError == null && !string.IsNullOrEmpty(_signIn.PlayerId))
            {
                SelectIdentity(_signIn.PlayerId, _signIn.IsGuest);
            }
            else if (PlayerPrefs.HasKey(LastAccountKey))
            {
                string lastId = PlayerPrefs.GetString(LastAccountKey, string.Empty);
                if (authError != null && _signIn.IsAuthorized &&
                    !string.Equals(_signIn.PlayerId, lastId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Authenticated a different player before account restoration finished.",
                        authError);

                // Local saves can still be loaded if UGS is unavailable,
                // or when a previous offline guest has no UGS session yet.
                if (authError != null)
                    Debug.LogWarning("UGS unavailable, loading local account: " + authError.Message);
                RestoreLastLocalIdentity();
            }
            else
            {
                // Crucial: do NOT create a guest before the welcome choice.
                _local.SelectAccount(null);
                _levels = Array.Empty<GameDataDynamic>();
                _settings = new SettingsData();
                _inventory = new InventoryData();
                _ready = false;
            }

            _startupInitialized = true;
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        if (IsCloudAvailable) RequestSync();
    }

    /// <summary>
    /// Welcome: Play as Guest. Creates a UGS anonymous account when available,
    /// but never blocks offline play if Unity Services cannot connect.
    /// </summary>
    public async Task PlayAsGuestAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        _changingAccount = true;
        try
        {
            RequireStartup();
            if (_ready)
            {
                if (IsGuest) return;
                throw new InvalidOperationException("A linked account must log out first.");
            }

            try
            {
                await _signIn.InitializeAsync();
                if (_signIn.IsLinked)
                    throw new InvalidOperationException("A linked account is already active.");
                if (_signIn.IsGuest)
                    SelectIdentity(_signIn.PlayerId, nextIsGuest: true);
                else
                    await _signIn.StartNewGuestAsync();
            }
            catch (Exception error) when (!_signIn.IsAuthorized &&
                !_signIn.HasPendingLocalAccountChange)
            {
                Debug.LogWarning("Offline guest selected: " + error.Message);
                SelectIdentity(OfflineGuestId, nextIsGuest: true);
            }
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        if (IsCloudAvailable) RequestSync();
    }

    /// <summary>
    /// The ONE Log In method for Welcome and Settings. Guest progress stays
    /// attached to the same Player ID if linking succeeds. If the provider
    /// already belongs to another UGS account, SignInService invokes the
    /// confirmation callback BEFORE replacing the current guest identity.
    /// On confirmation the previous guest slot is discarded, never merged.
    /// The callback should open a popup and asynchronously return true/false.
    /// </summary>
    public async Task<LoginOutcome> LogInAsync(
        Func<Task<bool>> confirmReplaceGuestProgress = null,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        _changingAccount = true;
        LoginOutcome result;
        try
        {
            RequireStartup();
            if (IsLinked)
                throw new InvalidOperationException("Log out before logging into another account.");

            // On a device with no UGS session (Welcome or offline guest),
            // initialize first. For an offline guest, create a UGS guest
            // identity and migrate that local slot BEFORE linking Google/Apple.
            await _signIn.InitializeAsync();
            if (_ready && PlayerId == OfflineGuestId && !_signIn.IsGuest)
                await _signIn.StartNewGuestAsync();

            // A freshly created guest with no levels, preferences, or hints
            // does not need a destructive-change dialog. Meaningful guest
            // data always requires the caller-provided confirmation.
            Func<Task<bool>> confirmation = confirmReplaceGuestProgress;
            if (IsGuest && _levels.Length == 0 &&
                SettingsData.IsDefault(_settings) &&
                InventoryData.IsDefault(_inventory))
                confirmation = () => Task.FromResult(true);

            result = await _signIn.LogInAsync(confirmation, cancellationToken);
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }

        // Await the first sync so an existing account's cloud levels/settings/
        // inventory are loaded BEFORE the UI navigates to Home. Failure does
        // not erase the local save and SyncAsync reports it via its event.
        if (result == LoginOutcome.SignedIn ||
            result == LoginOutcome.GuestLinked ||
            result == LoginOutcome.SwitchedToExisting)
        {
            if (!await SyncAsync())
                MarkPending(PlayerId, true); // Retry on a later sync/launch.
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
            throw new InvalidOperationException("Log Out is only available for linked accounts.");

        // Best-effort flush, without blocking logout if cloud is unavailable.
        // Unsynced changes remain stored under the old linked Player ID.
        if (IsCloudAvailable && HasPendingUpload)
            await SyncAsync();

        await _operationGate.WaitAsync();
        _changingAccount = true;
        try
        {
            RequireSelected();
            if (!IsLinked)
                throw new InvalidOperationException("The selected account changed.");

            _signIn.SignOut(); // Clear UGS and Player Accounts tokens.
            try
            {
                await _signIn.StartNewGuestAsync();
            }
            catch (Exception error) when (!_signIn.HasPendingLocalAccountChange &&
                !_signIn.IsAuthorized)
            {
                // The linked session was already cleared successfully. Play
                // can continue offline; a real UGS guest is created later.
                Debug.LogWarning("New anonymous UGS account unavailable: " + error.Message);
                _local.Delete(OfflineGuestId); // Truly fresh guest, not a prior offline save.
                MarkPending(OfflineGuestId, false);
                SelectIdentity(OfflineGuestId, nextIsGuest: true);
            }
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        if (IsCloudAvailable) RequestSync();
    }

    public void RecordAttempt(int levelId, int steps = 0)
    {
        RequirePlayable();
        GameDataDynamic[] updated = ProgressRules.RecordAttempt(
            _levels, levelId, steps);
        PersistCurrent(updated, markPending: true);
        RequestSync();
    }

    public bool CanPlay(int levelId) =>
        IsReady && ProgressRules.CanPlay(_levels, levelId);

    public void SetSoundEnabled(bool enabled)
    {
        RequirePlayable();
        if (_settings.SoundEnabled == enabled) return;
        SettingsData updated = Settings;
        updated.SoundEnabled = enabled;
        updated.SoundChangedAtUtcTicks = SettingsData.NextTimestamp(
            updated.SoundChangedAtUtcTicks);
        PersistSettings(updated, markPending: true);
        RequestSync();
    }

    public void SetMusicEnabled(bool enabled)
    {
        RequirePlayable();
        if (_settings.MusicEnabled == enabled) return;
        SettingsData updated = Settings;
        updated.MusicEnabled = enabled;
        updated.MusicChangedAtUtcTicks = SettingsData.NextTimestamp(
            updated.MusicChangedAtUtcTicks);
        PersistSettings(updated, markPending: true);
        RequestSync();
    }

    public void SetLanguage(ELanguageCode language)
    {
        RequirePlayable();
        
        SettingsData updated = Settings;
        updated.LanguageCode = (int)language;
        updated.LanguageChangedAtUtcTicks = SettingsData.NextTimestamp(
            updated.LanguageChangedAtUtcTicks);
        PersistSettings(updated, markPending: true);
        RequestSync();
    }

    public void SetHints(int value)
    {
        RequirePlayable();
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (_inventory.HintsCount == value) return;
        InventoryData updated = Inventory;
        updated.HintsCount = value;
        updated.HintsChangedAtUtcTicks = InventoryData.NextTimestamp(
            updated.HintsChangedAtUtcTicks);
        PersistInventory(updated, markPending: true);
        RequestSync();
    }

    public void AddHints(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        RequirePlayable();
        SetHints(checked(_inventory.HintsCount + amount));
    }

    public bool TrySpendHints(int amount = 1)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        RequirePlayable();
        if (_inventory.HintsCount < amount) return false;
        SetHints(_inventory.HintsCount - amount);
        return true;
    }

    /// <summary>
    /// Link the current anonymous UGS identity. Same Player ID; local data retained.
    /// </summary>
    public async Task LinkGuestAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        _changingAccount = true;
        try
        {
            RequireSelected();
            if (!IsCloudIdentitySelected())
                throw new InvalidOperationException("Reconnect authentication first.");
            await _signIn.LinkGuestAsync(cancellationToken);
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        RequestSync();
    }

    /// <summary>
    /// Switch to EXISTING Unity Player Account, without transferring guest data.
    /// SignInService triggers a synchronous save-slot switch through its callback.
    /// </summary>
    public async Task<bool> SwitchAccountAsync(bool confirmedGuestLoss,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        _changingAccount = true;
        bool changed;
        try
        {
            RequireSelected();
            if (!IsCloudIdentitySelected())
                throw new InvalidOperationException("Reconnect authentication first.");
            changed = await _signIn.SwitchAccountAsync(
                confirmedGuestLoss, cancellationToken);
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        if (changed) RequestSync();
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
            throw new InvalidOperationException("Confirm guest progress loss first.");
        await _operationGate.WaitAsync();
        _changingAccount = true;
        try
        {
            RequireSelected();
            _signIn.SignOut(confirmedGuestLoss: true);
            try { await _signIn.StartNewGuestAsync(); }
            catch (Exception error) when (!_signIn.HasPendingLocalAccountChange &&
                !_signIn.IsAuthorized)
            {
                Debug.LogWarning("New guest is offline: " + error.Message);
                _local.Delete(OfflineGuestId);
                MarkPending(OfflineGuestId, false);
                SelectIdentity(OfflineGuestId, nextIsGuest: true);
            }
        }
        finally
        {
            _changingAccount = false;
            _operationGate.Release();
        }
        if (IsCloudAvailable) RequestSync();
    }

    /// <summary>
    /// Synchronize three independent records: levels, settings and inventory.
    /// Each record uses its own Cloud Save write lock. All three are verified
    /// before the account's pending-upload flag is cleared.
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        await _operationGate.WaitAsync();
        _syncing = true;
        try
        {
            if (!IsCloudAvailable) return false;
            string identity = PlayerId;

            for (int attempt = 0; attempt < MaxCloudAttempts; attempt++)
            {
                // 1. Level progress: keep the best valid sequential progress.
                LevelsCloudSnapshot levelCloud = await _cloud.DownloadAsync();
                RequireSameCloudIdentity(identity);
                GameDataDynamic[] mergedLevels = ProgressRules.Merge(_levels,
                    levelCloud.Levels);
                if (!ProgressRules.Same(_levels, mergedLevels))
                    PersistCurrent(mergedLevels, markPending: true);

                GameDataDynamic[] sendingLevels = ProgressRules.Clone(_levels);
                bool writeLevels = levelCloud.Exists
                    ? !ProgressRules.Same(levelCloud.Levels, sendingLevels)
                    : sendingLevels.Length != 0;
                if (writeLevels)
                {
                    try { await _cloud.UploadAsync(sendingLevels, levelCloud); }
                    catch (CloudSaveException error) when (
                        string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                    { continue; }
                    RequireSameCloudIdentity(identity);
                }

                // 2. Settings: merge sound, music and language independently.
                SettingsCloudSnapshot settingsCloud =
                    await _cloud.DownloadSettingsAsync();
                RequireSameCloudIdentity(identity);
                SettingsData mergedSettings = SettingsData.Merge(_settings, settingsCloud.Data);
                if (!SettingsData.Same(_settings, mergedSettings))
                    PersistSettings(mergedSettings, markPending: true);

                SettingsData sendingSettings = Settings;
                bool writeSettings = settingsCloud.Exists
                    ? !SettingsData.Same(settingsCloud.Data, sendingSettings)
                    : !SettingsData.IsDefault(sendingSettings);
                if (writeSettings)
                {
                    try { await _cloud.UploadSettingsAsync(sendingSettings, settingsCloud); }
                    catch (CloudSaveException error) when (
                        string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                    { continue; }
                    RequireSameCloudIdentity(identity);
                }

                // 3. Inventory: independent last-change-wins hint balance.
                InventoryCloudSnapshot inventoryCloud =
                    await _cloud.DownloadInventoryAsync();
                RequireSameCloudIdentity(identity);
                InventoryData mergedInventory = InventoryData.Merge(_inventory,
                    inventoryCloud.Data);
                if (!InventoryData.Same(_inventory, mergedInventory))
                    PersistInventory(mergedInventory, markPending: true);

                InventoryData sendingInventory = Inventory;
                bool writeInventory = inventoryCloud.Exists
                    ? !InventoryData.Same(inventoryCloud.Data, sendingInventory)
                    : !InventoryData.IsDefault(sendingInventory);
                if (writeInventory)
                {
                    try { await _cloud.UploadInventoryAsync(sendingInventory, inventoryCloud); }
                    catch (CloudSaveException error) when (
                        string.Equals(error.Reason.ToString(), "Conflict", StringComparison.Ordinal))
                    { continue; }
                    RequireSameCloudIdentity(identity);
                }

                // 4. Re-read and verify each key. This also detects non-atomic
                // first-time creates and local changes during network requests.
                LevelsCloudSnapshot checkedLevels = await _cloud.DownloadAsync();
                RequireSameCloudIdentity(identity);
                bool levelsMatch = checkedLevels.Exists
                    ? ProgressRules.Same(checkedLevels.Levels, sendingLevels)
                    : sendingLevels.Length == 0;
                if (!levelsMatch || !ProgressRules.Same(_levels, sendingLevels)) continue;

                SettingsCloudSnapshot checkedSettings =
                    await _cloud.DownloadSettingsAsync();
                RequireSameCloudIdentity(identity);
                bool settingsMatch = checkedSettings.Exists
                    ? SettingsData.Same(checkedSettings.Data, sendingSettings)
                    : SettingsData.IsDefault(sendingSettings) &&
                      SettingsData.IsDefault(checkedSettings.Data);
                if (!settingsMatch || !SettingsData.Same(_settings, sendingSettings)) continue;

                InventoryCloudSnapshot checkedInventory =
                    await _cloud.DownloadInventoryAsync();
                RequireSameCloudIdentity(identity);
                bool inventoryMatch = checkedInventory.Exists
                    ? InventoryData.Same(checkedInventory.Data, sendingInventory)
                    : InventoryData.IsDefault(sendingInventory) &&
                      InventoryData.IsDefault(checkedInventory.Data);
                if (!inventoryMatch || !InventoryData.Same(_inventory, sendingInventory))
                    continue;

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
            _syncing = false;
            _operationGate.Release();
        }
    }

    private void HookSignInOnce()
    {
        if (_hooked) return;
        if (_signIn.ApplyLocalAccountChange != null)
            throw new InvalidOperationException(
                "Another component already owns SignInService.ApplyLocalAccountChange. " +
                "Remove the old progress/authentication integration.");

        _signIn.ApplyLocalAccountChange = OnIdentityChanging;
        _signIn.GuestLinked += OnGuestLinked;
        _hooked = true;
    }

    private void OnGuestLinked(string playerId)
    {
        if (!string.Equals(PlayerId, playerId, StringComparison.Ordinal))
            throw new InvalidOperationException("Linked account does not match local slot.");
        SaveLastIdentity(playerId, wasGuest: false);
        // The UI changes from Log In to Log Out without another page load.
        OnAccountChanged?.Invoke(playerId);
    }

    private void OnIdentityChanging(PlayerIdentityChange change)
    {
        // This synchronous callback executes only during the serialized
        // AccountManager account-operation, before gameplay resumes.
        if (!_changingAccount)
            throw new InvalidOperationException(
                "Use AccountManager to switch accounts, not SignInService directly.");

        if (string.IsNullOrEmpty(change.CurrentPlayerId))
        {
            // Explicit sign-out: discard previous guest, keep linked saves.
            if (change.DiscardPreviousGuestProgress &&
                !string.IsNullOrEmpty(change.PreviousPlayerId))
                DeleteGuestSlot(change.PreviousPlayerId);
            _local.SelectAccount(null);
            _levels = Array.Empty<GameDataDynamic>();
            _settings = new SettingsData();
            _inventory = new InventoryData();
            _ready = false;
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
            throw new ArgumentException("Cannot select an empty Player ID.", nameof(nextId));

        string lastId = PlayerPrefs.GetString(LastAccountKey, string.Empty);
        bool lastWasGuest = PlayerPrefs.GetInt(LastWasGuestKey, 0) == 1;
        bool different = !string.Equals(lastId, nextId, StringComparison.Ordinal);

        // Offline-only first launch may contain attempts created before UGS
        // assigned a guest Player ID. Carry only that temporary guest forward.
        GameDataDynamic[] offlineToMigrate = null;
        SettingsData offlineSettingsToMigrate = null;
        InventoryData offlineInventoryToMigrate = null;
        if (different && lastId == OfflineGuestId && nextIsGuest &&
            (_local.HasSave(OfflineGuestId) ||
             _local.HasSettings(OfflineGuestId) ||
             _local.HasInventory(OfflineGuestId)))
        {
            _local.SelectAccount(OfflineGuestId);
            if (_local.HasSave(OfflineGuestId))
                offlineToMigrate = _local.Load();
            if (_local.HasSettings(OfflineGuestId))
                offlineSettingsToMigrate = _local.LoadSettings();
            if (_local.HasInventory(OfflineGuestId))
                offlineInventoryToMigrate = _local.LoadInventory();
        }

        _local.SelectAccount(nextId);
        GameDataDynamic[] destination = _local.Load();
        SettingsData settingsDestination = _local.LoadSettings();
        InventoryData inventoryDestination = _local.LoadInventory();
        if (offlineToMigrate != null)
        {
            destination = ProgressRules.Merge(destination, offlineToMigrate);
            _local.Save(destination);
            MarkPending(nextId, true);
        }
        if (offlineSettingsToMigrate != null)
        {
            settingsDestination = SettingsData.Merge(settingsDestination, offlineSettingsToMigrate);
            _local.SaveSettings(settingsDestination);
            MarkPending(nextId, true);
        }
        if (offlineInventoryToMigrate != null)
        {
            inventoryDestination = InventoryData.Merge(inventoryDestination, offlineInventoryToMigrate);
            _local.SaveInventory(inventoryDestination);
            MarkPending(nextId, true);
        }

        // Guest -> existing account: NEVER import guest progress into destination.
        // The same cleanup also covers an interrupted switch after relaunch.
        if (different && lastWasGuest && !string.IsNullOrEmpty(lastId))
            DeleteGuestSlot(lastId);

        _levels = destination;
        _settings = settingsDestination;
        _inventory = inventoryDestination;
        _ready = true;
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
        _local.SelectAccount(id);
        _levels = _local.Load();
        _settings = _local.LoadSettings();
        _inventory = _local.LoadInventory();
        _ready = true;
        SaveLastIdentity(id, isGuest);
        OnAccountChanged?.Invoke(id);
        OnLevelsChanged?.Invoke(Levels);
        OnSettingsChanged?.Invoke(Settings);
        OnInventoryChanged?.Invoke(Inventory);
    }

    private void PersistCurrent(GameDataDynamic[] levels, bool markPending)
    {
        _local.Save(levels); // persist first; never advertise a save that failed
        _levels = ProgressRules.Clone(levels);
        if (markPending) MarkPending(PlayerId, true);
        OnLevelsChanged?.Invoke(Levels);
    }

    private void PersistSettings(SettingsData data, bool markPending)
    {
        SettingsData clean = SettingsData.Normalize(data);
        _local.SaveSettings(clean);
        _settings = clean;
        if (markPending) MarkPending(PlayerId, true);
        OnSettingsChanged?.Invoke(Settings);
    }

    private void PersistInventory(InventoryData data, bool markPending)
    {
        InventoryData clean = InventoryData.Normalize(data);
        _local.SaveInventory(clean);
        _inventory = clean;
        if (markPending) MarkPending(PlayerId, true);
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
        _local.Delete(playerId);
        PlayerPrefs.DeleteKey(PendingKey(playerId));
        PlayerPrefs.Save();
    }

    private static void MarkPending(string id, bool pending)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (pending) PlayerPrefs.SetInt(PendingKey(id), 1);
        else PlayerPrefs.DeleteKey(PendingKey(id));
        PlayerPrefs.Save();
    }

    private static string PendingKey(string id) => PendingPrefix + id;

    private bool IsCloudIdentitySelected() =>
        _signIn.IsAuthorized && !_signIn.HasPendingLocalAccountChange &&
        string.Equals(_signIn.PlayerId, PlayerId, StringComparison.Ordinal);

    private void RequireSameCloudIdentity(string id)
    {
        if (!IsCloudIdentitySelected() ||
            !string.Equals(id, PlayerId, StringComparison.Ordinal))
            throw new InvalidOperationException("Authenticated account changed during sync.");
    }

    private void RequireStartup()
    {
        if (!_startupInitialized)
            throw new InvalidOperationException("Call InitializeAsync at startup first.");
    }

    private void RequireSelected()
    {
        if (!_ready || string.IsNullOrEmpty(PlayerId))
            throw new InvalidOperationException("Call InitializeAsync first.");
    }

    private void RequirePlayable()
    {
        RequireSelected();
        if (_changingAccount || _signIn.IsBusy || _signIn.HasPendingLocalAccountChange)
            throw new InvalidOperationException("An account change is in progress.");
    }

    private void RequestSync()
    {
        if (IsCloudAvailable && !_syncing)
            _ = SyncAsync(); // SyncAsync handles and reports service exceptions.
    }
}