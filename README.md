![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# Essentials Plugin Template (c) 2023

## License

Provided under MIT license

## Device Configuration

```json
{
    "key": "dsp-1",
    "name": "Shure P300 DSP",
    "type": "shurep300",
    "group": "plugin",
    "properties": {
        "control": {
            "method": "tcpIp",
            "endOfLineString": "\n",
            "deviceReadyResponsePattern": "",
            "tcpSshProperties": {
                "address": "",
                "port": 2202,
                "username": "",
                "password": "",
                "autoReconnect": true,
                "autoReconnectIntervalMs": 5000
            }
        }
    }
}
```

## Fader Keys

The following fader keys are automatically built for the fixed architecture DSP.

>[!NOTE]
>Faders will be prefixed with the plugin device `{key}-`

```c#
dsp-1-AecReference
dsp-1-AnalogOutput1
dsp-1-AnalogOutput2
dsp-1-AutomixerOutput
dsp-1-DanteInput09
dsp-1-DanteInput10
dsp-1-DanteMicInput01
dsp-1-DanteMicInput02
dsp-1-DanteMicInput03
dsp-1-DanteMicInput04
dsp-1-DanteMicInput05
dsp-1-DanteMicInput06
dsp-1-DanteMicInput07
dsp-1-DanteMicInput08
dsp-1-DanteOutput1
dsp-1-DanteOutput2
dsp-1-MobileInput
dsp-1-MobileOutput
dsp-1-UsbInput
dsp-1-UsbOutput

```

## Join Map

### Digitals
| Join Number | Join Span | Description                   | Type    | Capabilities |
| ----------- | --------- | ----------------------------- | ------- | ------------ |
| 1           | 1         | Is Online                     | Digital | ToSIMPL      |

### Serials
| Join Number | Join Span | Description                   | Type   | Capabilities |
| ----------- | --------- | ----------------------------- | ------ | ------------ |
| 1           | 1         | Device Name                   | Serial | ToSIMPL      |
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.16.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Is Online |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Name |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IHasDspPresets
- ICommunicationMonitor
- IDeviceInfoProvider
- IOnline
- IHasFeedback
- IBridgeAdvanced
- IDspPreset
- IKeyed
- IBasicVolumeWithFeedback
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- DspBase
- JoinMapBaseAdvanced
- DspControlPoint
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void ParseResponse(string response)
- public void SendText(string text)
- public void RecallPreset(IDspPreset preset)
- public void RecallPreset(string presetName)
- public void TestVolume(string volume)
- public void UpdateDeviceInfo()
- public void VolumeUp(bool pressRelease)
- public void VolumeDown(bool pressRelease)
- public void MuteToggle()
- public void MuteOn()
- public void MuteOff()
- public void SetVolume(ushort level)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IpChangeFeedback
- IsOnline
- MuteFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- VolumeLevelFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
