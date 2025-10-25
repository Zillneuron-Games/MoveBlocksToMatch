using System;
using Zillneuron.UILayout;

public class SettingsDialog : ADialog
{
    private Action onClickClose;

    public static ADialogParameters CreateDialogParameters(Action onClickClose)
    {
        return new SettingsDialogParameters(onClickClose);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        SettingsDialogParameters currentParameters = parameters as SettingsDialogParameters;

        onClickClose = currentParameters.OnClickClose;
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

    public void OnClick_Close()
    {
        onClickClose();
    }
}
