using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Devices.Common.DSP;

namespace PDT.Plugins.Shure.DSP
{
    public class ShureDspDevice : DspBase, IHasDspPresets, ICommunicationMonitor, IDeviceInfoProvider, IOnline, IHasFeedback
    {
        private readonly CCriticalSection _deviceInfoLock = new CCriticalSection();

        private readonly ShureDspProps _props;
        private readonly IBasicCommunication _comms;
        private readonly CTimer _poll;

        private readonly IDictionary<ShureP300ChannelEnum, ShureDspFader> _controlPoints;

        private DeviceInfo _currentDeviceInfo = new DeviceInfo
        {
            FirmwareVersion = "",
            HostName = "",
            IpAddress = "",
            SerialNumber = ""
        };

        public static int ScaleValue(int value, int originalMin, int originalMax, int newMin, int newMax)
        {
            var originalRatio = (value - originalMin) / (originalMax - originalMin);
            var newValue = newMin + (originalRatio * (newMax - newMin));

            return newValue;
        }

        public static int ScaleToInt(int value, int originalMin, int originalMax)
        {
            return ScaleValue(value, originalMin, originalMax, int.MinValue, int.MaxValue);
        }

        public ShureDspDevice(string key, string name, ShureDspProps props, IBasicCommunication comms) : base(key, name)
        {
            _props = props;
            _comms = comms;

            _controlPoints = new Dictionary<ShureP300ChannelEnum, ShureDspFader>
            {
                {ShureP300ChannelEnum.DanteMicInput01, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput01)},
                {ShureP300ChannelEnum.DanteMicInput02, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput02)},
                {ShureP300ChannelEnum.DanteMicInput03, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput03)},
                {ShureP300ChannelEnum.DanteMicInput04, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput04)},
                {ShureP300ChannelEnum.DanteMicInput05, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput05)},
                {ShureP300ChannelEnum.DanteMicInput06, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput06)},
                {ShureP300ChannelEnum.DanteMicInput07, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput07)},
                {ShureP300ChannelEnum.DanteMicInput08, new ShureDspFader(this, ShureP300ChannelEnum.DanteMicInput08)},
                {ShureP300ChannelEnum.DanteInput09, new ShureDspFader(this, ShureP300ChannelEnum.DanteInput09)},
                {ShureP300ChannelEnum.DanteInput10, new ShureDspFader(this, ShureP300ChannelEnum.DanteInput10)},
                {ShureP300ChannelEnum.UsbInput, new ShureDspFader(this, ShureP300ChannelEnum.UsbInput)},
                {ShureP300ChannelEnum.MobileInput, new ShureDspFader(this, ShureP300ChannelEnum.MobileInput)},
                {ShureP300ChannelEnum.DanteOutput1, new ShureDspFader(this, ShureP300ChannelEnum.DanteOutput1)},
                {ShureP300ChannelEnum.DanteOutput2, new ShureDspFader(this, ShureP300ChannelEnum.DanteOutput2)},
                {ShureP300ChannelEnum.AnalogOutput1, new ShureDspFader(this, ShureP300ChannelEnum.AnalogOutput1)},
                {ShureP300ChannelEnum.AnalogOutput2, new ShureDspFader(this, ShureP300ChannelEnum.AnalogOutput2)},
                {ShureP300ChannelEnum.UsbOutput, new ShureDspFader(this, ShureP300ChannelEnum.UsbOutput)},
                {ShureP300ChannelEnum.MobileOutput, new ShureDspFader(this, ShureP300ChannelEnum.MobileOutput)},
                {ShureP300ChannelEnum.AutomixerOutput, new ShureDspFader(this, ShureP300ChannelEnum.AutomixerOutput)},
                {ShureP300ChannelEnum.AecReference, new ShureDspFader(this, ShureP300ChannelEnum.AecReference)},
            };

            Presets = new List<IDspPreset>
            {
                new ShureDspPreset {Name = "PRESET 01"},
                new ShureDspPreset {Name = "PRESET 02"},
                new ShureDspPreset {Name = "PRESET 03"},
                new ShureDspPreset {Name = "PRESET 04"},
                new ShureDspPreset {Name = "PRESET 05"},
                new ShureDspPreset {Name = "PRESET 06"},
                new ShureDspPreset {Name = "PRESET 07"},
                new ShureDspPreset {Name = "PRESET 08"},
                new ShureDspPreset {Name = "PRESET 09"},
                new ShureDspPreset {Name = "PRESET 10"},
            };

            _poll = new CTimer(_ =>
            {
                SendText("< GET 00 AUDIO_GAIN_HI_RES >");
                SendText("< GET 00 AUDIO_MUTE >");
            }, Timeout.Infinite);

            var gather = new CommunicationGather(comms, ">");
            gather.LineReceived += GatherOnLineReceived;

            CommunicationMonitor = new GenericCommunicationMonitor(this, comms, 30000, 60000, 120000, "< GET MODEL >");
            CommunicationMonitor.IsOnlineFeedback.OutputChange += (sender, args) =>
            {
                if (args.BoolValue)
                {
                    SendText("< GET 00 ALL>");
                    _poll.Reset(5000, 5000);
                }
                else
                {
                    _poll.Stop();
                }
            };

            Feedbacks = new FeedbackCollection<Feedback>
            {
                IsOnline
            };

            foreach (var shureDspFader in _controlPoints.Values)
            {
                DeviceManager.AddDevice(shureDspFader);
                Feedbacks.Add(shureDspFader.MuteFeedback);
                Feedbacks.Add(shureDspFader.VolumeLevelFeedback);

                var fader = shureDspFader;
                fader.VolumeLevelFeedback.OutputChange += (sender, args) =>
                    Debug.Console(1, this, "Volume update:{0}|{1}", fader.Key, args.IntValue);

                fader.MuteFeedback.OutputChange += (sender, args) =>
                    Debug.Console(1, this, "Mute update:{0}|{1}", fader.Key, args.BoolValue);
            }

        }

        public override void Initialize()
        {
            _comms.Connect();
            CommunicationMonitor.Start();
        }

        private void GatherOnLineReceived(object sender, GenericCommMethodReceiveTextArgs genericCommMethodReceiveTextArgs)
        {
            var rx = genericCommMethodReceiveTextArgs.Text.Trim();
            ParseResponse(rx);
        }

        public void ParseResponse(string response)
        {
            // < REP xx AUDIO_MUTE ON >
            // < REP xx AUDIO_GAIN_HI_RES yyyy > 
            // Where yyyy takes on the ASCII values
            // of 0000 to 1400. yyyy is in steps of onetenth
            // of a dB.

            const string muteIdentifier = "AUDIO_MUTE";
            const string levelIdentifier = "AUDIO_GAIN";
            const string firmwareIdentifier = "FW_VER";
            const string serialNumberIdentifier = "SERIAL_NUM";
            const string ipIdentifier = "IP_ADDR_NET_AUDIO_PRIMARY";

            try
            {
                if (response.Contains(muteIdentifier))
                {
                    ParseMuteResponse(response);
                }
                else if (response.Contains(levelIdentifier))
                {
                    ParseLevelResponse(response);
                }
                else if (response.Contains(firmwareIdentifier))
                {
                    ParseFirmwareResponse(response);
                }
                else if (response.Contains(serialNumberIdentifier))
                {
                    ParseSerialNumberResponse(response);
                }
                else if (response.Contains(ipIdentifier))
                {
                    ParseIpAddressResponse(response);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Caught an exception parsing:{0}{1}", response, ex);
            }
        }

        private void ParseMuteResponse(string response)
        {
            // < REP xx AUDIO_MUTE ON >
            var parts = response.Split(new[] {' '});
            var channelId = Convert.ToInt32(parts[2]);
            Debug.Console(1, this, "Parse mute response channel:{0}", channelId);
            var channelEnum = (ShureP300ChannelEnum)channelId;
            Debug.Console(1, this, "Parse mute response channel enum:{0}", channelEnum);

            ShureDspFader fader;
            if (_controlPoints.TryGetValue(channelEnum, out fader))
            {
                fader.VolumeIsMuted = response.Contains("ON");
            }
            else
            {
                Debug.Console(1, this, "Could not find fader with enum:{0}", channelEnum);
            }
        }

        private void ParseLevelResponse(string response)
        {
            // < REP xx AUDIO_GAIN_HI_RES yyyy >
            var parts = response.Split(new[] { ' ' });
            var channelId = Convert.ToInt32(parts[2]);
            Debug.Console(1, this, "Parse level response channel:{0}", channelId);
            var channelEnum = (ShureP300ChannelEnum)channelId;
            Debug.Console(1, this, "Parse level response channel enum:{0}", channelEnum);

            ShureDspFader fader;
            if (_controlPoints.TryGetValue(channelEnum, out fader))
            {
                var volume = Convert.ToInt32(parts[4]);
                Debug.Console(1, this, "Parse level response channel:{0} volume:{1}", channelId, volume);
                var scaledVolume = ScaleToInt(volume, 0, 1400);
                Debug.Console(1, this, "Parse level response channel:{0} scaled volume:{1}", channelId, scaledVolume);
                fader.CurrentLevel = scaledVolume;
            }
            else
            {
                Debug.Console(1, this, "Could not find fader with enum:{0}", channelEnum);
            }
        }

        private void ParseFirmwareResponse(string response)
        {
            // < REP FW_VER {yyyyyyyyyyyyyyyyyy} >

            var shouldUpdate = false;

            try
            {
                var parts = response.Split(new[] {' '});
                var firmware = parts[3];
                var oldDeviceInfo = _currentDeviceInfo;
                _currentDeviceInfo = oldDeviceInfo.WithFirmware(firmware);
                shouldUpdate = !ReferenceEquals(oldDeviceInfo, _currentDeviceInfo);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "Caught an exception parsing FW {0}", ex);
            }

            if (!shouldUpdate)
                return;

            OnDeviceInfoChanged(_currentDeviceInfo);
        }

        private void ParseSerialNumberResponse(string response)
        {
            // < REP SERIAL_NUM {yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy} >

            var shouldUpdate = false;

            try
            {
                var parts = response.Split(new[] { ' ' });
                var serialNumber = parts[3];
                var oldDeviceInfo = _currentDeviceInfo;
                _currentDeviceInfo = oldDeviceInfo.WithSerialNumber(serialNumber);
                shouldUpdate = !ReferenceEquals(oldDeviceInfo, _currentDeviceInfo);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "Caught an exception parsing SN {0}", ex);
            }

            if (!shouldUpdate)
                return;

            OnDeviceInfoChanged(_currentDeviceInfo);
        }

        private void ParseIpAddressResponse(string response)
        {
            // < REP IP_ADDR_NET_AUDIO_PRIMARY {yyyyyyyyyyyyyyy} >

            var shouldUpdate = false;

            try
            {
                var parts = response.Split(new[] { ' ' });
                var ipAddress = parts[3];
                var oldDeviceInfo = _currentDeviceInfo;
                _currentDeviceInfo = oldDeviceInfo.WithIpAddress(ipAddress);
                shouldUpdate = !ReferenceEquals(oldDeviceInfo, _currentDeviceInfo);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "Caught an exception parsing IP {0}", ex);
            }

            if (!shouldUpdate)
                return;

            OnDeviceInfoChanged(_currentDeviceInfo);
        }

        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textToSend = text.Trim();
            _comms.SendText(textToSend);
        }

        public void RecallPreset(IDspPreset preset)
        {
            // < SET PRESET nn >
            const string commandTempate = "< SET {0} >";
            var command = string.Format(commandTempate, preset.Name);
            SendText(command);
        }

        public List<IDspPreset> Presets { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public BoolFeedback IsOnline
        {
             get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

        public void UpdateDeviceInfo()
        {
            SendText("< GET SERIAL_NUM >");
            SendText("< GET FW_VER >");
            SendText("< GET IP_ADDR_NET_AUDIO_PRIMARY >");
        }

        public DeviceInfo DeviceInfo
        {
            get
            {
                return _currentDeviceInfo;
            }
        }

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        private void OnDeviceInfoChanged(DeviceInfo deviceInfo)
        {
            var handler = DeviceInfoChanged;
            if (handler != null)
                handler(this, new DeviceInfoEventArgs { DeviceInfo = deviceInfo });
        }
    }
}

