using System;
using UnityEngine;
using TMPro;
using Zillneuron.UILayout;

public class MenuDialog : ADialog
{
    private string minimalSteps;
    private string bestSteps;
    private Action onClickRestart;
    private Action onClickMenu;
    private Action onClickCancel;

    [SerializeField]
    private TMP_Text minimalStepsText;

    [SerializeField]
    private TMP_Text bestStepsText;

    [SerializeField]
    private TMP_Text restartButtonText;

    [SerializeField]
    private TMP_Text menuButtonText;

    [SerializeField]
    private TMP_Text cancelButtonText;

    public static ADialogParameters CreateDialogParameters(string minimalSteps, string bestSteps, string restartButtonName, string menuButtonName, string cancelButtonName, Action onClickRestart, Action onClickMenu, Action onClickCancel)
    {
        return new MenuDialogParameters(minimalSteps, bestSteps, restartButtonName, menuButtonName, cancelButtonName, onClickRestart, onClickMenu, onClickCancel);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        MenuDialogParameters currentParameters = parameters as MenuDialogParameters;

        minimalSteps = currentParameters.MinimalSteps;
        bestSteps = currentParameters.BestSteps;
        onClickRestart = currentParameters.OnClickRestart;
        onClickMenu = currentParameters.OnClickMenu;
        onClickCancel = currentParameters.OnClickCancel;

        minimalStepsText.text = minimalSteps;
        bestStepsText.text = bestSteps;
        restartButtonText.text = currentParameters.RestartButtonName;
        menuButtonText.text = currentParameters.MenuButtonName;
        cancelButtonText.text = currentParameters.CancelButtonName;
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }

    public void OnClick_Restart()
    {
        onClickRestart();
    }

    public void OnClick_Menu()
    {
        onClickMenu();
    }

    public void OnClick_Cancel()
    {
        onClickCancel();
    }
}
