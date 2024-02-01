using PepperDash.Essentials.Core.DeviceInfo;

namespace PDT.Plugins.Shure.DSP
{
    public static class DeviceInfoExt
    {
        public static DeviceInfo WithIpAddress(this DeviceInfo self, string ipAddress)
        {
            return self.IpAddress == ipAddress
                ? self
                : new DeviceInfo
                {
                    FirmwareVersion = self.FirmwareVersion,
                    HostName = self.HostName,
                    MacAddress = self.MacAddress,
                    IpAddress = ipAddress,
                    SerialNumber = self.SerialNumber
                };
        }

        public static DeviceInfo WithFirmware(this DeviceInfo self, string firmware)
        {
            return self.FirmwareVersion == firmware
                ? self
                : new DeviceInfo
                {
                    FirmwareVersion = firmware,
                    HostName = self.HostName,
                    MacAddress = self.MacAddress,
                    IpAddress = self.IpAddress,
                    SerialNumber = self.SerialNumber
                };
        }

        public static DeviceInfo WithSerialNumber(this DeviceInfo self, string serialNumber)
        {
            return self.SerialNumber == serialNumber
                ? self
                : new DeviceInfo
                {
                    FirmwareVersion = self.FirmwareVersion,
                    HostName = self.HostName,
                    MacAddress = self.MacAddress,
                    IpAddress = self.IpAddress,
                    SerialNumber = serialNumber
                };
        }
    }
}