// using System;
// using System.Threading.Tasks;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;

namespace Zillneuron.Blockmates.Services.SignIn
{
    // Unused component. No scene or prefab references it.
    // /// <summary>
    // /// Attach to a Canvas GameObject. Connect Button.onClick entries in the Inspector.
    // /// All identity and save changes go through AccountService; never call SignInService here.
    // /// Uses TextMeshPro labels for account status and feedback.
    // /// </summary>
    // public sealed class AccountUIController : MonoBehaviour
    // {
    //     [Header("Account Buttons")]
    //     [SerializeField] private Button loginButton;
    //     [SerializeField] private Button linkButton;
    //     [SerializeField] private Button switchButton;
    //     [SerializeField] private Button logoutButton;
    //     [SerializeField] private Button syncButton;
    //
    //     [Header("Labels")]
    //     [SerializeField] private TMP_Text accountStatusText;
    //     [SerializeField] private TMP_Text feedbackText;
    //
    //     [Header("Confirmation Dialog (required for login/switch/logout)")]
    //     [SerializeField] private GameObject confirmationPanel;
    //     [SerializeField] private TMP_Text confirmationText;
    //
    //     private enum Operation
    //     {
    //         None,
    //         Initialize,
    //         LinkGuest,
    //         ExistingAccount,
    //         Logout,
    //         Sync
    //     }
    //
    //     private AccountService account = AccountService.Instance;
    //     private Operation pendingConfirmation = Operation.None;
    //     private bool working;
    //     private string feedback = string.Empty;
    //
    //     private void OnEnable()
    //     {
    //         account.OnAccountChanged += OnAccountChanged;
    //         account.OnLevelsChanged += OnLevelsChanged;
    //         account.OnSynchronizationFailed += OnSyncFailed;
    //         RefreshUI();
    //     }
    //
    //     private void OnDisable()
    //     {
    //         account.OnAccountChanged -= OnAccountChanged;
    //         account.OnLevelsChanged -= OnLevelsChanged;
    //         account.OnSynchronizationFailed -= OnSyncFailed;
    //     }
    //
    //     private void Start()
    //     {
    //         if (confirmationPanel != null)
    //         {
    //             confirmationPanel.SetActive(false);
    //         }
    //
    //         // If your game bootstrap already initialized AccountService, don't repeat it.
    //         if (!account.IsReady)
    //         {
    //             _ = ExecuteAsync(Operation.Initialize);
    //         }
    //         else
    //         {
    //             RefreshUI();
    //         }
    //     }
    //
    //     // Connect the following public methods to Button -> On Click() in the Inspector.
    //
    //     public void OnLoginClicked()
    //     {
    //         if (!CanDoAccountAction() || !account.IsGuest)
    //         {
    //             return;
    //         }
    //
    //         AskConfirmation(Operation.ExistingAccount,
    //             "Sign in to an existing account? Your current guest progress " +
    //             "will be deleted after a successful account switch.");
    //     }
    //
    //     public void OnLinkClicked()
    //     {
    //         if (!CanDoAccountAction() || !account.IsGuest)
    //         {
    //             return;
    //         }
    //
    //         _ = ExecuteAsync(Operation.LinkGuest);
    //     }
    //
    //     public void OnSwitchClicked()
    //     {
    //         if (!CanDoAccountAction() || !account.IsLinked)
    //         {
    //             return;
    //         }
    //
    //         AskConfirmation(Operation.ExistingAccount,
    //             "Switch to another account? The current linked account's " +
    //             "save will remain stored separately.");
    //     }
    //
    //     public void OnLogoutClicked()
    //     {
    //         if (!CanDoAccountAction())
    //         {
    //             return;
    //         }
    //
    //         string message = account.IsGuest
    //             ? "Start a new guest account? Your current guest progress will be lost."
    //             : "Log out and start a new guest account? You can sign back in " +
    //               "to recover the linked account's progress.";
    //         AskConfirmation(Operation.Logout, message);
    //     }
    //
    //     public void OnSyncClicked()
    //     {
    //         if (!CanDoAccountAction() || account.IsSyncing)
    //         {
    //             return;
    //         }
    //
    //         _ = ExecuteAsync(Operation.Sync);
    //     }
    //
    //     public void OnConfirmClicked()
    //     {
    //         if (pendingConfirmation == Operation.None || working)
    //         {
    //             return;
    //         }
    //
    //         Operation operation = pendingConfirmation;
    //         CloseConfirmation();
    //         _ = ExecuteAsync(operation);
    //     }
    //
    //     public void OnCancelClicked()
    //     {
    //         CloseConfirmation();
    //     }
    //
    //     // Optional: connect a Retry Connection button to this method.
    //     public void OnRetryConnectionClicked()
    //     {
    //         if (working || pendingConfirmation != Operation.None)
    //         {
    //             return;
    //         }
    //
    //         _ = ExecuteAsync(Operation.Initialize);
    //     }
    //
    //     private bool CanDoAccountAction()
    //     {
    //         return !working && pendingConfirmation == Operation.None &&
    //             account.IsCloudAvailable;
    //     }
    //
    //     private void AskConfirmation(Operation operation, string message)
    //     {
    //         // Never bypass required destructive-action confirmation.
    //         if (confirmationPanel == null || confirmationText == null)
    //         {
    //             feedback = "Assign a confirmation panel and text in the Inspector.";
    //             Debug.LogError(feedback);
    //             RefreshUI();
    //             return;
    //         }
    //
    //         pendingConfirmation = operation;
    //         confirmationText.text = message;
    //         confirmationPanel.SetActive(true);
    //         RefreshUI();
    //     }
    //
    //     private void CloseConfirmation()
    //     {
    //         pendingConfirmation = Operation.None;
    //         if (confirmationPanel != null)
    //         {
    //             confirmationPanel.SetActive(false);
    //         }
    //
    //         RefreshUI();
    //     }
    //
    //     private async Task ExecuteAsync(Operation operation)
    //     {
    //         if (working)
    //         {
    //             return;
    //         }
    //
    //         working = true;
    //         feedback = "Working...";
    //         RefreshUI();
    //
    //         try
    //         {
    //             switch (operation)
    //             {
    //                 case Operation.Initialize:
    //                     await account.InitializeAsync();
    //                     feedback = account.IsCloudAvailable
    //                         ? "Account ready." : "Offline: local save is available.";
    //                     break;
    //
    //                 case Operation.LinkGuest:
    //                     await account.LinkGuestAsync();
    //                     feedback = "Guest progress linked successfully.";
    //                     break;
    //
    //                 case Operation.ExistingAccount:
    //                     // 'true' is permitted only after the confirmation above.
    //                     bool changed = await account.SwitchAccountAsync(
    //                         confirmedGuestLoss: true);
    //                     feedback = changed
    //                         ? "Account changed. Its progress is now selected."
    //                         : "That account was already active.";
    //                     break;
    //
    //                 case Operation.Logout:
    //                     await account.SignOutAndStartGuestAsync(
    //                         confirmedGuestLoss: true);
    //                     feedback = "Signed out. New guest account created.";
    //                     break;
    //
    //                 case Operation.Sync:
    //                     bool synced = await account.SyncAsync();
    //                     feedback = synced
    //                         ? "Progress synchronized."
    //                         : "Could not synchronize. Local progress is retained.";
    //                     break;
    //             }
    //         }
    //         catch (Exception error)
    //         {
    //             Debug.LogException(error);
    //             feedback = "Account operation failed: " + error.Message;
    //         }
    //         finally
    //         {
    //             working = false;
    //             RefreshUI();
    //         }
    //     }
    //
    //     private void OnAccountChanged(string playerId)
    //     {
    //         RefreshUI();
    //     }
    //     private void OnLevelsChanged(GameDataDynamic[] levels)
    //     {
    //         RefreshUI();
    //     }
    //
    //     private void OnSyncFailed(Exception error)
    //     {
    //         feedback = "Cloud sync failed; local progress is safe.";
    //         RefreshUI();
    //     }
    //
    //     private void RefreshUI()
    //     {
    //         bool ready = account.IsReady;
    //         bool online = account.IsCloudAvailable;
    //         bool guest = account.IsGuest;
    //         bool linked = account.IsLinked;
    //         bool enabled = !working && pendingConfirmation == Operation.None && online;
    //
    //         if (loginButton != null)
    //         {
    //             loginButton.gameObject.SetActive(!linked);
    //             loginButton.interactable = enabled && guest;
    //         }
    //         if (linkButton != null)
    //         {
    //             linkButton.gameObject.SetActive(!linked);
    //             linkButton.interactable = enabled && guest;
    //         }
    //         if (switchButton != null)
    //         {
    //             switchButton.gameObject.SetActive(linked);
    //             switchButton.interactable = enabled && linked;
    //         }
    //         if (logoutButton != null)
    //         {
    //             logoutButton.interactable = enabled;
    //         }
    //
    //         if (syncButton != null)
    //         {
    //             syncButton.interactable = enabled && !account.IsSyncing;
    //         }
    //
    //         if (accountStatusText != null)
    //         {
    //             string status = !ready ? "Initializing" :
    //                 !online ? "Offline (local save)" :
    //                 guest ? "Guest" : "Linked account";
    //
    //             string id = ready ? account.PlayerId : "—";
    //             accountStatusText.text =
    //                 $"Status: {status}\nPlayer ID: {id}\n" +
    //                 $"Unlocked level: {account.LastUnlockedLevel}";
    //         }
    //
    //         if (feedbackText != null)
    //         {
    //             feedbackText.text = feedback;
    //         }
    //     }
    // }
}
