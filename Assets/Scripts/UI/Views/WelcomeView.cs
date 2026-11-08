using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class WelcomeView : ACompleteView
{
    private Canvas canvas;

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
        LogIn();
    }

    private async void LogIn()
    {
        LoginOutcome result = await AccountService.Instance.LogInAsync(confirmReplaceGuestProgress: ConfirmGuestLossAsync);
    }

    private async Task<bool> ConfirmGuestLossAsync()
    {
        return true;
    }

    public void OnClick_PlayAsGuest()
    {
        PlayAsGuest();
    }

    private async void PlayAsGuest()
    {
        await AccountService.Instance.PlayAsGuestAsync();
    }
}
