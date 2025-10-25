using System;
using Zillneuron.UILayout;

public class MenuDialogParameters : ADialogParameters
{
    private string minimalSteps;
    private string bestSteps;
    private string restartButtonName;
    private string menuButtonName;
    private string cancelButtonName;
    private Action onClickRestart;
    private Action onClickMenu;
    private Action onClickCancel;

    public string MinimalSteps => minimalSteps;
    public string BestSteps => bestSteps;
    public string RestartButtonName => restartButtonName;
    public string MenuButtonName => menuButtonName;
    public string CancelButtonName => cancelButtonName;
    public Action OnClickRestart => onClickRestart;
    public Action OnClickMenu => onClickMenu;
    public Action OnClickCancel => onClickCancel;

    public MenuDialogParameters(string minimalSteps, string bestSteps, string restartButtonName, string menuButtonName, string cancelButtonName, Action onClickRestart, Action onClickMenu, Action onClickCancel)
    {
        this.minimalSteps = minimalSteps;
        this.bestSteps = bestSteps;
        this.restartButtonName = restartButtonName;
        this.menuButtonName = menuButtonName;
        this.cancelButtonName = cancelButtonName;
        this.onClickRestart = onClickRestart;
        this.onClickMenu = onClickMenu;
        this.onClickCancel = onClickCancel;
    }
}
