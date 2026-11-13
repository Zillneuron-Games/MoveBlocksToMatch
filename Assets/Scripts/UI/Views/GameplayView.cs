using System;
using System.Security.Cryptography;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
using Zillneuron.Blockmates.Services.SignIn;
using Zillneuron.UILayout;

public class GameplayView : ACompleteView
{
    private Canvas canvas;

    [SerializeField]
    private GameplayManager gameplayManager;

    [SerializeField]
    private PurchaseManager purchaseManager;

    [SerializeField]
    private TMP_Text stepsText;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text hintsCountText;

    [SerializeField]
    private Image hintIndicator;

    [SerializeField]
    private Sprite hintIndicatorBackground;

    [SerializeField]
    private Sprite noHintIndicatorBackground;

    [SerializeField]
    private Image[] directionImages;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        canvas = GetComponentInParent<Canvas>(true);
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        canvas.enabled = true;

        gameplayManager.onStartGame -= StartGame;
        gameplayManager.onStartGame += StartGame;

        gameplayManager.onStepsUpdate -= UpdateSteps;
        gameplayManager.onStepsUpdate += UpdateSteps;

        gameplayManager.onShowHint -= ShowHint;
        gameplayManager.onShowHint += ShowHint;

        gameplayManager.onShowMenu -= ShowMenu;
        gameplayManager.onShowMenu += ShowMenu;

        gameplayManager.onShowSettings -= ShowSettings;
        gameplayManager.onShowSettings += ShowSettings;

        gameplayManager.onShowError -= ShowError;
        gameplayManager.onShowError += ShowError;

        gameplayManager.onWinGame -= WinGame;
        gameplayManager.onWinGame += WinGame;

        gameplayManager.onEndGame -= EndGame;
        gameplayManager.onEndGame += EndGame;

        gameplayManager.onQuitGame -= QuitGame;
        gameplayManager.onQuitGame += QuitGame;

        gameplayManager.StartGame();

        AccountService.Instance.OnInventoryChanged -= HandleInventoryChanged;
        AccountService.Instance.OnInventoryChanged += HandleInventoryChanged;

        UpdateHints();
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameplayManager.onStartGame -= StartGame;
        gameplayManager.onStepsUpdate -= UpdateSteps;
        gameplayManager.onShowHint -= ShowHint;
        gameplayManager.onShowMenu -= ShowMenu;
        gameplayManager.onShowSettings -= ShowSettings;
        gameplayManager.onShowError -= ShowError;
        gameplayManager.onWinGame -= WinGame;
        gameplayManager.onEndGame -= EndGame;
        gameplayManager.onQuitGame -= QuitGame;
        AccountService.Instance.OnInventoryChanged -= HandleInventoryChanged;

        canvas.enabled = false;
    }

    protected override void OnPause()
    {
        base.OnPause();

        InputManager.Instance.Disable();
    }

    protected override void OnResume()
    {
        base.OnResume();

        InputManager.Instance.Enable();
    }

    private void UpdateHints()
    {
        int hintsCount = AccountService.Instance.Hints;
        hintsCountText.text = hintsCount.ToString();
        hintIndicator.sprite = hintsCount > 0 ? hintIndicatorBackground : noHintIndicatorBackground;
    }

    private void HandleInventoryChanged(InventoryData inventory)
    {
        UpdateHints();
    }

    private void StartGame(int level, int steps)
    {
        levelText.text = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Level)} : {level}";
        stepsText.text = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Steps)} : {steps}";
    }

    private void UpdateSteps(int steps)
    {
        for (int i = 0; i < directionImages.Length; i++)
        {
            directionImages[i].gameObject.SetActive(false);
        }

        stepsText.text = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Steps)} : {steps}";
    }

    private void ShowHint(EDirection direction)
    {
        for (int i = 0; i < directionImages.Length; i++)
        {
            directionImages[i].gameObject.SetActive(false);
        }

        directionImages[(int)direction].gameObject.SetActive(true);
    }

    private void ShowMenu(int minimalSteps, int bestSteps)
    {
        string minimalStepsText = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.MinimalSteps)} : {minimalSteps}";
        string bestStepsText = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.BestSteps)} : {bestSteps}";
        string restartText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Restart);
        string menuText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Menu);
        string cancelText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Cancel);

        ADialogParameters parameters = MenuDialog.CreateDialogParameters(
            minimalStepsText,
            bestStepsText,
            restartText,
            menuText,
            cancelText,
            () => { ViewContext.Instance.HideDialog<MenuDialog>(); RestartGame(); },
            () => { ViewContext.Instance.HideDialog<MenuDialog>(); BackToMainMenu(); },
            () => { ViewContext.Instance.HideDialog<MenuDialog>(); CloseDialog(); });

        ViewContext.Instance.OpenDialog<MenuDialog>(parameters);
    }

    private void ShowSettings()
    {
        ADialogParameters parameters = SettingsDialog.CreateDialogParameters(
            () => { ViewContext.Instance.HideDialog<SettingsDialog>(); CloseDialog(); });

        ViewContext.Instance.OpenDialog<SettingsDialog>(parameters);
    }

    private void ShowError(EErrorType errorType)
    {
        string messageText = messageText = LocalizationManager.Instance.GetPhrase(PhraseCollection.ErrorDefault); 
        
        switch (errorType)
        {
            case EErrorType.AvailableSteps: messageText = LocalizationManager.Instance.GetPhrase(PhraseCollection.ErrorAvailableSteps); break;
            case EErrorType.StepsCount: messageText = LocalizationManager.Instance.GetPhrase(PhraseCollection.ErrorStepsCount); break;
        }
        
        string okText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Restart);

        ADialogParameters parameters = GameOverDialog.CreateDialogParameters(
            messageText,
            okText,
            () => { ViewContext.Instance.HideDialog<GameOverDialog>(); RestartGame(); });

        ViewContext.Instance.OpenDialog<GameOverDialog>(parameters);
    }

    private void WinGame(int steps, int bestSteps, int minimalSteps, bool isNextLevelAvailable)
    {
        string stepsText = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.Steps)} : {steps}";
        string minimalStepsText = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.MinimalSteps)} : {minimalSteps}";
        string bestStepsText = $"{LocalizationManager.Instance.GetPhrase(PhraseCollection.BestSteps)} : {bestSteps}";
        string restartText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Restart);
        string menuText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Menu);
        string nextText = LocalizationManager.Instance.GetPhrase(PhraseCollection.Next);

        ADialogParameters parameters = ResultDialog.CreateDialogParameters(
            stepsText,
            minimalStepsText,
            bestStepsText,
            isNextLevelAvailable,
            restartText,
            menuText,
            nextText,
            () => { ViewContext.Instance.HideDialog<ResultDialog>(); RestartGame(); },
            () => { ViewContext.Instance.HideDialog<ResultDialog>(); BackToMainMenu(); },
            () => { ViewContext.Instance.HideDialog<ResultDialog>(); NextGame(); });

        ViewContext.Instance.OpenDialog<ResultDialog>(parameters);
    }

    private void EndGame()
    {
        
    }

    private void QuitGame()
    {
        ChangeActivityToPrevious();
    }

    private void BackToMainMenu()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Quit);
        InputManager.Instance.ButtonPressed(this, args);
    }

    private void RestartGame()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Reload);
        InputManager.Instance.ButtonPressed(this, args);
    }

    private void NextGame()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Next);
        InputManager.Instance.ButtonPressed(this, args);
    }

    private void CloseDialog()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Escape);
        InputManager.Instance.ButtonPressed(this, args);
    }

    #region Buttons

    public void OnClick_Undo()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Back);
        InputManager.Instance.ButtonPressed(this, args);
    }

    public void OnClick_Settings()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Settings);
        InputManager.Instance.ButtonPressed(this, args);
    }

    public void OnClick_Hint()
    {
        AccountService account = AccountService.Instance;
        if (!account.IsReady)
        {
            return;
        }

        if (account.Hints == 0)
        {
            ADialogParameters parameters = ShopDialog.CreateDialogParameters(purchaseManager);
            ViewContext.Instance.OpenDialog<ShopDialog>(parameters);
            return;
        }

        InputEventArgs args = new InputEventArgs(EInputEvent.Hint);
        InputManager.Instance.ButtonPressed(this, args);
    }

    public void OnClick_Menu()
    {
        InputEventArgs args = new InputEventArgs(EInputEvent.Menu);
        InputManager.Instance.ButtonPressed(this, args);
    }

    #endregion Buttons
}
