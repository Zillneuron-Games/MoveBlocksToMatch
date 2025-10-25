using System;
using UnityEngine;
using TMPro;
using Zillneuron.UILayout;

public class GameOverDialog : ADialog
{
    protected string message;
    protected Action onClickOk;

    [SerializeField]
    protected TMP_Text messageText;

    [SerializeField]
    protected TMP_Text okButtonText;

    public string Message => message;

    public static ADialogParameters CreateDialogParameters(string message, string okButtonName, Action onClickOk)
    {
        return new GameOverDialogParameters(message, okButtonName, onClickOk);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        GameOverDialogParameters currentParameters = parameters as GameOverDialogParameters;

        message = currentParameters.Message;
        onClickOk = currentParameters.OnClickOk;

        messageText.text = message;
        okButtonText.text = currentParameters.OkButtonName;
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

    public void OnClick_Ok()
    {
        onClickOk();
    }
}
