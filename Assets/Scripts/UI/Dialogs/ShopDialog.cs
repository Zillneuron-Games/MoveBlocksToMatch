using Zillneuron.UILayout;

public class ShopDialog : ADialog
{
    public override void SetUp(ADialogParameters parameters)
    {
        
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
}
