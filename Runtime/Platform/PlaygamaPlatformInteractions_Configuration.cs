using MirraGames.SDK.Common;
using UnityEngine;

namespace MirraGames.SDK.Playgama {

    [ProviderConfiguration(typeof(PlaygamaPlatformInteractions))]
    public class PlaygamaPlatformInteractions_Configuration : PropertyGroup {

        public override string Name => nameof(PlaygamaPlatformInteractions);

        /// <summary>
        /// Id of the entry in the 'social.shares' list of playgama-bridge-config.json used when sharing the game.
        /// </summary>
        [field: SerializeField] public string ShareConfigId { get; private set; } = "default";

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new(
                    nameof(ShareConfigId),
                    getter: () => ShareConfigId,
                    setter: (value) => ShareConfigId = value
                )
            };
        }

    }

}
