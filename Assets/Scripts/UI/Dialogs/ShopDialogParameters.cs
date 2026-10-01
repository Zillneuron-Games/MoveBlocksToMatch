using UnityEngine;
using Zillneuron.UILayout;

public class ShopDialogParameters : ADialogParameters
{
    private IPurchaser purchaser;

    public IPurchaser Purchaser { get { return purchaser; } }
    
    public ShopDialogParameters(IPurchaser purchaser)
    {
        this.purchaser = purchaser;
    }
}
