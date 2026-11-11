using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Zillneuron.Utilities;
using UnityEngine.Scripting.APIUpdating;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Manages Unity authentication and delegates local save changes to ApplyLocalAccountChange.
    /// Call methods on Unity's main thread.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class SignInService : MonoSingleton<SignInService>
    {
        private IGameAuthentication authentication;
        private IPlayerAccountsLogin playerAccounts;
        private bool initialized;
        private int busy;
        private PlayerIdentityChange pendingLocalChange;

        protected override void OnInstanceCreated()
        {
            base.OnInstanceCreated();

            authentication = new UgsAuthenticationGateway();
            playerAccounts = new UnityPlayerAccountsGateway();
        }

        public SignInState State
        {
            get;
            private set;
        } = SignInState.NotInitialized;
        
        public bool IsBusy
        {
            get
            {
                return Volatile.Read(ref busy) != 0;
            }
        }

        public bool IsGuest
        {
            get
            {
                return State == SignInState.Guest;
            }
        }

        public bool IsLinked
        {
            get
            {
                return State == SignInState.Linked;
            }
        }

        public bool IsAuthorized
        {
            get
            {
                return initialized && authentication.IsAuthorized;
            }
        }

        public string PlayerId
        {
            get
            {
                return initialized && authentication.IsSignedIn ? authentication.PlayerId : null;
            }
        }

        public bool HasPendingLocalAccountChange
        {
            get
            {
                return pendingLocalChange != null;
            }
        }

        /// <summary>
        /// REQUIRED before switching accounts, signing out, or starting a fresh guest.
        /// Synchronously select the destination player's LOCAL save slot. If requested,
        /// discard the previous guest's data. Do not merge progress across identities.
        /// Do not start cloud synchronization in this callback.
        /// </summary>
        public Action<PlayerIdentityChange> ApplyLocalAccountChange
        {
            get;
            set;
        }

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
                if (pendingLocalChange != null)
                {
                    throw new InvalidOperationException("Apply the pending local account change before reinitializing.");
                }

                if (!initialized)
                {
                    await authentication.InitializeAsync();
                    initialized = true;
                }

                if (!authentication.IsAuthorized)
                {
                    if (authentication.IsSignedIn)
                    {
                        authentication.SignOut(false); // Keep the existing cached session.
                    }

                    if (authentication.HasCachedSessionToken)
                    {
                        await authentication.SignInCachedOrGuestAsync();
                    }
                    else
                    {
                        SetState(SignInState.SignedOut);
                        return;
                    }
                }

                await authentication.RefreshPlayerIdentityAsync();
                SetState(authentication.HasUnityIdentity ? SignInState.Linked : SignInState.Guest);
            }
            finally
            {
                ExitOperation();
            }
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
        public async Task<LoginOutcome> LogInAsync(Func<Task<bool>> confirmReplaceGuestProgress = null, CancellationToken cancellationToken = default)
        {
            EnterOperation();

            try
            {
                RequireReady();
                RequireLocalSaveHandler();

                if (IsLinked)
                {
                    throw new InvalidOperationException("Log out before signing in as another account.");
                }

                if (IsGuest && !authentication.IsAuthorized)
                {
                    throw new InvalidOperationException("Reconnect the guest session before linking it.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                string token = await playerAccounts.GetAccessTokenAsync(cancellationToken);
                try
                {
                    if (IsGuest)
                    {
                        string guestId = PlayerId;
                        try
                        {
                            await authentication.LinkToUnityAsync(token);
                        }
                        catch (AuthenticationException error) when (error.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                        {
                            // No changes to the guest have been made at this point.
                            if (confirmReplaceGuestProgress == null)
                            {
                                return LoginOutcome.ConfirmationRequired;
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                            if (!await confirmReplaceGuestProgress())
                            {
                                return LoginOutcome.Cancelled;
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                            bool switched = await ChangeUnityIdentityAsync(token, createAccount: false);
                            return switched ? LoginOutcome.SwitchedToExisting : LoginOutcome.SameAccount;
                        }

                        if (!string.Equals(guestId, PlayerId, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException("Guest linking changed the UGS Player ID.");
                        }

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
                    playerAccounts.SignOut();
                }
            }
            finally
            {
                ExitOperation();
            }
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

            if (authentication.IsSignedIn)
            {
                authentication.SignOut(false); // Preserve old UGS session for rollback.
            }

            try
            {
                await authentication.SignInUnityAsync(token, createAccount);
            }
            catch
            {
                if (!string.IsNullOrEmpty(previousId))
                {
                    try
                    {
                        if (authentication.IsSignedIn)
                        {
                            authentication.SignOut(false);
                        }

                        await authentication.SignInCachedOrGuestAsync();
                        if (!string.Equals(previousId, PlayerId, StringComparison.Ordinal))
                        {
                            authentication.SignOut(true);
                            throw new InvalidOperationException("Session recovery returned an unexpected Player ID.");
                        }
                        await authentication.RefreshPlayerIdentityAsync();
                        SetState(authentication.HasUnityIdentity ? SignInState.Linked : SignInState.Guest);
                    }
                    catch
                    {
                        if (authentication.IsSignedIn)
                        {
                            authentication.SignOut(false);
                        }

                        SetState(SignInState.SignedOut);
                    }
                }
                else
                {
                    SetState(SignInState.SignedOut);
                }

                throw;
            }

            string currentId = PlayerId;
            // Unity sign-in succeeded; selecting the corresponding local slot
            // must not depend on a second PlayerInfo network request.
            SetState(SignInState.Linked);
            if (string.IsNullOrEmpty(currentId))
            {
                throw new InvalidOperationException("Unity sign-in did not return a Player ID.");
            }

            if (string.Equals(previousId, currentId, StringComparison.Ordinal))
            {
                return false;
            }

            pendingLocalChange = new PlayerIdentityChange(previousId, currentId, discardPreviousGuestProgress: previousWasGuest && previousId != null, currentIsGuest: false);
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
                {
                    throw new InvalidOperationException("Only guest accounts can be linked.");
                }

                if (!authentication.IsAuthorized)
                {
                    throw new InvalidOperationException("The guest's UGS session is not authorized.");
                }

                string originalId = PlayerId;
                string token = await playerAccounts.GetAccessTokenAsync(cancellationToken);
                try
                {
                    await authentication.LinkToUnityAsync(token);
                    if (!string.Equals(originalId, PlayerId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Linking unexpectedly changed the Player ID.");
                    }

                    SetState(SignInState.Linked);
                }
                finally
                {
                    // Browser access token is not needed after UGS linking.
                    playerAccounts.SignOut();
                }

                // The save system upgrades the guest save ownership, not its contents.
                GuestLinked?.Invoke(originalId);
            }
            finally
            {
                ExitOperation();
            }
        }


        /// <summary>
        /// Switch to an EXISTING account only (never auto-create another UGS account).
        /// An unlinked guest requires a progress-loss confirmation before proceeding.
        /// Returns false if the selected account has the same UGS Player ID.
        /// </summary>
        public async Task<bool> SwitchAccountAsync(bool confirmedGuestLoss, CancellationToken cancellationToken = default)
        {
            EnterOperation();
            try
            {
                RequireReady();
                RequireLocalSaveHandler();
                if (IsGuest && !confirmedGuestLoss)
                {
                    throw new InvalidOperationException("Confirm guest progress loss before switching accounts.");
                }

                string token = await playerAccounts.GetAccessTokenAsync(cancellationToken);
                try
                {
                    return await ChangeUnityIdentityAsync(token, createAccount: false);
                }
                finally
                {
                    playerAccounts.SignOut();
                }
            }
            finally
            {
                ExitOperation();
            }
        }

        //Unused by the current project.
        ///// <summary>Compatibility alias for existing UI scripts.</summary>
        //public async Task SignInExistingAsync(bool confirmedGuestLoss, CancellationToken cancellationToken = default)
        //{
        //    await SwitchAccountAsync(confirmedGuestLoss, cancellationToken);
        //}

        /// <summary>
        /// Reapply the pending save-slot change if a local file operation failed
        /// AFTER authentication successfully switched accounts.
        /// Block gameplay/cloud sync while HasPendingLocalAccountChange is true.
        /// </summary>
        public void ApplyPendingLocalChange()
        {
            if (IsBusy)
            {
                throw new InvalidOperationException("An authentication operation is in progress.");
            }

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
                // online UGS initialization failed and AccountService restored
                // a previously linked local save for offline gameplay.
                if (pendingLocalChange != null)
                {
                    throw new InvalidOperationException("Apply the pending account change before signing out.");
                }

                RequireLocalSaveHandler();

                if (IsGuest && !confirmedGuestLoss)
                {
                    throw new InvalidOperationException("Confirm guest progress loss before signing out.");
                }

                string previousId = PlayerId;
                bool previousWasGuest = IsGuest;

                authentication.SignOut(true);
                playerAccounts.SignOut();

                SetState(SignInState.SignedOut);

                pendingLocalChange = new PlayerIdentityChange(previousId, null, previousWasGuest, currentIsGuest: true);

                ApplyPendingLocalChangeInternal();
            }
            finally
            {
                ExitOperation();
            }
        }

        /// <summary>Start a new UGS guest after SignOut().</summary>
        public async Task StartNewGuestAsync()
        {
            EnterOperation();

            try
            {
                RequireReady();
                RequireLocalSaveHandler();

                if (authentication.IsSignedIn)
                {
                    throw new InvalidOperationException("Sign out before creating a new guest.");
                }

                // Creating a fresh guest must never revive an old linked session.
                // A cached token means the caller forgot to clear credentials.
                if (authentication.HasCachedSessionToken)
                {
                    throw new InvalidOperationException("A cached session exists. Clear credentials before starting a fresh guest.");
                }

                await authentication.SignInCachedOrGuestAsync();

                if (authentication.HasUnityIdentity)
                {
                    throw new InvalidOperationException("Expected a new guest but a linked account was restored.");
                }

                SetState(SignInState.Guest);

                pendingLocalChange = new PlayerIdentityChange(null, PlayerId, discardPreviousGuestProgress: false,currentIsGuest: true);

                ApplyPendingLocalChangeInternal();
            }
            finally
            {
                ExitOperation();
            }
        }

        private void ApplyPendingLocalChangeInternal()
        {
            var change = pendingLocalChange;
            if (change == null)
            {
                return;
            }

            RequireLocalSaveHandler();

            ApplyLocalAccountChange(change); // On failure, preserve pending transition.
            pendingLocalChange = null;
            IdentityChanged?.Invoke(change);
        }

        private void RequireReady()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Call InitializeAsync first.");
            }

            if (pendingLocalChange != null)
            {
                throw new InvalidOperationException("Call ApplyPendingLocalChange before another account operation.");
            }
        }

        private void RequireLocalSaveHandler()
        {
            if (ApplyLocalAccountChange == null)
            {
                throw new InvalidOperationException("Set ApplyLocalAccountChange before changing account identity.");
            }
        }

        private void SetState(SignInState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }

        private void EnterOperation()
        {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            {
                throw new InvalidOperationException("An authentication operation is already running.");
            }
        }

        private void ExitOperation()
        {
            Volatile.Write(ref busy, 0);
        }
    }
}
