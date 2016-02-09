using System;
using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Pitch Bend message.
    /// </summary>
    public class PitchBendMessage : ChannelMessage
    {
        /// <summary>
        ///     Constructs a Pitch Bend message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="value">Pitch bend value, 0..16383, 8192 is centered.</param>
        /// <param name="time">The timestamp for this message.</param>
        public PitchBendMessage(DeviceBase device, Channel channel, int value, float time)
            : base(device, channel, time)
        {
            if (value < 0 || value > 16383)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            Value = value;
        }

        /// <summary>
        ///     Pitch bend value, 0..16383, 8192 is centered.
        /// </summary>
        public int Value { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendPitchBend(Channel, Value);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new PitchBendMessage(Device, Channel, Value, Time + delta);
        }
    }
}