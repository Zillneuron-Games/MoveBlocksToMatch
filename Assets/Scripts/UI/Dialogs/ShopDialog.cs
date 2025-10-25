using Zillneuron.UILayout;

public class ShopDialog : ADialog
{
    public static ADialogParameters CreateDialogParameters()
    {
        return new ShopDialogParameters();
    }

    public override void SetUp(ADialogParameters parameters)
    {
        ShopDialogParameters currentParameters = parameters as ShopDialogParameters;
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

    public void Close_ButtonHandler()
    {
        ViewContext.Instance.HideDialog<ShopDialog>();
    }
}
