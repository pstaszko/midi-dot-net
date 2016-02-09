using System;
using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     Base class for messages relevant to a specific note.
    /// </summary>
    public abstract class NoteMessage : ChannelMessage
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        protected NoteMessage(DeviceBase device, Channel channel, Pitch pitch, int velocity,
            float time)
            : base(device, channel, time)
        {
            if (!pitch.IsInMidiRange())
            {
                throw new ArgumentOutOfRangeException(nameof(pitch));
            }
            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(velocity));
            }
            Pitch = pitch;
            Velocity = velocity;
        }

        /// <summary>The pitch for this note message.</summary>
        public Pitch Pitch { get; }

        /// <summary>
        ///     Velocity, 0..127.
        /// </summary>
        public int Velocity { get; }
    }
}