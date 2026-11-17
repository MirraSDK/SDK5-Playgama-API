using MirraGames.SDK.Common;
using MirraGames.SDK.UnityEngine;
using Playgama;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    /// <summary>
    /// Unity audio muted while the platform turns sound off, starting muted when it is already off (Playgama rule 3.3.4).
    /// </summary>
    [Provider(typeof(IAudio))]
    public class PlaygamaAudio : UnityEngineAudio {

        public PlaygamaAudio(UnityEngineAudio_Configuration config, IEventAggregator eventAggregator) : base(config, eventAggregator) {
            try {
                SetMuted(!Bridge.platform.isAudioEnabled);
                Bridge.platform.audioStateChanged += OnAudioStateChanged;
            }
            catch (Exception exception) {
                Logger.CreateError(this, exception);
            }
        }

        private void OnAudioStateChanged(bool isEnabled) {
            Logger.CreateText(this, nameof(OnAudioStateChanged), isEnabled);
            SetMuted(!isEnabled);
        }

    }

}
