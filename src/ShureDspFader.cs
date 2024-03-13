using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PDT.Plugins.Shure.DSP
{
    public class ShureDspFader : IKeyed, IBasicVolumeWithFeedback
    {
        private readonly ShureDspDevice _parent;
        private readonly ShureP300ChannelEnum _channel;

        private bool _volumeIsMuted;
        private int _currentLevel;

        public bool VolumeIsMuted
        {
            get => _volumeIsMuted;
            set
            {
                _volumeIsMuted = value;
                MuteFeedback.FireUpdate();
            }
        }

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                VolumeLevelFeedback.FireUpdate();
            }
        }

        private volatile bool _volumeIsRampingUp;
        private volatile bool _volumeIsRampingDown;

        public ShureDspFader(ShureDspDevice parent, ShureP300ChannelEnum channel)
        {
            _parent = parent;
            _channel = channel;

            Key = parent.Key + "-" + channel;

            VolumeLevelFeedback = new IntFeedback(Key + "-Level", () => CurrentLevel);
            MuteFeedback = new BoolFeedback(Key + "-Mute", () => VolumeIsMuted);
        }

        public void VolumeUp(bool pressRelease)
        {
            /* < SET xx AUDIO_GAIN_HI_RES INC nn >
             * Where nn is the amount in one-tenth of
                a dB to increase the gain. nn can be
                single digit ( n ), double digit ( nn ),
                triple digit ( nnn ).
             */

            if (_volumeIsRampingUp && pressRelease)
            {
                return;
            }

            _volumeIsRampingUp = pressRelease;

            if (!pressRelease) return;


            CrestronInvoke.BeginInvoke(_ =>
            {
                // < SET xx AUDIO_GAIN_HI_RES INC nn >
                const string commandTemplate = "< SET {0} AUDIO_GAIN_HI_RES INC 100 >";
                var command = string.Format(commandTemplate, _channel);

                using (var wh = new CEvent(true, false))
                {
                    while (_volumeIsRampingUp)
                    {
                        _parent.SendText(command);
                        wh.Wait(25);
                    }
                }
            });
        }

        public void VolumeDown(bool pressRelease)
        {
            /* < SET xx AUDIO_GAIN_HI_RES DEC nn >
             * Where nn is the amount in one-tenth of
                a dB to increase the gain. nn can be
                single digit ( n ), double digit ( nn ),
                triple digit ( nnn ).
             */

            if (_volumeIsRampingDown && pressRelease)
            {
                return;
            }

            _volumeIsRampingDown = pressRelease;

            if (!pressRelease) return;

            CrestronInvoke.BeginInvoke(_ =>
            {
                // < SET xx AUDIO_GAIN_HI_RES INC nn >
                const string commandTemplate = "< SET {0} AUDIO_GAIN_HI_RES DEC 100 >";
                var command = string.Format(commandTemplate, _channel);

                using (var wh = new CEvent(true, false))
                {
                    while (_volumeIsRampingDown)
                    {
                        _parent.SendText(command);
                        wh.Wait(25);
                    }
                }
            });
        }

        public void MuteToggle()
        {
            // < SET xx AUDIO_MUTE TOGGLE >
            const string commandTemplate = "< SET {0} AUDIO_MUTE TOGGLE >";
            var command = string.Format(commandTemplate, (int) _channel);
            _parent.SendText(command);
        }

        public void MuteOn()
        {
            // < SET xx AUDIO_MUTE ON >
            const string commandTemplate = "< SET {0} AUDIO_MUTE ON >";
            var command = string.Format(commandTemplate, (int)_channel);
            _parent.SendText(command);
        }

        public void MuteOff()
        {
            // < SET xx AUDIO_MUTE OFF >
            const string commandTemplate = "< SET {0} AUDIO_MUTE OFF >";
            var command = string.Format(commandTemplate, (int)_channel);
            _parent.SendText(command);
        }

        public void SetVolume(ushort level)
        {
            // < SET xx AUDIO_GAIN_HI_RES yyyy >
            const string commandTemplate = "< SET {0} AUDIO_GAIN_HI_RES {1} >";

            var mappedLevel = CrestronEnvironment.ScaleWithLimits(level, ushort.MaxValue, 0, 1400, 0);
            var command = string.Format(commandTemplate, (int)_channel, mappedLevel);
            _parent.SendText(command);
        }

        public BoolFeedback MuteFeedback { get; }
        public IntFeedback VolumeLevelFeedback { get; }
        public string Key { get; }
    }
}