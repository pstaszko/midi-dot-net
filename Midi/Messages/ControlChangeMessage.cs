using System;
using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Control change message.
    /// </summary>
    public class ControlChangeMessage : ChannelMessage
    {
        /// <summary>
        ///     Construts a Control Change message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="control">Control, 0..119</param>
        /// <param name="value">Value, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public ControlChangeMessage(DeviceBase device, Channel channel, Control control, int value,
            float time)
            : base(device, channel, time)
        {
            control.Validate();
            if (value < 0 || value > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(control));
            }
            Control = control;
            Value = value;
        }

        /// <summary>
        ///     The control for this message.
        /// </summary>
        public Control Control { get; }

        /// <summary>
        ///     Value, 0..127.
        /// </summary>
        public int Value { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendControlChange(Channel, Control, Value);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new ControlChangeMessage(Device, Channel, Control, Value, Time + delta);
        }

        public override string ToString()
        {
            return $"Control: {Control}, Value: {Value}";
        }
    }
}