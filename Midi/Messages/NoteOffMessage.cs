using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Note Off message.
    /// </summary>
    public class NoteOffMessage : NoteMessage
    {
        /// <summary>
        ///     Constructs a Note Off message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="pitch">The pitch for this note message.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public NoteOffMessage(IDeviceBase device, Channel channel, Pitch pitch, int velocity,
            float time)
            : base(device, channel, pitch, velocity, time)
        {
        }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            (Device as IOutputDevice)?.SendNoteOff(Channel, Pitch, Velocity);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NoteOffMessage(Device, Channel, Pitch, Velocity, Time + delta);
        }
    }
}