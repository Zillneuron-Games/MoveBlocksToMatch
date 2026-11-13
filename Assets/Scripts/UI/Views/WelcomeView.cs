using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zillneuron.UILayout;
using Zillneuron.Blockmates.Services.SignIn;

public class WelcomeView : ACompleteView
{
    private Canvas canvas;
    private bool choosingAccount;

    [SerializeField]
    protected TMP_Text titleText;

    [SerializeField]
    protected TMP_Text messageText;

    [SerializeField]
    protected TMP_Text logInButtonText;

    [SerializeField]
    protected TMP_Text playAsGuestButtonText;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        canvas = GetComponentInParent<Canvas>(true);
    }

    protected override void OnLaunch()
    {
        canvas.enabled = true;

        base.OnLaunch();
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        canvas.enabled = false;
    }

    public void OnClick_LogIn()
    {
        ChooseAccountAsync(AccountService.Instance.LogInAsync, "Could not sign in. Please try again.");
    }

    public void OnClick_PlayAsGuest()
    {
        ChooseAccountAsync(AccountService.Instance.PlayAsGuestAsync, "Could not start guest play. Please try again.");
    }

    private async void ChooseAccountAsync(Func<Task> chooseAccount, string errorMessage)
    {
        if (choosingAccount)
        {
            return;
        }

        choosingAccount = true;
        try
        {
            await chooseAccount();
            await LocalizationManager.Instance.Initialize(AccountService.Instance.LanguageCode);
            ChangeActivityToNext<MainMenuView>();
        }
        catch (Exception error)
        {
            string message = error is OperationCanceledException ? "Sign-in was cancelled." : errorMessage;
            if (error is PlatformNotSupportedException)
            {
                message = error.Message;
            }

            ADialogParameters parameters = AlertDialog.CreateDialogParameters(message, "OK", () => ViewContext.Instance.HideDialog<AlertDialog>());
            ViewContext.Instance.OpenDialog<AlertDialog>(parameters);
        }
        finally
        {
            choosingAccount = false;
        }
    }
}
