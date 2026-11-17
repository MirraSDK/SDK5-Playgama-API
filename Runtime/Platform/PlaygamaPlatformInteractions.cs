using MirraGames.SDK.Common;
using Playgama;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IPlatformInteractions))]
    public class PlaygamaPlatformInteractions : CommonPlatformInteractions {

        private readonly string shareConfigId;

        public PlaygamaPlatformInteractions(PlaygamaPlatformInteractions_Configuration configuration) {
            shareConfigId = string.IsNullOrEmpty(configuration?.ShareConfigId) ? "default" : configuration.ShareConfigId;
        }

        protected override void RateGameImpl() {
            if (Bridge.social.isRateSupported == false) {
                Logger.NotAvailableWarning(this, nameof(RateGameImpl));
                return;
            }
            Bridge.social.Rate((isSuccess) => {
                Logger.CreateText(this, nameof(RateGameImpl), isSuccess);
            });
        }

        /// <summary>
        /// Bridge v2 shares a preset from the 'social.shares' list of playgama-bridge-config.json,
        /// so the MirraSDK message text is not sent; the configured preset id is used instead.
        /// </summary>
        protected override void ShareGameImpl(string messageText) {
            if (Bridge.social.isShareSupported == false) {
                Logger.NotAvailableWarning(this, nameof(ShareGameImpl));
                return;
            }
            Bridge.social.Share(shareConfigId, (isSuccess) => {
                Logger.CreateText(this, nameof(ShareGameImpl), shareConfigId, isSuccess);
            });
        }

    }

}
