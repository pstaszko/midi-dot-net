namespace Midi.Messages
{
    /// <summary>
    ///     Base class for all MIDI messages.
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        /// <param name="time">The timestamp for this message.</param>
        protected Message(float time)
        {
            Time = time;
        }

        /// <summary>
        ///     Milliseconds since the music started.
        /// </summary>
        public float Time { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public abstract void SendNow();

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public abstract Message MakeTimeShiftedCopy(float delta);
    }
}