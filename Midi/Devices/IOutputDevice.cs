using System;
using Midi.Enums;
using Midi.Messages;

namespace Midi.Devices
{
    /// <summary>
    ///     A MIDI output device.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each instance of this class describes a MIDI output device installed on the system.
    ///         You cannot create your own instances, but instead must go through the
    ///         <see cref="DeviceManager.OutputDevices" /> property to find which devices are available.  You may wish
    ///         to examine the <see cref="DeviceBase.Name" /> property of each one and present the user with
    ///         a choice of which device to use.
    ///     </para>
    ///     <para>
    ///         Open an output device with <see cref="Open" /> and close it with <see cref="Close" />.
    ///         While it is open, you may send MIDI messages with functions such as
    ///         <see cref="SendNoteOn" />, <see cref="SendNoteOff" /> and <see cref="SendProgramChange" />.
    ///         All notes may be silenced on the device by calling <see cref="SilenceAllNotes" />.
    ///     </para>
    ///     <para>
    ///         Note that the above methods send their messages immediately.  If you wish to arrange
    ///         for a message to be sent at a specific future time, you'll need to instantiate some subclass
    ///         of <see cref="Message" /> (eg <see cref="NoteOnMessage" />) and then pass it to
    ///         <see cref="Clock.Schedule(Message)">Clock.Schedule</see>.
    ///     </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true" />
    /// <seealso cref="Clock" />
    /// <seealso cref="InputDevice" />
    public interface IOutputDevice : IDeviceBase
    {
        int Id { get; set; }
        /// <summary>
        ///     True if this device is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        ///     Opens this output device.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device is already open.</exception>
        /// <exception cref="DeviceException">The device cannot be opened.</exception>
        void Open();

        /// <summary>
        ///     Closes this output device.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The device cannot be closed.</exception>
        void Close();

        /// <summary>
        ///     Silences all notes on this output device.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SilenceAllNotes();

        /// <summary>
        ///     Sends a Note On message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="pitch">The pitch.</param>
        /// <param name="velocity">The velocity 0..127.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     channel, pitch, or velocity is
        ///     out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendNoteOn(Channel channel, Pitch pitch, int velocity);

        /// <summary>
        ///     Sends a Note Off message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="pitch">The pitch.</param>
        /// <param name="velocity">The velocity 0..127.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     channel, note, or velocity is
        ///     out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendNoteOff(Channel channel, Pitch pitch, int velocity);

        /// <summary>
        ///     Sends a Note On message to Channel10 of this MIDI output device.
        /// </summary>
        /// <param name="percussion">The percussion.</param>
        /// <param name="velocity">The velocity 0..127.</param>
        /// <remarks>
        ///     This is simply shorthand for a Note On message on Channel10 with a
        ///     percussion-specific note, so there is no corresponding message to receive from an input
        ///     device.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     percussion or velocity is out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendPercussion(Percussion percussion, int velocity);

        /// <summary>
        ///     Sends a Control Change message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="control">The control.</param>
        /// <param name="value">The new value 0..127.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     channel, control, or value is
        ///     out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendControlChange(Channel channel, Control control, int value);

        /// <summary>
        ///     Sends a Pitch Bend message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="value">The pitch bend value, 0..16383, 8192 is centered.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     channel or value is out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendPitchBend(Channel channel, int value);

        /// <summary>
        ///     Sends a Program Change message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="instrument">The instrument.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     channel or instrument is out-of-range.
        /// </exception>
        /// <exception cref="InvalidOperationException">The device is not open.</exception>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        /// <remarks>
        ///     A Program Change message is used to switch among instrument settings, generally
        ///     instrument voices.  An instrument conforming to General Midi 1 will have the
        ///     instruments described in the <see cref="Instrument" /> enum; other instruments
        ///     may have different instrument sets.
        /// </remarks>
        void SendProgramChange(Channel channel, Instrument instrument);

        /// <summary>
        ///     Sends a System Exclusive (sysex) message to this MIDI output device.
        /// </summary>
        /// <param name="data">The message to send (as byte array)</param>
        /// <exception cref="DeviceException">The message cannot be sent.</exception>
        void SendSysEx(byte[] data);

        /// <summary>
        ///  Sends a Non-Registered Parameter Number (NRPN) message to this MIDI output device.
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="parameter">Parameter number, 0..16383</param>
        /// <param name="value">Value, 0..16383</param>
        void SendNrpn(Channel channel, int parameter, int value);
    }
}