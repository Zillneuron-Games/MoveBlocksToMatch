using System;
using UnityEngine;

namespace Zillneuron.Ads
{
    public static class AdsSettings
    {
        public static readonly string AndroidAppId;
        public static readonly string AndroidBannerAdUnitId;
        public static readonly string AndroidInterstitialAdUnitId;

        public static readonly string IOSAppId;
        public static readonly string IOSBannerAdUnitId;
        public static readonly string IOSInterstitialAdUnitId;

        static AdsSettings()
        {
            AdsSettingsFile configurationFile = Resources.Load<AdsSettingsFile>("AdsSettings");

            AndroidAppId = configurationFile.AndroidAppId;
            AndroidBannerAdUnitId = configurationFile.AndroidBannerAdUnitId;
            AndroidInterstitialAdUnitId = configurationFile.AndroidInterstitialAdUnitId;

            IOSAppId = configurationFile.IOSAppId;
            IOSBannerAdUnitId = configurationFile.IOSBannerAdUnitId;
            IOSInterstitialAdUnitId = configurationFile.IOSInterstitialAdUnitId;
        }
    }
}