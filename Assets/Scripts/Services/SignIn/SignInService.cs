using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Zillneuron.Utilities;

/// <summary>
/// Single entry point for authentication. Does NOT read/write game progress.
/// Consumer must react to IdentityChanged and switch/delete local save slots.
/// Call methods on Unity's main thread.
/// </summary>
public class SignInService : MonoSingleton<SignInService>
{
    private IGameAuthentication _authentication;
    private IPlayerAccountsLogin _playerAccounts;
    private bool _initialized;
    private int _busy;
    private PlayerIdentityChange _pendingLocalChange;

    protected override void OnInstanceCreated()
    {
        base.OnInstanceCreated();

        _authentication = new UgsAuthenticationGateway();
        _playerAccounts = new UnityPlayerAccountsGateway();
    }

    public SignInState State { get; private set; } = SignInState.NotInitialized;
    public bool IsBusy => Volatile.Read(ref _busy) != 0;
    public bool IsGuest => State == SignInState.Guest;
    public bool IsLinked => State == SignInState.Linked;
    public bool IsAuthorized => _initialized && _authentication.IsAuthorized;
    public string PlayerId => _initialized && _authentication.IsSignedIn
        ? _authentication.PlayerId : null;
    public bool HasPendingLocalAccountChange => _pendingLocalChange != null;

    /// <summary>
    /// REQUIRED before switching accounts, signing out, or starting a fresh guest.
    /// Synchronously select the destination player's LOCAL save slot. If requested,
    /// discard the previous guest's data. Do not merge progress across identities.
    /// Do not start cloud synchronization in this callback.
    /// </summary>
    public Action<PlayerIdentityChange> ApplyLocalAccountChange { get; set; }

    public event Action<SignInState> StateChanged;
    public event Action<string> GuestLinked;
    public event Action<PlayerIdentityChange> IdentityChanged;

    /// <summary>
    /// Initialize UGS and restore a cached player if one exists. On a completely
    /// new installation, remain SignedOut until the Welcome screen chooses either
    /// Log In or Play as Guest. This method NEVER creates a guest implicitly.
    /// </summary>
    public async Task InitializeAsync()
    {
        EnterOperation();
        try
        {
            if (_pendingLocalChange != null)
                throw new InvalidOperationException(
                    "Apply the pending local account change before reinitializing.");

            if (!_initialized)
            {
                await _authentication.InitializeAsync();
                _initialized = true;
            }

            if (!_authentication.IsAuthorized)
            {
                if (_authentication.IsSignedIn)
                    _authentication.SignOut(false); // Keep the existing cached session.

                if (_authentication.HasCachedSessionToken)
                    await _authentication.SignInCachedOrGuestAsync();
                else
                {
                    SetState(SignInState.SignedOut);
                    return;
                }
            }

            await _authentication.RefreshPlayerIdentityAsync();
            SetState(_authentication.HasUnityIdentity ? SignInState.Linked : SignInState.Guest);
        }
        finally { ExitOperation(); }
    }

    /// <summary>
    /// The one Log In entry point, used by Welcome and Settings.
    /// - No current UGS identity: create or sign into a Player Account.
    /// - Guest + unlinked Player Account: link to same Player ID, keeping progress.
    /// - Guest + already-linked Player Account: ask for confirmation; switch
    ///   to the existing account only if accepted.
    /// Linked players must Log Out before logging into a different identity.
    /// The optional confirmation callback is invoked AFTER browser login, only
    /// when an already-linked account is detected. It must display a real UI
    /// confirmation, not just return true automatically.
    /// </summary>
    public async Task<LoginOutcome> LogInAsync(
        Func<Task<bool>> confirmReplaceGuestProgress = null,
        CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            RequireReady();
            RequireLocalSaveHandler();
            if (IsLinked)
                throw new InvalidOperationException("Log out before signing in as another account.");
            if (IsGuest && !_authentication.IsAuthorized)
                throw new InvalidOperationException("Reconnect the guest session before linking it.");

            cancellationToken.ThrowIfCancellationRequested();
            string token = await _playerAccounts.GetAccessTokenAsync(cancellationToken);
            try
            {
                if (IsGuest)
                {
                    string guestId = PlayerId;
                    try
                    {
                        await _authentication.LinkToUnityAsync(token);
                    }
                    catch (AuthenticationException error) when (
                        error.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                    {
                        // No changes to the guest have been made at this point.
                        if (confirmReplaceGuestProgress == null)
                            return LoginOutcome.ConfirmationRequired;

                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await confirmReplaceGuestProgress())
                            return LoginOutcome.Cancelled;

                        cancellationToken.ThrowIfCancellationRequested();
                        bool switched = await ChangeUnityIdentityAsync(token,
                            createAccount: false);
                        return switched ? LoginOutcome.SwitchedToExisting : LoginOutcome.SameAccount;
                    }

                    if (!string.Equals(guestId, PlayerId, StringComparison.Ordinal))
                        throw new InvalidOperationException("Guest linking changed the UGS Player ID.");

                    // UgsAuthenticationGateway marks the Unity identity after a
                    // successful link. Do not make a second network request here:
                    // that could fail after the irreversible link succeeded.
                    SetState(SignInState.Linked);
                    GuestLinked?.Invoke(guestId);
                    return LoginOutcome.GuestLinked;
                }

                // Welcome screen without a guest: this allows first-time creation.
                bool changed = await ChangeUnityIdentityAsync(token, createAccount: true);
                return changed ? LoginOutcome.SignedIn : LoginOutcome.SameAccount;
            }
            finally
            {
                _playerAccounts.SignOut();
            }
        }
        finally { ExitOperation(); }
    }

    /// <summary>
    /// Change to an authenticated Unity identity using a token already selected
    /// in the browser. Tries to restore the previous cached session if the new
    /// login fails, and never changes the local save slot before UGS succeeds.
    /// </summary>
    private async Task<bool> ChangeUnityIdentityAsync(string token, bool createAccount)
    {
        string previousId = PlayerId;
        bool previousWasGuest = IsGuest;

        if (_authentication.IsSignedIn)
            _authentication.SignOut(false); // Preserve old UGS session for rollback.

        try
        {
            await _authentication.SignInUnityAsync(token, createAccount);
        }
        catch
        {
            if (!string.IsNullOrEmpty(previousId))
            {
                try
                {
                    if (_authentication.IsSignedIn)
                        _authentication.SignOut(false);
                    await _authentication.SignInCachedOrGuestAsync();
                    if (!string.Equals(previousId, PlayerId, StringComparison.Ordinal))
                    {
                        _authentication.SignOut(true);
                        throw new InvalidOperationException(
                            "Session recovery returned an unexpected Player ID.");
                    }
                    await _authentication.RefreshPlayerIdentityAsync();
                    SetState(_authentication.HasUnityIdentity
                        ? SignInState.Linked : SignInState.Guest);
                }
                catch
                {
                    if (_authentication.IsSignedIn) _authentication.SignOut(false);
                    SetState(SignInState.SignedOut);
                }
            }
            else
                SetState(SignInState.SignedOut);
            throw;
        }

        string currentId = PlayerId;
        // Unity sign-in succeeded; selecting the corresponding local slot
        // must not depend on a second PlayerInfo network request.
        SetState(SignInState.Linked);
        if (string.IsNullOrEmpty(currentId))
            throw new InvalidOperationException("Unity sign-in did not return a Player ID.");
        if (string.Equals(previousId, currentId, StringComparison.Ordinal))
            return false;

        _pendingLocalChange = new PlayerIdentityChange(previousId, currentId,
            discardPreviousGuestProgress: previousWasGuest && previousId != null,
            currentIsGuest: false);
        ApplyPendingLocalChangeInternal();
        return true;
    }

    /// <summary>
    /// Link this guest to a NEW Unity Player Account. The UGS Player ID must
    /// remain identical, so the guest's progress is preserved.
    /// Linking an account already owned by another player is an error.
    /// </summary>
    public async Task LinkGuestAsync(CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            RequireReady();
            if (!IsGuest)
                throw new InvalidOperationException("Only guest accounts can be linked.");
            if (!_authentication.IsAuthorized)
                throw new InvalidOperationException("The guest's UGS session is not authorized.");

            string originalId = PlayerId;
            string token = await _playerAccounts.GetAccessTokenAsync(cancellationToken);
            try
            {
                await _authentication.LinkToUnityAsync(token);
                if (!string.Equals(originalId, PlayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Linking unexpectedly changed the Player ID.");

                SetState(SignInState.Linked);
            }
            finally
            {
                // Browser access token is not needed after UGS linking.
                _playerAccounts.SignOut();
            }

            // The save system upgrades the guest save ownership, not its contents.
            GuestLinked?.Invoke(originalId);
        }
        finally { ExitOperation(); }
    }

    /// <summary>
    /// Switch to an EXISTING account only (never auto-create another UGS account).
    /// An unlinked guest requires a progress-loss confirmation before proceeding.
    /// Returns false if the selected account has the same UGS Player ID.
    /// </summary>
    public async Task<bool> SwitchAccountAsync(bool confirmedGuestLoss,
        CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            RequireReady();
            RequireLocalSaveHandler();
            if (IsGuest && !confirmedGuestLoss)
                throw new InvalidOperationException(
                    "Confirm guest progress loss before switching accounts.");

            string token = await _playerAccounts.GetAccessTokenAsync(cancellationToken);
            try
            {
                return await ChangeUnityIdentityAsync(token, createAccount: false);
            }
            finally { _playerAccounts.SignOut(); }
        }
        finally { ExitOperation(); }
    }

    /// <summary>Compatibility alias for existing UI scripts.</summary>
    public async Task SignInExistingAsync(bool confirmedGuestLoss,
        CancellationToken cancellationToken = default)
    {
        await SwitchAccountAsync(confirmedGuestLoss, cancellationToken);
    }

    /// <summary>
    /// Reapply the pending save-slot change if a local file operation failed
    /// AFTER authentication successfully switched accounts.
    /// Block gameplay/cloud sync while HasPendingLocalAccountChange is true.
    /// </summary>
    public void ApplyPendingLocalChange()
    {
        if (IsBusy)
            throw new InvalidOperationException("An authentication operation is in progress.");
        ApplyPendingLocalChangeInternal();
    }

    /// <summary>
    /// Forget the current UGS session. Also reset active local progress to a fresh
    /// guest slot through ApplyLocalAccountChange. Then call StartNewGuestAsync.
    /// </summary>
    public void SignOut(bool confirmedGuestLoss = false)
    {
        EnterOperation();
        try
        {
            // Signing out only clears local credentials. Allow it even if
            // online UGS initialization failed and AccountManager restored
            // a previously linked local save for offline gameplay.
            if (_pendingLocalChange != null)
                throw new InvalidOperationException(
                    "Apply the pending account change before signing out.");
            RequireLocalSaveHandler();
            if (IsGuest && !confirmedGuestLoss)
                throw new InvalidOperationException(
                    "Confirm guest progress loss before signing out.");

            string previousId = PlayerId;
            bool previousWasGuest = IsGuest;

            _authentication.SignOut(true);
            _playerAccounts.SignOut();
            SetState(SignInState.SignedOut);

            _pendingLocalChange = new PlayerIdentityChange(
                previousId, null, previousWasGuest, currentIsGuest: true);
            ApplyPendingLocalChangeInternal();
        }
        finally { ExitOperation(); }
    }

    /// <summary>Start a new UGS guest after SignOut().</summary>
    public async Task StartNewGuestAsync()
    {
        EnterOperation();
        try
        {
            RequireReady();
            RequireLocalSaveHandler();
            if (_authentication.IsSignedIn)
                throw new InvalidOperationException("Sign out before creating a new guest.");

            // Creating a fresh guest must never revive an old linked session.
            // A cached token means the caller forgot to clear credentials.
            if (_authentication.HasCachedSessionToken)
                throw new InvalidOperationException(
                    "A cached session exists. Clear credentials before starting a fresh guest.");

            await _authentication.SignInCachedOrGuestAsync();
            if (_authentication.HasUnityIdentity)
                throw new InvalidOperationException(
                    "Expected a new guest but a linked account was restored.");

            SetState(SignInState.Guest);
            _pendingLocalChange = new PlayerIdentityChange(
                null, PlayerId, discardPreviousGuestProgress: false,
                currentIsGuest: true);
            ApplyPendingLocalChangeInternal();
        }
        finally { ExitOperation(); }
    }

    private void ApplyPendingLocalChangeInternal()
    {
        var change = _pendingLocalChange;
        if (change == null) return;

        RequireLocalSaveHandler();
        ApplyLocalAccountChange(change); // On failure, preserve pending transition.
        _pendingLocalChange = null;
        IdentityChanged?.Invoke(change);
    }

    private void RequireReady()
    {
        if (!_initialized)
            throw new InvalidOperationException("Call InitializeAsync first.");
        if (_pendingLocalChange != null)
            throw new InvalidOperationException(
                "Call ApplyPendingLocalChange before another account operation.");
    }

    private void RequireLocalSaveHandler()
    {
        if (ApplyLocalAccountChange == null)
            throw new InvalidOperationException(
                "Set ApplyLocalAccountChange before changing account identity.");
    }

    private void SetState(SignInState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(next);
    }

    private void EnterOperation()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("An authentication operation is already running.");
    }

    private void ExitOperation() => Volatile.Write(ref _busy, 0);
}
