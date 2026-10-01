using UnityEngine;

public interface IProductPurchaser
{
    void BuyFromStore(string productId);
    void GetFromAds(int count);
}
