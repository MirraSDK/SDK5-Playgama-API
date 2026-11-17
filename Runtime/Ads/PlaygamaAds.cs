using MirraGames.SDK.Common;
using Playgama;
using Playgama.Modules.Advertisement;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    /// <summary>
    /// Playgama ads. An ad counts as visible from the request until its final state, so a second request
    /// made while the first one loads is refused by <see cref="CommonAds"/> instead of taking over its callbacks.
    /// </summary>
    [Provider(typeof(IAds))]
    public class PlaygamaAds : CommonAds {

        private Action onInterstitialOpen;
        private Action<bool> onInterstitialClose;
        private bool isInterstitialOpened;

        private Action onRewardedOpen;
        private Action<bool> onRewardedClose;
        private bool isRewardedOpened;
        private bool isPlayerRewarded;

        public PlaygamaAds(IEventAggregator eventAggregator) : base(eventAggregator) {
            Bridge.advertisement.bannerStateChanged += OnBannerStateChanged;
            Bridge.advertisement.interstitialStateChanged += OnInterstitialStateChanged;
            Bridge.advertisement.rewardedStateChanged += OnRewardedStateChanged;
            SetInitialized();
        }

        // Banner

        public override bool IsBannerReady => true;
        public override bool IsBannerVisible { get; protected set; } = false;
        public override bool IsBannerAvailable => Bridge.advertisement.isBannerSupported;

        private void OnBannerStateChanged(BannerState state) {
            Logger.CreateText(this, nameof(OnBannerStateChanged), state);
            switch (state) {
                case BannerState.Shown:
                    IsBannerVisible = true;
                    break;
                case BannerState.Hidden:
                case BannerState.Failed:
                    IsBannerVisible = false;
                    break;
            }
        }

        protected override void InvokeBannerImpl() {
            Bridge.advertisement.ShowBanner();
        }

        protected override void RefreshBannerImpl() { }

        protected override void DisableBannerImpl() {
            Bridge.advertisement.HideBanner();
        }

        // Interstitial

        public override bool IsInterstitialReady => true;
        public override bool IsInterstitialVisible { get; protected set; } = false;
        public override bool IsInterstitialAvailable => Bridge.advertisement.isInterstitialSupported;

        protected override void InvokeInterstitialImpl(InterstitialParameters parameters, Action onOpen, Action<bool> onClose) {
            onInterstitialOpen = onOpen;
            onInterstitialClose = onClose;
            isInterstitialOpened = false;
            IsInterstitialVisible = true;
            Bridge.advertisement.ShowInterstitial(string.IsNullOrEmpty(parameters.PlacementId) ? null : parameters.PlacementId);
        }

        private void OnInterstitialStateChanged(InterstitialState state) {
            Logger.CreateText(this, nameof(OnInterstitialStateChanged), state);
            switch (state) {
                case InterstitialState.Opened: {
                    isInterstitialOpened = true;
                    Action onOpen = onInterstitialOpen;
                    onInterstitialOpen = null;
                    onOpen?.Invoke();
                    break;
                }
                case InterstitialState.Closed:
                    // A close without an open means the platform skipped the ad.
                    CloseInterstitial(isInterstitialOpened);
                    break;
                case InterstitialState.Failed:
                    CloseInterstitial(false);
                    break;
            }
        }

        private void CloseInterstitial(bool isSuccess) {
            Action<bool> onClose = onInterstitialClose;
            onInterstitialOpen = null;
            onInterstitialClose = null;
            isInterstitialOpened = false;
            IsInterstitialVisible = false;
            onClose?.Invoke(isSuccess);
        }

        // Rewarded

        public override bool IsRewardedReady => true;
        public override bool IsRewardedVisible { get; protected set; } = false;
        public override bool IsRewardedAvailable => Bridge.advertisement.isRewardedSupported;

        protected override void InvokeRewardedImpl(RewardedParameters parameters, Action onOpen, Action<bool> onClose) {
            onRewardedOpen = onOpen;
            onRewardedClose = onClose;
            isRewardedOpened = false;
            isPlayerRewarded = false;
            IsRewardedVisible = true;
            Bridge.advertisement.ShowRewarded(string.IsNullOrEmpty(parameters.PlacementId) ? null : parameters.PlacementId);
        }

        private void OnRewardedStateChanged(RewardedState state) {
            Logger.CreateText(this, nameof(OnRewardedStateChanged), state);
            switch (state) {
                case RewardedState.Opened: {
                    isRewardedOpened = true;
                    Action onOpen = onRewardedOpen;
                    onRewardedOpen = null;
                    onOpen?.Invoke();
                    break;
                }
                case RewardedState.Rewarded:
                    isPlayerRewarded = true;
                    break;
                case RewardedState.Closed:
                    // The reward is granted only after the Rewarded state (Playgama rule 2.2.5).
                    CloseRewarded(isRewardedOpened && isPlayerRewarded);
                    break;
                case RewardedState.Failed:
                    CloseRewarded(false);
                    break;
            }
        }

        private void CloseRewarded(bool isSuccess) {
            Action<bool> onClose = onRewardedClose;
            onRewardedOpen = null;
            onRewardedClose = null;
            isRewardedOpened = false;
            isPlayerRewarded = false;
            IsRewardedVisible = false;
            onClose?.Invoke(isSuccess);
        }

    }

}
