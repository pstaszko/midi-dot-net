using Midi.Devices;
using Midi.Enums;
using System;

namespace Midi.Messages
{
    public class NrpnMessage : ChannelMessage
    {
        /// <summary>
        ///     Constructs a Non-Registered Parameter Number message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion</param>
        /// <param name="parameter">Parameter number, 0..16383</param>
        /// <param name="value">Value, 0..16383</param>
        /// <param name="time">The timestamp for this message</param>
        public NrpnMessage(IDeviceBase device, Channel channel, int parameter, int value, float time) : base(device, channel, time)
        {
            if (parameter < 0 || parameter > 16383)
                throw new ArgumentOutOfRangeException(nameof(parameter));

            if (value < 0 || value > 16383)
                throw new ArgumentOutOfRangeException(nameof(value));

            Parameter = parameter;
            Value = value;
        }

        /// <summary>
        ///     Parameter number, 0..16383
        /// </summary>
        public int Parameter { get; }

        /// <summary>
        ///     Value, 0..16383
        /// </summary>
        public int Value { get; }

        public override void SendNow()
        {
            (Device as IOutputDevice)?.SendNrpn(Channel, Parameter, Value);
        }

        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NrpnMessage(Device, Channel, Parameter, Value, Time + delta);
        }

        public override string ToString()
        {
            return $"Parameter: {Parameter}, Value: {Value}";
        }
    }
}