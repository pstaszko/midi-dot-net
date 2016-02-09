// Copyright (c) 2009, Tom Lokovic
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using Midi.Messages;
using Midi.Win32;

namespace Midi
{
    /// <summary>
    ///     A MIDI input device.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each instance of this class describes a MIDI input device installed on the system.
    ///         You cannot create your own instances, but instead must go through the
    ///         <see cref="InstalledDevices" /> property to find which devices are available.  You may wish
    ///         to examine the <see cref="DeviceBase.Name" /> property of each one and present the user with
    ///         a choice of which device(s) to use.
    ///     </para>
    ///     <para>
    ///         Open an input device with <see cref="Open" /> and close it with <see cref="Close" />.
    ///         While it is open, you may arrange to start receiving messages with
    ///         <see cref="StartReceiving(Midi.Clock,bool)" /> and then stop receiving them with <see cref="StopReceiving" />.
    ///         An input device can only receive messages when it is both open and started.
    ///     </para>
    ///     <para>
    ///         Incoming messages are routed to the corresponding events, such as <see cref="NoteOn" />
    ///         and <see cref="ProgramChange" />.  The event handlers are invoked on a background thread
    ///         which is started in <see cref="StartReceiving(Midi.Clock,bool)" /> and stopped in <see cref="StopReceiving" />.
    ///     </para>
    ///     <para>
    ///         As each message is received, it is assigned a timestamp in one of two ways.  If
    ///         <see cref="StartReceiving(Midi.Clock,bool)" /> is called with a <see cref="Clock" />, then each message is
    ///         assigned a time by querying the clock's <see cref="Clock.Time" /> property.  If
    ///         <see cref="StartReceiving(Midi.Clock,bool)" /> is called with null, then each message is assigned a time
    ///         based on the number of seconds since <see cref="StartReceiving(Midi.Clock,bool)" /> was called.
    ///     </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true" />
    /// <seealso cref="Clock" />
    /// <seealso cref="InputDevice" />
    public class InputDevice : DeviceBase
    {
        /// <summary>
        ///     Delegate called when an input device receives a Control Change message.
        /// </summary>
        public delegate void ControlChangeHandler(ControlChangeMessage msg);

        /// <summary>
        ///     Delegate called when an input device receives a Note Off message.
        /// </summary>
        public delegate void NoteOffHandler(NoteOffMessage msg);

        /// <summary>
        ///     Delegate called when an input device receives a Note On message.
        /// </summary>
        public delegate void NoteOnHandler(NoteOnMessage msg);

        /// <summary>
        ///     Delegate called when an input device receives a Pitch Bend message.
        /// </summary>
        public delegate void PitchBendHandler(PitchBendMessage msg);

        /// <summary>
        ///     Delegate called when an input device receives a Program Change message.
        /// </summary>
        public delegate void ProgramChangeHandler(ProgramChangeMessage msg);

        /// <summary>
        ///     Delegate called when an input device receives a SysEx message.
        /// </summary>
        public delegate void SysExHandler(SysExMessage msg);

        // Access to the global state is guarded by lock(staticLock).
        private static readonly object StaticLock = new object();
        private static InputDevice[] _installedDevices;

        /// <summary>
        ///     Thread-local, set to true when called by an input handler, false in all other threads.
        /// </summary>
        [ThreadStatic] private static bool _isInsideInputHandler;

        // These fields initialized in the constructor never change after construction,
        // so they don't need to be guarded by a lock.  We keep a reference to the
        // callback delegate because we pass it to unmanaged code (midiInOpen) and unmanaged code
        // cannot prevent the garbage collector from collecting the delegate.
        private readonly UIntPtr _deviceId;
        private readonly Win32API.MidiInProc _inputCallbackDelegate;

        //Holds a list of pointers to all the buffers created for handling Long Messages.
        private readonly List<IntPtr> _longMsgBuffers = new List<IntPtr>();
        // ReSharper disable once NotAccessedField.Local
        private MIDIINCAPS _caps;
        private Clock _clock;
        private HMIDIIN _handle;
        private bool _isClosing;

        // Access to the Open/Close state is guarded by lock(this).
        private bool _isOpen;
        private bool _isReceiving;

        /// <summary>
        ///     Private Constructor, only called by the getter for the InstalledDevices property.
        /// </summary>
        /// <param name="deviceId">Position of this device in the list of all devices.</param>
        /// <param name="caps">Win32 Struct with device metadata</param>
        private InputDevice(UIntPtr deviceId, MIDIINCAPS caps)
            : base(caps.szPname)
        {
            _deviceId = deviceId;
            _caps = caps;
            _inputCallbackDelegate = InputCallback;
            _isOpen = false;
            _clock = null;
        }

        /// <summary>
        ///     List of input devices installed on this system.
        /// </summary>
        public static ReadOnlyCollection<InputDevice> InstalledDevices
        {
            get
            {
                lock (StaticLock)
                {
                    if (_installedDevices == null)
                    {
                        _installedDevices = MakeDeviceList();
                    }
                    return new ReadOnlyCollection<InputDevice>(_installedDevices);
                }
            }
        }

        /// <summary>
        ///     True if this device has been successfully opened.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                if (_isInsideInputHandler)
                {
                    return true;
                }
                lock (this)
                {
                    return _isOpen;
                }
            }
        }

        /// <summary>
        ///     True if this device is receiving messages.
        /// </summary>
        public bool IsReceiving
        {
            get
            {
                if (_isInsideInputHandler)
                {
                    return true;
                }
                lock (this)
                {
                    return _isReceiving;
                }
            }
        }

        /// <summary>
        ///     Event called when an input device receives a Note On message.
        /// </summary>
        public event NoteOnHandler NoteOn;

        /// <summary>
        ///     Event called when an input device receives a Note Off message.
        /// </summary>
        public event NoteOffHandler NoteOff;

        /// <summary>
        ///     Event called when an input device receives a Control Change message.
        /// </summary>
        public event ControlChangeHandler ControlChange;

        /// <summary>
        ///     Event called when an input device receives a Program Change message.
        /// </summary>
        public event ProgramChangeHandler ProgramChange;

        /// <summary>
        ///     Event called when an input device receives a Pitch Bend message.
        /// </summary>
        public event PitchBendHandler PitchBend;

        /// <summary>
        ///     Event called when an input device receives a SysEx message.
        /// </summary>
        public event SysExHandler SysEx;

        /// <summary>
        ///     Removes all event handlers from the input events on this device.
        /// </summary>
        public void RemoveAllEventHandlers()
        {
            NoteOn = null;
            NoteOff = null;
            ControlChange = null;
            ProgramChange = null;
            PitchBend = null;
            SysEx = null;
        }

        /// <summary>
        ///     Refresh the list of input devices
        /// </summary>
        public static void UpdateInstalledDevices()
        {
            lock (StaticLock)
            {
                _installedDevices = null;
            }
        }

        /// <summary>
        ///     Opens this input device.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device is already open.</exception>
        /// <exception cref="DeviceException">The device cannot be opened.</exception>
        /// <remarks>
        ///     Note that Open() establishes a connection to the device, but no messages will
        ///     be received until <see cref="StartReceiving(Midi.Clock)" /> is called.
        /// </remarks>
        public void Open()
        {
            if (_isInsideInputHandler)
            {
                throw new InvalidOperationException("Device is open.");
            }
            lock (this)
            {
                CheckNotOpen();
                CheckReturnCode(Win32API.midiInOpen(out _handle, _deviceId,
                    _inputCallbackDelegate, (UIntPtr) 0));
                _isOpen = true;
            }
        }

        /// <summary>
        ///     Closes this input device.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The device is not open or is still
        ///     receiving.
        /// </exception>
        /// <exception cref="DeviceException">The device cannot be closed.</exception>
        public void Close()
        {
            if (_isInsideInputHandler)
            {
                throw new InvalidOperationException("Device is receiving.");
            }
            lock (this)
            {
                CheckOpen();

                _isClosing = true;
                if (_longMsgBuffers.Count > 0)
                {
                    CheckReturnCode(Win32API.midiInReset(_handle));
                }
                //Destroy any Long Message buffers we created when opening this device.
                //foreach (IntPtr buffer in LongMsgBuffers)
                //{
                //    if (DestroyLongMsgBuffer(buffer))
                //    {
                //        LongMsgBuffers.Remove(buffer);
                //    }
                //}

                CheckReturnCode(Win32API.midiInClose(_handle));
                _isOpen = false;

                _isClosing = false;
            }
        }

        /// <summary>
        ///     <see cref="StartReceiving(Clock,bool)" />
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
        public void StartReceiving(Clock clock)
        {
            StartReceiving(clock, false);
        }

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
        ///         <see cref="StopReceiving" />.
        ///     </para>
        /// </remarks>
        public void StartReceiving(Clock clock, bool handleSysEx)
        {
            if (_isInsideInputHandler)
            {
                throw new InvalidOperationException("Device is receiving.");
            }
            lock (this)
            {
                CheckOpen();
                CheckNotReceiving();

                if (handleSysEx)
                {
                    _longMsgBuffers.Add(CreateLongMsgBuffer());
                }

                CheckReturnCode(Win32API.midiInStart(_handle));
                _isReceiving = true;
                _clock = clock;
            }
        }

        /// <summary>
        ///     Stops this input device from receiving messages.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method waits for all in-progress input event handlers to finish, and then
        ///         joins (shuts down) the background thread that was created in
        ///         <see cref="StartReceiving(Midi.Clock)" />.  Thus, when this function returns you can be sure that no
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
        public void StopReceiving()
        {
            if (_isInsideInputHandler)
            {
                throw new InvalidOperationException(
                    "Can't call StopReceiving() from inside an input handler.");
            }
            lock (this)
            {
                CheckReceiving();
                CheckReturnCode(Win32API.midiInStop(_handle));
                _clock = null;
                _isReceiving = false;
            }
        }

        /// <summary>
        ///     Makes sure rc is MidiWin32Wrapper.MMSYSERR_NOERROR.  If not, throws an exception with an
        ///     appropriate error message.
        /// </summary>
        /// <param name="rc"></param>
        private static void CheckReturnCode(MMRESULT rc)
        {
            if (rc != MMRESULT.MMSYSERR_NOERROR)
            {
                var errorMsg = new StringBuilder(128);
                rc = Win32API.midiInGetErrorText(rc, errorMsg);
                if (rc != MMRESULT.MMSYSERR_NOERROR)
                {
                    throw new DeviceException("no error details");
                }
                throw new DeviceException(errorMsg.ToString());
            }
        }

        /// <summary>
        ///     Throws a MidiDeviceException if this device is not open.
        /// </summary>
        private void CheckOpen()
        {
            if (!_isOpen)
            {
                throw new InvalidOperationException("Device is not open.");
            }
        }

        /// <summary>
        ///     Throws a MidiDeviceException if this device is open.
        /// </summary>
        private void CheckNotOpen()
        {
            if (_isOpen)
            {
                throw new InvalidOperationException("Device is open.");
            }
        }

        /// <summary>
        ///     Throws a MidiDeviceException if this device is not receiving.
        /// </summary>
        private void CheckReceiving()
        {
            if (!_isReceiving)
            {
                throw new DeviceException("device not receiving");
            }
        }

        /// <summary>
        ///     Throws a MidiDeviceException if this device is receiving.
        /// </summary>
        private void CheckNotReceiving()
        {
            if (_isReceiving)
            {
                throw new DeviceException("device receiving");
            }
        }

        /// <summary>
        ///     Private method for constructing the array of MidiInputDevices by calling the Win32 api.
        /// </summary>
        /// <returns></returns>
        private static InputDevice[] MakeDeviceList()
        {
            var inDevs = Win32API.midiInGetNumDevs();
            var result = new InputDevice[inDevs];
            for (uint deviceId = 0; deviceId < inDevs; deviceId++)
            {
                MIDIINCAPS caps;
                Win32API.midiInGetDevCaps((UIntPtr) deviceId, out caps);
                result[deviceId] = new InputDevice((UIntPtr) deviceId, caps);
            }
            return result;
        }

        /// <summary>
        ///     The input callback for midiOutOpen.
        /// </summary>
        private void InputCallback(HMIDIIN hMidiIn, MidiInMessage wMsg,
            UIntPtr dwInstance, UIntPtr dwParam1, UIntPtr dwParam2)
        {
            _isInsideInputHandler = true;
            try
            {
                if (wMsg == MidiInMessage.MIM_DATA)
                {
                    Channel channel;
                    Pitch pitch;
                    int velocity;
                    int value;
                    uint win32Timestamp;
                    if (ShortMsg.IsNoteOn(dwParam1, dwParam2))
                    {
                        if (NoteOn != null)
                        {
                            ShortMsg.DecodeNoteOn(dwParam1, dwParam2, out channel, out pitch,
                                out velocity, out win32Timestamp);
                            NoteOn(new NoteOnMessage(this, channel, pitch, velocity,
                                _clock?.Time ?? win32Timestamp/1000f));
                        }
                    }
                    else if (ShortMsg.IsNoteOff(dwParam1, dwParam2))
                    {
                        if (NoteOff != null)
                        {
                            ShortMsg.DecodeNoteOff(dwParam1, dwParam2, out channel, out pitch,
                                out velocity, out win32Timestamp);
                            NoteOff(new NoteOffMessage(this, channel, pitch, velocity,
                                _clock?.Time ?? win32Timestamp/1000f));
                        }
                    }
                    else if (ShortMsg.IsControlChange(dwParam1, dwParam2))
                    {
                        if (ControlChange != null)
                        {
                            Control control;
                            ShortMsg.DecodeControlChange(dwParam1, dwParam2, out channel,
                                out control, out value, out win32Timestamp);
                            ControlChange(new ControlChangeMessage(this, channel, control, value,
                                _clock?.Time ?? win32Timestamp/1000f));
                        }
                    }
                    else if (ShortMsg.IsProgramChange(dwParam1, dwParam2))
                    {
                        if (ProgramChange != null)
                        {
                            Instrument instrument;
                            ShortMsg.DecodeProgramChange(dwParam1, dwParam2, out channel,
                                out instrument, out win32Timestamp);
                            ProgramChange(new ProgramChangeMessage(this, channel, instrument,
                                _clock?.Time ?? win32Timestamp/1000f));
                        }
                    }
                    else if (ShortMsg.IsPitchBend(dwParam1, dwParam2))
                    {
                        if (PitchBend != null)
                        {
                            ShortMsg.DecodePitchBend(dwParam1, dwParam2, out channel,
                                out value, out win32Timestamp);
                            PitchBend(new PitchBendMessage(this, channel, value,
                                _clock?.Time ?? win32Timestamp/1000f));
                        }
                    }
                }
                else if (wMsg == MidiInMessage.MIM_LONGDATA)
                {
                    if (LongMsg.IsSysEx(dwParam1, dwParam2))
                    {
                        if (SysEx != null)
                        {
                            byte[] data;
                            uint win32Timestamp;
                            LongMsg.DecodeSysEx(dwParam1, dwParam2, out data, out win32Timestamp);
                            if (data.Length != 0)
                            {
                                SysEx(new SysExMessage(this, data, _clock?.Time ?? win32Timestamp/1000f));
                            }

                            if (_isClosing)
                            {
                                //buffers no longer needed
                                DestroyLongMsgBuffer(dwParam1);
                            }
                            else
                            {
                                //prepare the buffer for the next message
                                RecycleLongMsgBuffer(dwParam1);
                            }
                        }
                    }
                }
                // The rest of these are just for long message testing
                else if (wMsg == MidiInMessage.MIM_MOREDATA)
                {
                    InvokeSysEx(new SysExMessage(this, new byte[] {0x13}, 13));
                }
                else if (wMsg == MidiInMessage.MIM_OPEN)
                {
                    //SysEx(new SysExMessage(this, new byte[] { 0x01 }, 1));
                }
                else if (wMsg == MidiInMessage.MIM_CLOSE)
                {
                    //SysEx(new SysExMessage(this, new byte[] { 0x02 }, 2));
                }
                else if (wMsg == MidiInMessage.MIM_ERROR)
                {
                    InvokeSysEx(new SysExMessage(this, new byte[] {0x03}, 3));
                }
                else if (wMsg == MidiInMessage.MIM_LONGERROR)
                {
                    InvokeSysEx(new SysExMessage(this, new byte[] {0x04}, 4));
                }
                else
                {
                    InvokeSysEx(new SysExMessage(this, new byte[] {0x05}, 5));
                }
            }
            finally
            {
                _isInsideInputHandler = false;
            }
        }

        private IntPtr CreateLongMsgBuffer()
        {
            //add a buffer so we can receive SysEx messages
            IntPtr ptr;
            var size = (uint) Marshal.SizeOf(typeof (MIDIHDR));
            var header = new MIDIHDR
            {
                lpData = Marshal.AllocHGlobal(4096),
                dwBufferLength = 4096,
                dwFlags = 0
            };

            try
            {
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (MIDIHDR)));
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(header.lpData);
                throw;
            }

            try
            {
                Marshal.StructureToPtr(header, ptr, false);
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(header.lpData);
                Marshal.FreeHGlobal(ptr);
                throw;
            }

            CheckReturnCode(Win32API.midiInPrepareHeader(_handle, ptr, size));
            CheckReturnCode(Win32API.midiInAddBuffer(_handle, ptr, size));
            //CheckReturnCode(Win32API.midiInUnprepareHeader(handle, ptr, size));

            return ptr;
        }

        private void RecycleLongMsgBuffer(UIntPtr ptr)
        {
            var newPtr = ptr.ToIntPtr();
            var size = (uint) Marshal.SizeOf(typeof (MIDIHDR));
            CheckReturnCode(Win32API.midiInUnprepareHeader(_handle, newPtr, size));
            CheckReturnCode(Win32API.midiInPrepareHeader(_handle, newPtr, size));
            CheckReturnCode(Win32API.midiInAddBuffer(_handle, newPtr, size));
            //return unchecked((UIntPtr)(ulong)(long)newPtr);
        }

        /// <summary>
        ///     Releases the resources associated with the specified MidiHeader pointer.
        /// </summary>
        /// <param name="ptr">
        ///     The pointer to MIDIHDR buffer.
        /// </param>
        private void DestroyLongMsgBuffer(UIntPtr ptr)
        {
            var newPtr = ptr.ToIntPtr();
            var size = (uint) Marshal.SizeOf(typeof (MIDIHDR));
            CheckReturnCode(Win32API.midiInUnprepareHeader(_handle, newPtr, size));

            var header = (MIDIHDR) Marshal.PtrToStructure(newPtr, typeof (MIDIHDR));
            Marshal.FreeHGlobal(header.lpData);
            Marshal.FreeHGlobal(newPtr);

            _longMsgBuffers.Remove(newPtr);
        }

        private void InvokeSysEx(SysExMessage msg)
        {
            var @event = SysEx;
            @event?.Invoke(msg);
        }
    }
}