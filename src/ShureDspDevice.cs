using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Devices.Common.DSP;

namespace PDT.Plugins.Shure.DSP
{
    public class ShureDspDevice : DspBase, IHasDspPresets, ICommunicationMonitor, IDeviceInfoProvider, IOnline, IHasFeedback, IBridgeAdvanced
    {
        private readonly ShureDspProps _props;
        private readonly IBasicCommunication _comms;
        private readonly CTimer _poll;
        private readonly IDictionary<ShureP300ChannelEnum, ShureDspFader> _controlPoints;

        private bool _ipChanged;
        public BoolFeedback IpChangeFeedback;
        private DeviceConfig _dc;

        private DeviceInfo _currentDeviceInfo = new DeviceInfo
        {
            FirmwareVersion = "",
            HostName = "",
            IpAddress = "",
            SerialNumber = ""
        };

        public static int MapVolume(short level)
        {
            const float inputMin = 0;
            const float inputMax = 1400;

            const float outputMin = 0;
            const float outputMax = ushort.MaxValue;

            var normalized = (level - inputMin) / (inputMax - inputMin);
            var mappedValue = (int)(normalized * (outputMax - outputMin) + outputMin);

            return mappedValue;
        }

        public ShureDspDevice(string key, string name, ShureDspProps props, IBasicCommunication comms, DeviceConfig dc) : base(key, name)
        {
            _props = props;
            _comms = comms;
            _dc = dc;

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
                    _poll.Reset(10000, 10000);
                }
                else
                {
                    _poll.Stop();
                }
            };

            IpChangeFeedback = new BoolFeedback(() => _ipChanged);

            Feedbacks = new FeedbackCollection<PepperDash.Essentials.Core.Feedback>
            {
                IsOnline, IpChangeFeedback
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

            CrestronConsole.AddNewConsoleCommand(RecallPreset, Key + "PRESET", "Recalls a preset by string", ConsoleAccessLevelEnum.AccessAdministrator);
            CrestronConsole.AddNewConsoleCommand(TestVolume, Key + "VOLUME", "Recalls a preset by string", ConsoleAccessLevelEnum.AccessAdministrator);
        }

        public override void Initialize()
        {
            _comms.Connect();
            CommunicationMonitor.Start();
        }

        private void UpdateFeedbacks()
        {
            IsOnline.FireUpdate();
        }

        #region LinkToApi

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new ShureDspBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(1, this, "Linking to Bridge Type {0}", GetType().Name);

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            if(IsOnline != null) IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);          

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                UpdateFeedbacks();
            };

            JoinDataComplete setIpJoinData;
            if (joinMap.Joins.TryGetValue("SetIpAddress", out setIpJoinData))
            {
                trilist.SetStringSigAction(setIpJoinData.JoinNumber, SetIpAddress);
                Debug.Console(1, this, "Registered SetIpAddress to join {0}", setIpJoinData.JoinNumber);

            }

            JoinDataComplete ipSetFbJoinData;
            if (joinMap.Joins.TryGetValue("IpAddressSetFeedback", out ipSetFbJoinData))
            {
                IpChangeFeedback.OutputChange += (o, a) =>
                {
                    if (!a.BoolValue) return;
                    trilist.PulseBool(ipSetFbJoinData.JoinNumber, 1000);
                    _ipChanged = false;
                    IpChangeFeedback.FireUpdate();
                };
            }
        }
        #endregion

        protected void CustomSetConfig(DeviceConfig config)
        {
            ConfigWriter.UpdateDeviceConfig(_dc);

            Debug.Console(0, this, "IP address changed to {0}. Restart Essentials to take effect.", _dc.Properties["control"]["tcpSshProperties"]["address"].ToString());

            _ipChanged = true;
            IpChangeFeedback.FireUpdate();
        }

        private void SetIpAddress(string hostname)
        {
            try
            {
                Debug.Console(0, this, "SetIpAddress called with hostname: '{0}'", hostname);

                var currentHostname = _dc.Properties["control"]["tcpSshProperties"]["address"].ToString();

                Debug.Console(0, this, "Current hostname is: '{0}'", currentHostname);

                if (hostname.Length <= 2)
                {
                    Debug.Console(0, this, "Hostname is too short; ignoring.");

                    return;
                }
                if (currentHostname == hostname)
                {
                    Debug.Console(0, this, "Hostname is the same as current; no change needed.");

                    return;
                }
                //UpdateHostname(hostname);

                _dc.Properties["control"]["tcpSshProperties"]["address"] = hostname;
                Debug.Console(0, this, "New hostname set to: '{0}'", hostname);

                CustomSetConfig(_dc);
            }
            catch (Exception e)
            {

                Debug.Console(2, this, "Error SetIpAddress: '{0}'", e);
            }
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
                Debug.Console(0, this, "Caught an exception parsing:{0}{1}", response, ex);
            }
        }

        private void ParseMuteResponse(string response)
        {
            // < REP xx AUDIO_MUTE ON >
            var parts = response.Split(new[] {' '});
            var channelId = Convert.ToInt32(parts[2]);
            var channelEnum = (ShureP300ChannelEnum)channelId;

            ShureDspFader fader;
            if (_controlPoints.TryGetValue(channelEnum, out fader))
            {
                fader.VolumeIsMuted = response.Contains("ON");
                Debug.Console(2, this, "Parse mute response channel enum:{0} value:{1}", channelEnum, fader.VolumeIsMuted);
            }
            else
            {
                Debug.Console(2, this, "Could not find fader with enum:{0}", channelEnum);
            }
        }

        private void ParseLevelResponse(string response)
        {
            // < REP xx AUDIO_GAIN_HI_RES yyyy >
            var parts = response.Split(new[] { ' ' });
            var channelId = Convert.ToInt32(parts[2]);
            var channelEnum = (ShureP300ChannelEnum)channelId;

            ShureDspFader fader;
            if (_controlPoints.TryGetValue(channelEnum, out fader))
            {
                var volume = Convert.ToInt16(parts[4]);
                Debug.Console(2, this, "Parse level response channel:{0} volume:{1}", channelId, volume);
                var scaledVolume = MapVolume(volume);
                Debug.Console(2, this, "Parse level response channel:{0} scaled volume:{1}", channelId, scaledVolume);
                fader.CurrentLevel = scaledVolume;
            }
            else
            {
                Debug.Console(2, this, "Could not find fader with enum:{0}", channelEnum);
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

        public void RecallPreset(string presetName)
        {
            var preset = Presets.FirstOrDefault(p => p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase));
            if (preset == null)
            {
                Debug.Console(0, this, "Couldn't find preset name:{0}", presetName);
            }

            RecallPreset(preset);
        }

        public void TestVolume(string volume)
        {
            try
            {
                var testLevel = Convert.ToUInt16(volume);
                var fader = _controlPoints[ShureP300ChannelEnum.AnalogOutput1];
                fader.SetVolume(testLevel);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "Caught an exception testing volume:{0}", ex);
            }
        }

        public List<IDspPreset> Presets { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public BoolFeedback IsOnline
        {
             get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        public FeedbackCollection<PepperDash.Essentials.Core.Feedback> Feedbacks { get; private set; }

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

