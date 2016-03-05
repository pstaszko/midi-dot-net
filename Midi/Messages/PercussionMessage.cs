using System;
using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Percussion message.
    /// </summary>
    /// <remarks>
    ///     A percussion message is simply shorthand for sending a Note On message to Channel10 with a
    ///     percussion-specific note.  This message can be sent to an OutputDevice but will be received
    ///     by an InputDevice as a NoteOn message.
    /// </remarks>
    public class PercussionMessage : DeviceMessage
    {
        /// <summary>
        ///     Constructs a Percussion message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="percussion">Percussion.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public PercussionMessage(IDeviceBase device, Percussion percussion, int velocity,
            float time)
            : base(device, time)
        {
            percussion.Validate();
            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(velocity));
            }
            Percussion = percussion;
            Velocity = velocity;
        }

        /// <summary>
        ///     Percussion.
        /// </summary>
        public Percussion Percussion { get; }

        /// <summary>
        ///     Velocity, 0..127.
        /// </summary>
        public int Velocity { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            (Device as IOutputDevice)?.SendNoteOn(Channel.Channel10, (Pitch) Percussion, Velocity);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new PercussionMessage(Device, Percussion, Velocity, Time + delta);
        }
    }
}