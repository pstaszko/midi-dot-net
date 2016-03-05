using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Program Change message.
    /// </summary>
    public class ProgramChangeMessage : ChannelMessage
    {
        /// <summary>
        ///     Constructs a Program Change message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel.</param>
        /// <param name="instrument">Instrument.</param>
        /// <param name="time">The timestamp for this message.</param>
        public ProgramChangeMessage(IDeviceBase device, Channel channel, Instrument instrument,
            float time)
            : base(device, channel, time)
        {
            instrument.Validate();
            Instrument = instrument;
        }

        /// <summary>
        ///     Instrument.
        /// </summary>
        public Instrument Instrument { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            (Device as IOutputDevice)?.SendProgramChange(Channel, Instrument);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new ProgramChangeMessage(Device, Channel, Instrument, Time + delta);
        }
    }
}