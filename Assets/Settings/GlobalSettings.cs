using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "ScriptableObjects/Global Settings")]
public sealed class GlobalSettings : ScriptableObject
{
    [Header("New Player Defaults")]
    [SerializeField, Min(0)]
    private int startingHints;

    [SerializeField]
    private bool soundEnabled;

    [SerializeField]
    private bool musicEnabled;

    [SerializeField]
    private ELanguageCode language;

    [Header("Gameplay")]
    [SerializeField]
    private int levelsCount;

    [SerializeField, Min(0.01f)]
    private float blockMovementSpeed;

    [SerializeField, Min(0.001f)]
    private float stoppingDistance;

    [SerializeField, Min(0)]
    private float resultsDelay;

    [SerializeField, Min(1)]
    private int maximumSteps;

    [SerializeField, Min(1)]
    private float swipeThreshold;

    [Header("Shop")]
    [SerializeField, Min(0)]
    private int fromAdsProductsCount;

    [SerializeField]
    private StoreProductSettings[] storeProducts = Array.Empty<StoreProductSettings>();

    [Header("Cloud Save")]
    [SerializeField, Min(1)]
    private int cloudReconciliationLimit;

    public int StartingHints
    {
        get
        {
            return startingHints;
        }
    }

    public bool SoundEnabled
    {
        get
        {
            return soundEnabled;
        }
    }

    public bool MusicEnabled
    {
        get
        {
            return musicEnabled;
        }
    }

    public ELanguageCode Language
    {
        get
        {
            return language;
        }
    }

    public int LevelsCount
    {
        get
        {
            return levelsCount;
        }
    }

    public float BlockMovementSpeed
    {
        get
        {
            return blockMovementSpeed;
        }
    }

    public float StoppingDistance
    {
        get
        {
            return stoppingDistance;
        }
    }

    public float ResultsDelay
    {
        get
        {
            return resultsDelay;
        }
    }

    public int MaximumSteps
    {
        get
        {
            return maximumSteps;
        }
    }

    public float SwipeThreshold
    {
        get
        {
            return swipeThreshold;
        }
    }

    public int FromAdsProductsCount
    {
        get
        {
            return fromAdsProductsCount;
        }
    }

    public IReadOnlyList<StoreProductSettings> StoreProducts
    {
        get
        {
            return storeProducts;
        }
    }

    public int CloudReconciliationLimit
    {
        get
        {
            return cloudReconciliationLimit;
        }
    }
}

[Serializable]
public sealed class StoreProductSettings
{
    [SerializeField]
    private string productId;

    [SerializeField]
    private ProductType purchaseType;

    [SerializeField, Min(0)]
    private int grantAmount;

    public string ProductId
    {
        get
        {
            return productId;
        }
    }

    public ProductType PurchaseType
    {
        get
        {
            return purchaseType;
        }
    }

    public int GrantAmount
    {
        get
        {
            return grantAmount;
        }
    }
}
