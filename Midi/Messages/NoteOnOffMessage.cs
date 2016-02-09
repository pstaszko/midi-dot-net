using Midi.Devices;
using Midi.Enums;

namespace Midi.Messages
{
    /// <summary>
    ///     A Note On message which schedules its own Note Off message when played.
    /// </summary>
    public class NoteOnOffMessage : NoteMessage
    {
        /// <summary>
        ///     Constructs a Note On/Off message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="pitch">The pitch for this note message.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        /// <param name="clock">The clock that should schedule the off message.</param>
        /// <param name="duration">Time delay between on message and off messasge.</param>
        public NoteOnOffMessage(DeviceBase device, Channel channel, Pitch pitch,
            int velocity, float time, Clock clock, float duration)
            : base(device, channel, pitch, velocity, time)
        {
            Clock = clock;
            Duration = duration;
        }

        /// <summary>
        ///     The clock used to schedule the follow-up message.
        /// </summary>
        public Clock Clock { get; }

        /// <summary>
        ///     Time delay between the Note On and the Note Off.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendNoteOn(Channel, Pitch, Velocity);
            Clock.Schedule(new NoteOffMessage(Device, Channel, Pitch, Velocity, Time + Duration));
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NoteOnOffMessage(Device, Channel, Pitch, Velocity, Time + delta,
                Clock, Duration);
        }
    }
}