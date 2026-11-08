using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Attach to a Canvas GameObject. Connect Button.onClick entries in the Inspector.
    /// All identity and save changes go through AccountManager; never call SignInService here.
    /// Requires the existing AccountManager v3.1 and TextMeshPro.
    /// </summary>
    public sealed class AccountUIController : MonoBehaviour
    {
        [Header("Account Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button linkButton;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button syncButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text accountStatusText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Confirmation Dialog (required for login/switch/logout)")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;

        private enum Operation
        {
            None,
            Initialize,
            LinkGuest,
            ExistingAccount,
            Logout,
            Sync
        }

        private AccountService _account = AccountService.Instance;
        private Operation _pendingConfirmation = Operation.None;
        private bool _working;
        private string _feedback = string.Empty;

        private void OnEnable()
        {
            _account.OnAccountChanged += OnAccountChanged;
            _account.OnLevelsChanged += OnLevelsChanged;
            _account.OnSynchronizationFailed += OnSyncFailed;
            RefreshUI();
        }

        private void OnDisable()
        {
            _account.OnAccountChanged -= OnAccountChanged;
            _account.OnLevelsChanged -= OnLevelsChanged;
            _account.OnSynchronizationFailed -= OnSyncFailed;
        }

        private void Start()
        {
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);

            // If your game bootstrap already initialized AccountManager, don't repeat it.
            if (!_account.IsReady)
                _ = ExecuteAsync(Operation.Initialize);
            else
                RefreshUI();
        }

        // Connect the following public methods to Button -> On Click() in the Inspector.

        public void OnLoginClicked()
        {
            if (!CanDoAccountAction() || !_account.IsGuest) return;
            AskConfirmation(Operation.ExistingAccount,
                "Sign in to an existing account? Your current guest progress " +
                "will be deleted after a successful account switch.");
        }

        public void OnLinkClicked()
        {
            if (!CanDoAccountAction() || !_account.IsGuest) return;
            _ = ExecuteAsync(Operation.LinkGuest);
        }

        public void OnSwitchClicked()
        {
            if (!CanDoAccountAction() || !_account.IsLinked) return;
            AskConfirmation(Operation.ExistingAccount,
                "Switch to another account? The current linked account's " +
                "save will remain stored separately.");
        }

        public void OnLogoutClicked()
        {
            if (!CanDoAccountAction()) return;
            string message = _account.IsGuest
                ? "Start a new guest account? Your current guest progress will be lost."
                : "Log out and start a new guest account? You can sign back in " +
                  "to recover the linked account's progress.";
            AskConfirmation(Operation.Logout, message);
        }

        public void OnSyncClicked()
        {
            if (!CanDoAccountAction() || _account.IsSyncing) return;
            _ = ExecuteAsync(Operation.Sync);
        }

        public void OnConfirmClicked()
        {
            if (_pendingConfirmation == Operation.None || _working) return;
            Operation operation = _pendingConfirmation;
            CloseConfirmation();
            _ = ExecuteAsync(operation);
        }

        public void OnCancelClicked() => CloseConfirmation();

        // Optional: connect a Retry Connection button to this method.
        public void OnRetryConnectionClicked()
        {
            if (_working || _pendingConfirmation != Operation.None) return;
            _ = ExecuteAsync(Operation.Initialize);
        }

        private bool CanDoAccountAction() =>
            !_working && _pendingConfirmation == Operation.None &&
            _account.IsCloudAvailable;

        private void AskConfirmation(Operation operation, string message)
        {
            // Never bypass required destructive-action confirmation.
            if (confirmationPanel == null || confirmationText == null)
            {
                _feedback = "Assign a confirmation panel and text in the Inspector.";
                Debug.LogError(_feedback);
                RefreshUI();
                return;
            }

            _pendingConfirmation = operation;
            confirmationText.text = message;
            confirmationPanel.SetActive(true);
            RefreshUI();
        }

        private void CloseConfirmation()
        {
            _pendingConfirmation = Operation.None;
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            RefreshUI();
        }

        private async Task ExecuteAsync(Operation operation)
        {
            if (_working) return;
            _working = true;
            _feedback = "Working...";
            RefreshUI();

            try
            {
                switch (operation)
                {
                    case Operation.Initialize:
                        await _account.InitializeAsync();
                        _feedback = _account.IsCloudAvailable
                            ? "Account ready." : "Offline: local save is available.";
                        break;

                    case Operation.LinkGuest:
                        await _account.LinkGuestAsync();
                        _feedback = "Guest progress linked successfully.";
                        break;

                    case Operation.ExistingAccount:
                        // 'true' is permitted only after the confirmation above.
                        bool changed = await _account.SwitchAccountAsync(
                            confirmedGuestLoss: true);
                        _feedback = changed
                            ? "Account changed. Its progress is now selected."
                            : "That account was already active.";
                        break;

                    case Operation.Logout:
                        await _account.SignOutAndStartGuestAsync(
                            confirmedGuestLoss: true);
                        _feedback = "Signed out. New guest account created.";
                        break;

                    case Operation.Sync:
                        bool synced = await _account.SyncAsync();
                        _feedback = synced
                            ? "Progress synchronized." 
                            : "Could not synchronize. Local progress is retained.";
                        break;
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                _feedback = "Account operation failed: " + error.Message;
            }
            finally
            {
                _working = false;
                RefreshUI();
            }
        }

        private void OnAccountChanged(string playerId) => RefreshUI();
        private void OnLevelsChanged(GameDataDynamic[] levels) => RefreshUI();

        private void OnSyncFailed(Exception error)
        {
            _feedback = "Cloud sync failed; local progress is safe.";
            RefreshUI();
        }

        private void RefreshUI()
        {
            bool ready = _account.IsReady;
            bool online = _account.IsCloudAvailable;
            bool guest = _account.IsGuest;
            bool linked = _account.IsLinked;
            bool enabled = !_working && _pendingConfirmation == Operation.None && online;

            if (loginButton != null)
            {
                loginButton.gameObject.SetActive(!linked);
                loginButton.interactable = enabled && guest;
            }
            if (linkButton != null)
            {
                linkButton.gameObject.SetActive(!linked);
                linkButton.interactable = enabled && guest;
            }
            if (switchButton != null)
            {
                switchButton.gameObject.SetActive(linked);
                switchButton.interactable = enabled && linked;
            }
            if (logoutButton != null)
                logoutButton.interactable = enabled;
            if (syncButton != null)
                syncButton.interactable = enabled && !_account.IsSyncing;

            if (accountStatusText != null)
            {
                string status = !ready ? "Initializing" :
                    !online ? "Offline (local save)" :
                    guest ? "Guest" : "Linked account";

                string id = ready ? _account.PlayerId : "—";
                accountStatusText.text =
                    $"Status: {status}\nPlayer ID: {id}\n" +
                    $"Unlocked level: {_account.LastUnlockedLevel}";
            }

            if (feedbackText != null)
                feedbackText.text = _feedback;
        }
    }
}
