using PepperDash.Essentials.Core;

namespace PDT.Plugins.Shure.DSP
{
    public class ShureDspBridgeJoinMap : JoinMapBaseAdvanced
    {
		#region Digital

		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog		

		#endregion


		#region Serial

		[JoinName("Name")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});		

		#endregion

        private void SetIpChangeJoin(uint joinStart)
        {
            var ipSetJoinData = new JoinData
            {
                JoinNumber = joinStart + 98,
                JoinSpan = 1
            };

            var ipSetJoinMetaData = new JoinMetadata
            {
                Description = "Set device IP Address",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            };

            var ipSetJoinDataComplete = new JoinDataComplete(ipSetJoinData, ipSetJoinMetaData);
            Joins.Add("SetIpAddress", ipSetJoinDataComplete);

            var setFbJoinData = new JoinData
            {
                JoinNumber = joinStart + 98,
                JoinSpan = 1
            };

            var setFbJoinMetaData = new JoinMetadata
            {
                Description = "IP Address Change Feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            };

            var setFbJoinDataComplete = new JoinDataComplete(setFbJoinData, setFbJoinMetaData);
            Joins.Add("IpAddressSetFeedback", setFbJoinDataComplete);
        }

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public ShureDspBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(ShureDspBridgeJoinMap))
		{
            SetIpChangeJoin(joinStart);
        }
	}
}