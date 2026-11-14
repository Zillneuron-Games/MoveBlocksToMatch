using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using Zillneuron.Utilities;

namespace Zillneuron.Ads
{
    public class AdsService : AService<AdsService>
    {
        private event Action<EAdResult, string> onBannerNewEventRised;
        private event Action<EAdResult, string> onInterstitialNewEventRised;

        private string appKey;
        private string adBannerUnitId;
        private string adInterstitialUnitId;

        private bool bannersAllowed;
        private bool interstitialsAllowed;
        private bool logsAllowed;

        private BannerSettings bannerSettings;

        private LevelPlayBannerAd bannerAd;
        private LevelPlayInterstitialAd interstitialAd;

        private bool levelPlayInitialized;

        protected override void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            base.Initialize();

#if !UNITY_EDITOR && UNITY_ANDROID

            appKey = AdsSettings.AndroidAppId;
            adBannerUnitId = AdsSettings.AndroidBannerAdUnitId;
            adInterstitialUnitId = AdsSettings.AndroidInterstitialAdUnitId;

#elif !UNITY_EDITOR && UNITY_IOS

            appKey = AdsSettings.IOSAppId;
            adBannerUnitId = AdsSettings.IOSBannerAdUnitId;
            adInterstitialUnitId = AdsSettings.IOSInterstitialAdUnitId;

#else

            // LevelPlay supports mock ads in Editor.
            appKey = "editor";
            adBannerUnitId = "editor_banner";
            adInterstitialUnitId = "editor_interstitial";

#endif

            LevelPlay.OnInitSuccess += HandleInitializationSuccess;
            LevelPlay.OnInitFailed += HandleInitializationFailed;

            LevelPlay.Init(appKey);
        }

        private void HandleInitializationSuccess(LevelPlayConfiguration configuration)
        {
            levelPlayInitialized = true;

            PrintLogs("LevelPlay initialization succeeded.");

            CreateAds();
        }

        private void HandleInitializationFailed(LevelPlayInitError error)
        {
            levelPlayInitialized = false;

            PrintLogs($"LevelPlay initialization failed: {error}");
        }

        private void CreateAds()
        {
            if (bannersAllowed)
            {
                CreateBanner();
            }

            if (interstitialsAllowed)
            {
                CreateInterstitial();
            }
        }

        #region Banner

        private void CreateBanner()
        {
            if (bannerAd != null)
            {
                return;
            }

            LevelPlayAdSize adSize = GetBannerSize();
            LevelPlayBannerPosition position = GetBannerPosition();

            var configBuilder = new LevelPlayBannerAd.Config.Builder();

            configBuilder.SetSize(adSize);
            configBuilder.SetPosition(position);

            // We want ShowBanner() to control visibility explicitly.
            configBuilder.SetDisplayOnLoad(false);

#if UNITY_ANDROID
            configBuilder.SetRespectSafeArea(true);
#endif

            LevelPlayBannerAd.Config config = configBuilder.Build();

            bannerAd = new LevelPlayBannerAd(adBannerUnitId, config);

            bannerAd.OnAdLoaded += HandleOnBannerAdLoaded;
            bannerAd.OnAdLoadFailed += HandleOnBannerAdFailedToLoad;
            bannerAd.OnAdDisplayed += HandleOnBannerAdDisplayed;
            bannerAd.OnAdDisplayFailed += HandleOnBannerAdDisplayFailed;
            bannerAd.OnAdClicked += HandleOnBannerAdClicked;
            bannerAd.OnAdExpanded += HandleOnBannerAdExpanded;
            bannerAd.OnAdCollapsed += HandleOnBannerAdCollapsed;
            bannerAd.OnAdLeftApplication += HandleOnBannerAdLeftApplication;

            bannerAd.OnAdImpressionDataReady += HandleOnBannerImpressionDataReady;
        }

        private void RequestBanner()
        {
            if (!levelPlayInitialized)
            {
                PrintLogs("RequestBanner rejected: LevelPlay is not initialized.");
                return;
            }

            if (bannerAd == null)
            {
                CreateBanner();
            }

            bannerAd?.LoadAd();
        }

        private void HandleOnBannerAdLoaded(LevelPlayAdInfo adInfo)
        {
            RiseBannerNewEvent(EAdResult.Loaded, "Loaded.");

            PrintLogs($"Banner loaded: {adInfo}");
        }

        private void HandleOnBannerAdFailedToLoad(LevelPlayAdError error)
        {
            RiseBannerNewEvent(EAdResult.Failed, $"Failed to load: {error}");

            PrintLogs($"Banner failed to load: {error}");
        }

        private void HandleOnBannerAdDisplayed(LevelPlayAdInfo adInfo)
        {
            RiseBannerNewEvent(EAdResult.Opened, "Displayed.");

            PrintLogs($"Banner displayed: {adInfo}");
        }

        private void HandleOnBannerAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            RiseBannerNewEvent(EAdResult.Failed, $"Failed to display: {error}");

            PrintLogs($"Banner failed to display: {error}");
        }

        private void HandleOnBannerAdClicked(LevelPlayAdInfo adInfo)
        {
            RiseBannerNewEvent(EAdResult.Clicked, "Clicked.");

            PrintLogs($"Banner clicked: {adInfo}");
        }

        private void HandleOnBannerAdExpanded(LevelPlayAdInfo adInfo)
        {
            RiseBannerNewEvent(EAdResult.Opened, "Expanded.");

            PrintLogs($"Banner expanded: {adInfo}");
        }

        private void HandleOnBannerAdCollapsed(LevelPlayAdInfo adInfo)
        {
            RiseBannerNewEvent(EAdResult.Closed, "Collapsed.");

            PrintLogs($"Banner collapsed: {adInfo}");
        }

        private void HandleOnBannerAdLeftApplication(LevelPlayAdInfo adInfo)
        {
            PrintLogs($"Banner left application: {adInfo}");
        }

        private void HandleOnBannerImpressionDataReady(LevelPlayImpressionData impressionData)
        {
            double revenue = impressionData?.Revenue ?? 0;

            RiseBannerNewEvent(EAdResult.AdPaid, $"AdPaid: {revenue}");

            RiseBannerNewEvent(EAdResult.ImpressionRecorded, "Impression Recorded.");

            PrintLogs($"Banner impression. Revenue: {revenue}");
        }

        #endregion

        #region Interstitial

        private void CreateInterstitial()
        {
            if (interstitialAd != null)
            {
                return;
            }

            interstitialAd = new LevelPlayInterstitialAd(adInterstitialUnitId);

            interstitialAd.OnAdLoaded += HandleOnInterstitialAdLoaded;
            interstitialAd.OnAdLoadFailed += HandleOnInterstitialAdLoadFailed;
            interstitialAd.OnAdDisplayed += HandleOnInterstitialAdDisplayed;
            interstitialAd.OnAdDisplayFailed += HandleOnInterstitialAdDisplayFailed;
            interstitialAd.OnAdClicked += HandleOnInterstitialAdClicked;
            interstitialAd.OnAdClosed += HandleOnInterstitialAdClosed;
            interstitialAd.OnAdInfoChanged += HandleOnInterstitialAdInfoChanged;
            interstitialAd.OnAdImpressionDataReady += HandleOnInterstitialImpressionDataReady;
        }

        private void RequestInterstitial()
        {
            if (!levelPlayInitialized)
            {
                PrintLogs(
                    "RequestInterstitial rejected: LevelPlay is not initialized."
                );

                return;
            }

            if (interstitialAd == null)
            {
                CreateInterstitial();
            }

            interstitialAd?.LoadAd();
        }

        private void HandleOnInterstitialAdLoaded(
            LevelPlayAdInfo adInfo)
        {
            RiseInterstitialNewEvent(
                EAdResult.Loaded,
                "Loaded."
            );

            PrintLogs(
                $"Interstitial loaded: {adInfo}"
            );
        }

        private void HandleOnInterstitialAdLoadFailed(
            LevelPlayAdError error)
        {
            RiseInterstitialNewEvent(
                EAdResult.Failed,
                $"Failed to load: {error}"
            );

            PrintLogs(
                $"Interstitial failed to load: {error}"
            );
        }

        private void HandleOnInterstitialAdDisplayed(
            LevelPlayAdInfo adInfo)
        {
            RiseInterstitialNewEvent(
                EAdResult.Opened,
                "Opened."
            );

            PrintLogs(
                $"Interstitial displayed: {adInfo}"
            );
        }

        private void HandleOnInterstitialAdDisplayFailed(
            LevelPlayAdInfo adInfo,
            LevelPlayAdError error)
        {
            RiseInterstitialNewEvent(
                EAdResult.Failed,
                $"Failed to display: {error}"
            );

            PrintLogs(
                $"Interstitial failed to display: {error}"
            );

            RequestInterstitial();
        }

        private void HandleOnInterstitialAdClicked(
            LevelPlayAdInfo adInfo)
        {
            RiseInterstitialNewEvent(
                EAdResult.Clicked,
                "Clicked."
            );

            PrintLogs(
                $"Interstitial clicked: {adInfo}"
            );
        }

        private void HandleOnInterstitialAdClosed(
            LevelPlayAdInfo adInfo)
        {
            RiseInterstitialNewEvent(
                EAdResult.Closed,
                "Closed."
            );

            PrintLogs(
                $"Interstitial closed: {adInfo}"
            );

            // Prepare the next interstitial.
            RequestInterstitial();
        }

        private void HandleOnInterstitialAdInfoChanged(
            LevelPlayAdInfo adInfo)
        {
            PrintLogs(
                $"Interstitial info changed: {adInfo}"
            );
        }

        private void HandleOnInterstitialImpressionDataReady(
            LevelPlayImpressionData impressionData)
        {
            double revenue = impressionData?.Revenue ?? 0;

            RiseInterstitialNewEvent(
                EAdResult.ImpressionRecorded,
                "Impression Recorded."
            );

            RiseInterstitialNewEvent(
                EAdResult.AdPaid,
                $"AdPaid: {revenue}"
            );

            PrintLogs(
                $"Interstitial impression. Revenue: {revenue}"
            );
        }

        #endregion

        public void InitializeSettings(bool logsAllowed, bool bannersAllowed, bool interstitialsAllowed, BannerSettings bannerSettings)
        {
            this.logsAllowed = logsAllowed;
            this.bannersAllowed = bannersAllowed;
            this.interstitialsAllowed = interstitialsAllowed;
            this.bannerSettings = bannerSettings;

            /*
             * Important:
             *
             * If InitializeSettings() is called AFTER LevelPlay initialization
             * completed, CreateAds() must still be executed.
             */
            if (levelPlayInitialized)
            {
                CreateAds();
            }
        }

        public void RequestAds()
        {
            if (!levelPlayInitialized)
            {
                PrintLogs("RequestAds rejected: LevelPlay is not initialized.");

                return;
            }

            if (bannersAllowed)
            {
                RequestBanner();
            }

            if (interstitialsAllowed)
            {
                RequestInterstitial();
            }
        }

        public void ShowBanner()
        {
            if (!bannersAllowed)
            {
                return;
            }

            bannerAd?.ShowAd();
        }

        public void HideBanner()
        {
            if (!bannersAllowed)
            {
                return;
            }

            bannerAd?.HideAd();
        }

        public void ShowInterstitial()
        {
            try
            {
                if (!interstitialsAllowed)
                {
                    return;
                }

                if (interstitialAd != null && interstitialAd.IsAdReady())
                {
                    interstitialAd.ShowAd();
                }
                else
                {
                    RiseInterstitialNewEvent(EAdResult.Rejected, "Interstitial is not ready.");

                    PrintLogs("ShowInterstitial rejected: ad is not ready.");

                    RequestInterstitial();
                }
            }
            catch (Exception ex)
            {
                RiseInterstitialNewEvent(EAdResult.Rejected, "Rejected");

                PrintLogs($"[AdsService] [Exception: {ex.Message}] [Stack: {ex.StackTrace}]");
            }
        }

        #region Banner Settings Mapping

        private LevelPlayAdSize GetBannerSize()
        {
            /*
             * LevelPlay doesn't use arbitrary AdSize(width, height)
             * in the same way Google Mobile Ads does.
             *
             * Map your BannerSettings values to LevelPlay sizes.
             */

            if (bannerSettings == null)
            {
                return LevelPlayAdSize.BANNER;
            }

            if (bannerSettings.Width == 300 && bannerSettings.Height == 250)
            {
                return LevelPlayAdSize.MEDIUM_RECTANGLE;
            }

            if (bannerSettings.Height >= 90)
            {
                return LevelPlayAdSize.LARGE;
            }

            return LevelPlayAdSize.BANNER;
        }

        private LevelPlayBannerPosition GetBannerPosition()
        {
            /*
             * IMPORTANT:
             *
             * Your BannerSettings.Position currently stores a value
             * intended for Google AdPosition.
             *
             * Therefore we should NOT simply cast that number to
             * LevelPlayBannerPosition.
             *
             * For now BottomCenter is the safest equivalent.
             */

            return LevelPlayBannerPosition.BottomCenter;
        }

        #endregion

        public void AddBannerNewEventRisedHandler(Action<EAdResult, string> onNewEventRised)
        {
            onBannerNewEventRised -= onNewEventRised;
            onBannerNewEventRised += onNewEventRised;
        }

        public void RemoveBannerNewEventRisedHandler(Action<EAdResult, string> onNewEventRised)
        {
            onBannerNewEventRised -= onNewEventRised;
        }

        private void RiseBannerNewEvent(EAdResult adsResult, string message)
        {
            onBannerNewEventRised?.Invoke(adsResult, message);
        }

        private void ClearBannerNewEventRisedHandler()
        {
            onBannerNewEventRised = null;
        }

        public void AddInterstitialNewEventRisedHandler(Action<EAdResult, string> onNewEventRised)
        {
            onInterstitialNewEventRised -= onNewEventRised;
            onInterstitialNewEventRised += onNewEventRised;
        }

        public void RemoveInterstitialNewEventRisedHandler(Action<EAdResult, string> onNewEventRised)
        {
            onInterstitialNewEventRised -= onNewEventRised;
        }

        private void RiseInterstitialNewEvent(EAdResult adsResult, string message)
        {
            onInterstitialNewEventRised?.Invoke(adsResult, message);
        }

        private void ClearInterstitialNewEventRisedHandler()
        {
            onInterstitialNewEventRised = null;
        }

        private void OnDestroy()
        {
            ClearBannerNewEventRisedHandler();
            ClearInterstitialNewEventRisedHandler();

            LevelPlay.OnInitSuccess -= HandleInitializationSuccess;
            LevelPlay.OnInitFailed -= HandleInitializationFailed;

            if (bannerAd != null)
            {
                bannerAd.OnAdLoaded -= HandleOnBannerAdLoaded;
                bannerAd.OnAdLoadFailed -= HandleOnBannerAdFailedToLoad;
                bannerAd.OnAdDisplayed -= HandleOnBannerAdDisplayed;
                bannerAd.OnAdDisplayFailed -= HandleOnBannerAdDisplayFailed;
                bannerAd.OnAdClicked -= HandleOnBannerAdClicked;
                bannerAd.OnAdExpanded -= HandleOnBannerAdExpanded;
                bannerAd.OnAdCollapsed -= HandleOnBannerAdCollapsed;
                bannerAd.OnAdLeftApplication -= HandleOnBannerAdLeftApplication;
                bannerAd.OnAdImpressionDataReady -= HandleOnBannerImpressionDataReady;

                bannerAd.DestroyAd();
                bannerAd = null;
            }

            if (interstitialAd != null)
            {
                interstitialAd.OnAdLoaded -= HandleOnInterstitialAdLoaded;
                interstitialAd.OnAdLoadFailed -= HandleOnInterstitialAdLoadFailed;
                interstitialAd.OnAdDisplayed -= HandleOnInterstitialAdDisplayed;
                interstitialAd.OnAdDisplayFailed -= HandleOnInterstitialAdDisplayFailed;
                interstitialAd.OnAdClicked -= HandleOnInterstitialAdClicked;
                interstitialAd.OnAdClosed -= HandleOnInterstitialAdClosed;
                interstitialAd.OnAdInfoChanged -= HandleOnInterstitialAdInfoChanged;
                interstitialAd.OnAdImpressionDataReady -= HandleOnInterstitialImpressionDataReady;

                interstitialAd.DestroyAd();
                interstitialAd = null;
            }
        }

        private void PrintLogs(string message)
        {
            if (logsAllowed)
            {
                Debug.Log($"[LevelPlay] {message}");
            }
        }
    }
}
