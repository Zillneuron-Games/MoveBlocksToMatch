using System;
using UnityEngine;
using Zillneuron.UILayout;

public class SettingsDialogParameters : ADialogParameters
{
    private Action onClickClose;

    public Action OnClickClose => onClickClose;

    public SettingsDialogParameters(Action onClickClose)
    {
        this.onClickClose = onClickClose;
    }
}
