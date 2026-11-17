using MirraGames.SDK.Common;
using Playgama;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IPause))]
    public class PlaygamaPause : CommonPause {

        private bool isFocusLost;

        public PlaygamaPause(IEventAggregator aggregator, IEventDispatcher dispatcher) : base(aggregator) {
            dispatcher.OnApplicationFocus += OnApplicationFocus;
            dispatcher.OnApplicationPause += OnApplicationPause;
            dispatcher.LateUpdate += ReleaseFocusOnInput;
            Bridge.platform.pauseStateChanged += OnPauseStateChanged;
        }

        /// <summary>
        /// The platform pauses the game for ads, hidden tabs and its own overlays.
        /// </summary>
        private void OnPauseStateChanged(bool isPaused) {
            Logger.CreateText(this, nameof(OnPauseStateChanged), isPaused);
            Register(nameof(OnPauseStateChanged), isPaused);
        }

        public void OnApplicationFocus(bool focusStatus) {
            isFocusLost = !focusStatus;
            Register(nameof(OnApplicationFocus), !focusStatus);
        }

        public void OnApplicationPause(bool pauseStatus) {
            Register(nameof(OnApplicationPause), pauseStatus);
        }

        /// <summary>
        /// An ad iframe takes the page focus and can disappear without the window ever getting it back, so Unity never
        /// reports focus again and the game stays paused. A closed ad means the player is back, so its focus pause is
        /// released too.
        /// </summary>
        public override void OnEvent(PauseSourceEvent eventData) {
            base.OnEvent(eventData);
            if (eventData.Pause == false && IsAdSource(eventData.Source)) {
                ReleaseFocusPause();
            }
        }

        private static bool IsAdSource(string source) {
            return source == nameof(CommonAds.InvokeInterstitial) || source == nameof(CommonAds.InvokeRewarded);
        }

        /// <summary>
        /// Clicks, taps and keys only reach an unfocused game when the browser lost a focus event, so the first press
        /// releases a focus pause that is still held for any other reason.
        /// </summary>
        private void ReleaseFocusOnInput() {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (isFocusLost && HasPressThisFrame()) {
                ReleaseFocusPause();
            }
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private static bool HasPressThisFrame() {
            if (global::UnityEngine.Input.anyKeyDown) return true;
            for (int index = 0; index < global::UnityEngine.Input.touchCount; index++) {
                if (global::UnityEngine.Input.GetTouch(index).phase == global::UnityEngine.TouchPhase.Began) return true;
            }
            return false;
        }
#endif

        private void ReleaseFocusPause() {
            if (isFocusLost == false) return;
            isFocusLost = false;
            Register(nameof(OnApplicationFocus), false);
        }

    }

}
