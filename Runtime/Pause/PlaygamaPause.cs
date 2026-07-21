using MirraGames.SDK.Common;
using Playgama;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IPause))]
    public class PlaygamaPause : CommonPause {

        public PlaygamaPause(IEventAggregator aggregator, IEventDispatcher dispatcher) : base(aggregator) {
            dispatcher.OnApplicationPause += OnApplicationPause;
            dispatcher.OnApplicationFocus += OnApplicationFocus;
            dispatcher.Start += () => {
                Bridge.platform.pauseStateChanged += OnPauseStateChanged;
            };
        }
        
        private void OnPauseStateChanged(bool state) {
            Logger.CreateText(this, nameof(OnPauseStateChanged), state);
            Register(nameof(OnPauseStateChanged), state);
        }

        public void OnApplicationFocus(bool focusStatus) {
            Register(nameof(OnApplicationFocus), !focusStatus);
        }

        public void OnApplicationPause(bool pauseStatus) {
            Register(nameof(OnApplicationPause), pauseStatus);
        }

    }

}