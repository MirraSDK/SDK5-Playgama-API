using MirraGames.SDK.Common;
using Playgama;
using Playgama.Modules.Platform;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IGameplayReporter))]
    public class PlaygamaGameplayReporter : CommonGameplayReporter {

        public PlaygamaGameplayReporter() {
            SetInitialized();
        }

        protected override void GameIsReadyImpl() {
            Bridge.platform.SendMessage(PlatformMessage.GameReady);
        }

        // MirraSDK reports gameplay without a level outcome, so the Level* messages of bridge v2 would
        // mislabel failures as completions; the generic gameplay messages still work and are optional.
#pragma warning disable CS0618
        protected override void GameplayStartImpl(int level = 0) {
            Bridge.platform.SendMessage(PlatformMessage.GameplayStarted);
        }

        protected override void GameplayRestartImpl(int level = 0) {
            Bridge.platform.SendMessage(PlatformMessage.GameplayStarted);
        }

        protected override void GameplayStopImpl(int level = 0) {
            Bridge.platform.SendMessage(PlatformMessage.GameplayStopped);
        }
#pragma warning restore CS0618

    }

}
