using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using Zillneuron.InAppPurchase;

[CreateAssetMenu(fileName = "InAppPurchaseSettings", menuName = "ScriptableObjects/InAppPurchaseSettings", order = 1)]
public class InAppPurchaseSettingsScriptableObject : AInAppPurchaseSettingsScriptableObject
{
    public override List<InAppProductMetadata> GetProducts()
    {
        return new List<InAppProductMetadata>
        {
            new InAppProductMetadata("com.unity.50hints", ProductType.Consumable),
            new InAppProductMetadata("com.unity.250hints", ProductType.Consumable),
            new InAppProductMetadata("com.unity.1000hints", ProductType.Consumable),
            new InAppProductMetadata("com.unity.RemoveAds", ProductType.NonConsumable)
        };
    }
}
