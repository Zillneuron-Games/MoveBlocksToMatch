using System;
using UnityEngine;

namespace Zillneuron.Ads
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "AdsSettings File", order = 1)]
    public class AdsSettingsFile : ScriptableObject
    {
        public string AndroidAppId;
        public string AndroidBannerAdUnitId;
        public string AndroidInterstitialAdUnitId;

        public string IOSAppId;
        public string IOSBannerAdUnitId;
        public string IOSInterstitialAdUnitId;
    }
}