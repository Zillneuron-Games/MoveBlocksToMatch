using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zillneuron.UILayout;

public class ResultDialog : ADialog
{
    private string steps;
    private string bestSteps;
    private string minimalSteps;
    private bool isNextLevelAvailable;
    private Action onClickRestart;
    private Action onClickMenu;
    private Action onClickNext;

    [SerializeField]
    private TMP_Text stepsText;

    [SerializeField]
    private TMP_Text bestStepsText;

    [SerializeField]
    private TMP_Text minimalStepsText;

    [SerializeField]
    private TMP_Text restartButtonText;

    [SerializeField]
    private TMP_Text menuButtonText;

    [SerializeField]
    private TMP_Text nextButtonText;

    [SerializeField]
    private Button nextButton;

    public static ADialogParameters CreateDialogParameters(string steps, string bestSteps, string minimalSteps, bool isNextLevelAvailable, string restartButtonName, string menuButtonName, string nextButtonName, Action onClickRestart, Action onClickMenu, Action onClickNext)
    {
        return new ResultDialogParameters(steps, minimalSteps, bestSteps, isNextLevelAvailable, restartButtonName, menuButtonName, nextButtonName, onClickRestart, onClickMenu, onClickNext);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        ResultDialogParameters currentParameters = parameters as ResultDialogParameters;

        steps = currentParameters.Steps;        
        bestSteps = currentParameters.BestSteps;
        minimalSteps = currentParameters.MinimalSteps;
        isNextLevelAvailable = currentParameters.IsNextLevelAvailable;
        onClickRestart = currentParameters.OnClickRestart;
        onClickMenu = currentParameters.OnClickMenu;
        onClickNext = currentParameters.OnClickNext;

        stepsText.text = steps;        
        bestStepsText.text = bestSteps;
        minimalStepsText.text = minimalSteps;
        restartButtonText.text = currentParameters.RestartButtonName;
        menuButtonText.text = currentParameters.MenuButtonName;
        nextButtonText.text = currentParameters.NextButtonName;
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        nextButton.interactable = isNextLevelAvailable;

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

    public void OnClick_Next()
    {
        onClickNext();
    }
}
