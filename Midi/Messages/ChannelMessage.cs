using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Base class for messages relevant to a specific device channel.
    /// </summary>
    public abstract class ChannelMessage : DeviceMessage
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        protected ChannelMessage(DeviceBase device, Channel channel, float time)
            : base(device, time)
        {
            channel.Validate();
            Channel = channel;
        }

        /// <summary>
        ///     Channel.
        /// </summary>
        public Channel Channel { get; }
    }
}