using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PDT.Plugins.Shure.DSP
{
    public class ShureDspDeviceFactory : EssentialsPluginDeviceFactory<ShureDspDevice>
    {
        public ShureDspDeviceFactory ()
        {
            MinimumEssentialsFrameworkVersion = "1.16.0";
            TypeNames = new List<string>
            {
                "shurep300"
            };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            try
            {
                var comms = CommFactory.CreateCommForDevice(dc);
                var props = dc.Properties.ToObject<ShureDspProps>();
                return new ShureDspDevice(dc.Key, dc.Name, props, comms, dc);
            }
            catch (Exception ex)
            {
                Debug.Console(0, "Error building device:{0} {1}", dc.Key, ex);
                throw;
            }
        }
    }
}