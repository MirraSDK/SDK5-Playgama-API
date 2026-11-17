using MirraGames.SDK.Common;
using Playgama;
using BridgeDeviceType = Playgama.Modules.Device.DeviceType;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IDeviceInfo))]
    public class PlaygamaDeviceInfo : CommonDeviceInfo {

        /// <summary>
        /// Tablets get the touch layout too.
        /// </summary>
        protected override bool GetIsMobileImpl() {
            BridgeDeviceType type = Bridge.device.type;
            return type == BridgeDeviceType.Mobile || type == BridgeDeviceType.Tablet;
        }

        protected override SystemType GetSystemTypeImpl() {
            return SystemType.Unknown;
        }

    }

}
