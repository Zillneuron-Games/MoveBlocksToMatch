using System;
using UnityEngine;
using Zillneuron.UILayout;

public class GameOverDialogParameters : ADialogParameters
{
    private string message;
    private string okButtonName;
    private Action onClickOk;

    public string Message => message;
    public string OkButtonName => okButtonName;
    public Action OnClickOk => onClickOk;

    public GameOverDialogParameters(string message, string okButtonName, Action onClickOk)
    {
        this.message = message;
        this.okButtonName = okButtonName;
        this.onClickOk = onClickOk;
    }
}
