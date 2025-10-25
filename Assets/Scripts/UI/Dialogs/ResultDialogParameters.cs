using System;
using UnityEngine;
using Zillneuron.UILayout;

public class ResultDialogParameters : ADialogParameters
{
    private string steps;
    private string minimalSteps;
    private string bestSteps;
    private bool isNextLevelAvailable;
    private string restartButtonName;
    private string menuButtonName;
    private string nextButtonName;
    private Action onClickRestart;
    private Action onClickMenu;
    private Action onClickNext;

    public string Steps => steps;
    public string MinimalSteps => minimalSteps;
    public string BestSteps => bestSteps;
    public bool IsNextLevelAvailable => isNextLevelAvailable;
    public string RestartButtonName => restartButtonName;
    public string MenuButtonName => menuButtonName;
    public string NextButtonName => nextButtonName;
    public Action OnClickRestart => onClickRestart;
    public Action OnClickMenu => onClickMenu;
    public Action OnClickNext => onClickNext;

    public ResultDialogParameters(string steps, string bestSteps, string minimalSteps, bool isNextLevelAvailable, string restartButtonName, string menuButtonName, string nextButtonName, Action onClickRestart, Action onClickMenu, Action onClickNext)
    {
        this.steps = steps;
        this.bestSteps = bestSteps;
        this.minimalSteps = minimalSteps;
        this.isNextLevelAvailable = isNextLevelAvailable;
        this.restartButtonName = restartButtonName;
        this.menuButtonName = menuButtonName;
        this.nextButtonName = nextButtonName;
        this.onClickRestart = onClickRestart;
        this.onClickMenu = onClickMenu;
        this.onClickNext = onClickNext;
    }
}
