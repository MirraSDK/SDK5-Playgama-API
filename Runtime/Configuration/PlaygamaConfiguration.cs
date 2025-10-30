using MirraGames.SDK.Common;

namespace MirraGames.SDK.Playgama {

    [Configuration]
    public class PlaygamaConfiguration : Configuration {

        public override string Name { get; } = nameof(PlaygamaConfiguration);
        public override string Description { get; } = "Using PlaygamaBridge SDK API";
        public override string IconName { get; } = "Playgama";
        public override bool ReadOnly { get; } = false;

        public override string AchievementsProviderName { get; } = "FallbackAchievements";
        public override string AdsProviderName { get; } = "FallbackAds";
        public override string EventsReporterProviderName { get; } = "FallbackEventsReporter";
        public override string GameplayReporterProviderName { get; } = "FallbackGameplayReporter";
        public override string AddressablesProviderName { get; } = "UnityEngineAddressables";
        public override string AssetBundlesProviderName { get; } = "UnityEngineAssetBundles";
        public override string StreamingAssetsProviderName { get; } = "UnityEngineStreamingAssets";
        public override string AudioProviderName { get; } = "UnityEngineAudio";
        public override string BootstrapProviderName { get; } = "FallbackBootstrap";
        public override string DataProviderName { get; } = "FallbackData";
        public override string DeviceBrowserProviderName { get; } = "FallbackDeviceBrowser";
        public override string DeviceCursorProviderName { get; } = "UnityEngineDeviceCursor";
        public override string DeviceInfoProviderName { get; } = "FallbackDeviceInfo";
        public override string FlagsProviderName { get; } = "FallbackFlags";
        public override string LanguageInfoProviderName { get; } = "FallbackLanguageInfo";
        public override string PauseProviderName { get; } = "FallbackPause";
        public override string PaymentsProviderName { get; } = "FallbackPayments";
        public override string PlatformInfoProviderName { get; } = "FallbackPlatformInfo";
        public override string PlatformInteractionsProviderName { get; } = "FallbackPlatformInteractions";
        public override string PlayerAccountProviderName { get; } = "FallbackPlayerAccount";
        public override string DateTimeProviderName { get; } = "SystemDateTime";
        public override string TimeScaleProviderName { get; } = "UnityEngineTimeScale";

    }

}