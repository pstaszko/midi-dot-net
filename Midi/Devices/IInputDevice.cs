using Midi.Messages;
using System;

namespace Midi.Devices
{
    /// <summary>
    ///     A MIDI input device.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each instance of this class describes a MIDI input device installed on the system.
    ///         You cannot create your own instances, but instead must go through the
    ///         <see cref="DeviceManager.InputDevices" /> property to find which devices are available.  You may wish
    ///         to examine the <see cref="DeviceBase.Name" /> property of each one and present the user with
    ///         a choice of which device(s) to use.
    ///     </para>
    ///     <para>
    ///         Open an input device with <see cref="Open" /> and close it with <see cref="Close" />.
    ///         While it is open, you may arrange to start receiving messages with
    ///         <see cref="StartReceiving(Clock,bool)" /> and then stop receiving them with <see cref="StopReceiving" />.
    ///         An input device can only receive messages when it is both open and started.
    ///     </para>
    ///     <para>
    ///         Incoming messages are routed to the corresponding events, such as <see cref="NoteOn" />
    ///         and <see cref="ProgramChange" />.  The event handlers are invoked on a background thread
    ///         which is started in <see cref="StartReceiving(Clock,bool)" /> and stopped in <see cref="StopReceiving" />.
    ///     </para>
    ///     <para>
    ///         As each message is received, it is assigned a timestamp in one of two ways.  If
    ///         <see cref="StartReceiving(Clock,bool)" /> is called with a <see cref="Clock" />, then each message is
    ///         assigned a time by querying the clock's <see cref="Clock.Time" /> property.  If
    ///         <see cref="StartReceiving(Clock,bool)" /> is called with null, then each message is assigned a time
    ///         based on the number of seconds since <see cref="StartReceiving(Clock,bool)" /> was called.
    ///     </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true" />
    /// <seealso cref="Clock" />
    /// <seealso cref="InputDevice" />
    public interface IInputDevice : IDeviceBase
    {
        int Id { get; set; }
        /// <summary>
        ///     True if this device has been successfully opened.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        ///     True if this device is receiving messages.
        /// </summary>
        bool IsReceiving { get; }

        /// <summary>
        ///     Event called when an input device receives a Note On message.
        /// </summary>
        event NoteOnHandler NoteOn;

        /// <summary>
        ///     Event called when an input device receives a Note Off message.
        /// </summary>
        event NoteOffHandler NoteOff;

        /// <summary>
        ///     Event called when an input device receives a Control Change message.
        /// </summary>
        event ControlChangeHandler ControlChange;

        /// <summary>
        ///     Event called when an input device receives a Program Change message.
        /// </summary>
        event ProgramChangeHandler ProgramChange;

        /// <summary>
        ///     Event called when an input device receives a Pitch Bend message.
        /// </summary>
        event PitchBendHandler PitchBend;

        /// <summary>
        ///     Event called when an input device receives a SysEx message.
        /// </summary>
        event SysExHandler SysEx;

        /// <summary>
        ///     Event called when an input device receives a NRPN message.
        /// </summary>
        event NrpnHandler Nrpn;

        /// <summary>
        ///     Removes all event handlers from the input events on this device.
        /// </summary>
        void RemoveAllEventHandlers();

        /// <summary>
        ///     Opens this input device.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device is already open.</exception>
        /// <exception cref="DeviceException">The device cannot be opened.</exception>
        /// <remarks>
        ///     Note that Open() establishes a connection to the device, but no messages will
        ///     be received until <see cref="InputDevice.StartReceiving(Midi.Devices.Clock)" /> is called.
        /// </remarks>
        void Open();

        /// <summary>
        ///     Closes this input device.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The device is not open or is still
        ///     receiving.
        /// </exception>
        /// <exception cref="DeviceException">The device cannot be closed.</exception>
        void Close();

        /// <summary>
        ///     <see cref="InputDevice.StartReceiving(Midi.Devices.Clock,bool)" />
        /// </summary>
        /// <param name="clock">
        ///     If non-null, the clock's <see cref="Clock.Time" /> property will
        ///     be used to assign a timestamp to each incoming message.  If null, timestamps will be in
        ///     seconds since StartReceiving() was called.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The device is not open or is already
        ///     receiving.
        /// </exception>
        /// <exception cref="DeviceException">The device cannot start receiving.</exception>
        void StartReceiving(Clock clock);

        /// <summary>
        ///     Starts this input device receiving messages.
        /// </summary>
        /// <param name="clock">
        ///     If non-null, the clock's <see cref="Clock.Time" /> property will
        ///     be used to assign a timestamp to each incoming message.  If null, timestamps will be in
        ///     seconds since StartReceiving() was called.
        /// </param>
        /// <param name="handleSysEx">
        ///     Boolean, when TRUE buffers will be created to enable handling
        ///     of incoming MIDI Long Messages (SysEx). When FALSE, all long messages are ignored.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The device is not open or is already
        ///     receiving.
        /// </exception>
        /// <exception cref="DeviceException">The device cannot start receiving.</exception>
        /// <remarks>
        ///     <para>
        ///         This method launches a background thread to listen for input events, and as events
        ///         are received, the event handlers are invoked on that background thread.  Event handlers
        ///         should be written to work from a background thread.  (For example, if they want to
        ///         update the GUI, they may need to BeginInvoke to arrange for GUI updates to happen on
        ///         the correct thread.)
        ///     </para>
        ///     <para>
        ///         The background thread which is created by this method is joined (shut down) in
        ///         <see cref="InputDevice.StopReceiving" />.
        ///     </para>
        /// </remarks>
        void StartReceiving(Clock clock, bool handleSysEx);

        /// <summary>
        ///     Stops this input device from receiving messages.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method waits for all in-progress input event handlers to finish, and then
        ///         joins (shuts down) the background thread that was created in
        ///         <see cref="InputDevice.StartReceiving(Midi.Devices.Clock)" />.  Thus, when this function returns you can be sure that no
        ///         more event handlers will be invoked.
        ///     </para>
        ///     <para>
        ///         It is illegal to call this method from an input event handler (ie, from the
        ///         background thread), and doing so throws an exception. If an event handler really needs
        ///         to call this method, consider using BeginInvoke to schedule it on another thread.
        ///     </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The device is not open; is not receiving;
        ///     or called from within an event handler (ie, from the background thread).
        /// </exception>
        /// <exception cref="DeviceException">The device cannot start receiving.</exception>
        void StopReceiving();
    }

    public delegate void ControlChangeHandler(ControlChangeMessage msg);
    public delegate void NoteOffHandler(NoteOffMessage msg);
    public delegate void NoteOnHandler(NoteOnMessage msg);
    public delegate void PitchBendHandler(PitchBendMessage msg);
    public delegate void ProgramChangeHandler(ProgramChangeMessage msg);
    public delegate void SysExHandler(SysExMessage msg);
    public delegate void NrpnHandler(NrpnMessage msg);
}