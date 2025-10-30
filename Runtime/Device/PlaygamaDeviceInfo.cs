using MirraGames.SDK.Common;
using Playgama;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IDeviceInfo))]
    public class PlaygamaDeviceInfo : CommonDeviceInfo {

        protected override bool GetIsMobileImpl() {
            return Bridge.device.type == global::Playgama.Modules.Device.DeviceType.Mobile;
        }

        protected override SystemType GetSystemTypeImpl() {
            return SystemType.Unknown;
        }

    }

}