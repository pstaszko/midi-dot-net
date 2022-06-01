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
using System.Runtime.InteropServices;
using System.Text;
using Midi.Common;
using Midi.Enums;
using Midi.Messages;
using Midi.Win32;

namespace Midi.Devices
{
    public class InputDevice : DeviceBase, IInputDevice
    {
        // Thread-local, set to true when called by an input handler, false in all other threads.
        [ThreadStatic] private static bool _isInsideInputHandler;

        public readonly UIntPtr _deviceId;
        private readonly Win32API.MidiInProc _inputCallbackDelegate;
        private readonly List<IntPtr> _longMsgBuffers = new List<IntPtr>();
        private readonly NrpnWatcher _nrpnWatcher;
        // ReSharper disable once NotAccessedField.Local
        public MIDIINCAPS _caps;
        private Clock _clock;
        private HMIDIIN _handle;
        private bool _isClosing;
        private bool _isOpen;
        private bool _isReceiving;

        /// <summary>
        ///     Internal Constructor, only called by the getter for the InstalledDevices property.
        /// </summary>
        /// <param name="deviceId">Position of this device in the list of all devices.</param>
        /// <param name="caps">Win32 Struct with device metadata</param>
        internal InputDevice(UIntPtr deviceId, MIDIINCAPS caps)
            : base(caps.szPname)
        {
            _deviceId = deviceId;
            _caps = caps;
            _inputCallbackDelegate = InputCallback;
            _isOpen = false;
            _clock = null;
            _nrpnWatcher = new NrpnWatcher(this);
        }

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
		public int Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public event NoteOnHandler NoteOn;
        public event NoteOffHandler NoteOff;
        public event ControlChangeHandler ControlChange;
        public event ProgramChangeHandler ProgramChange;
        public event PitchBendHandler PitchBend;
        public event SysExHandler SysEx;
        public event NrpnHandler Nrpn;

        public void RemoveAllEventHandlers()
        {
            NoteOn = null;
            NoteOff = null;
            ControlChange = null;
            ProgramChange = null;
            PitchBend = null;
            SysEx = null;
        }

        public void Open()
        {
            if (_isInsideInputHandler)
            {
                throw new InvalidOperationException("Device is open.");
            }
            lock (this)
            {
                CheckNotOpen();
                CheckReturnCode(Win32API.midiInOpen(out _handle, _deviceId, _inputCallbackDelegate, (UIntPtr) 0));
                _isOpen = true;
            }
        }

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

        public void StartReceiving(Clock clock)
        {
            StartReceiving(clock, false);
        }

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

        private void InputCallback(HMIDIIN hMidiIn, MidiInMessage wMsg,
            UIntPtr dwInstance, UIntPtr dwParam1, UIntPtr dwParam2)
        {
            _isInsideInputHandler = true;
            try
            {
                switch (wMsg)
                {
                    case MidiInMessage.MIM_DATA:
                    {
                        HandleInputMimData(dwParam1, dwParam2);
                        break;
                    }
                    case MidiInMessage.MIM_LONGDATA:
                    {
                        HandleInputMimLongData(dwParam1, dwParam2);
                    }
                        break;
                    case MidiInMessage.MIM_MOREDATA:
                        SysEx?.Invoke(new SysExMessage(this, new byte[] {0x13}, 13));
                        break;
                    case MidiInMessage.MIM_OPEN:
                        //SysEx(new SysExMessage(this, new byte[] { 0x01 }, 1));
                        break;
                    case MidiInMessage.MIM_CLOSE:
                        //SysEx(new SysExMessage(this, new byte[] { 0x02 }, 2));
                        break;
                    case MidiInMessage.MIM_ERROR:
                        SysEx?.Invoke(new SysExMessage(this, new byte[] {0x03}, 3));
                        break;
                    case MidiInMessage.MIM_LONGERROR:
                        SysEx?.Invoke(new SysExMessage(this, new byte[] {0x04}, 4));
                        break;
                    default:
                        SysEx?.Invoke(new SysExMessage(this, new byte[] {0x05}, 5));
                        break;
                }
            }
            finally
            {
                _isInsideInputHandler = false;
            }
        }

        private void HandleInputMimData(UIntPtr dwParam1, UIntPtr dwParam2)
        {
            Channel channel;
            Pitch pitch;
            int velocity;
            int value;
            uint win32Timestamp;
            if (ShortMsg.IsNoteOn(dwParam1, dwParam2))
            {
                if (NoteOn == null) return;

                ShortMsg.DecodeNoteOn(dwParam1, dwParam2, out channel, out pitch,out velocity, out win32Timestamp);
                NoteOn(new NoteOnMessage(this, channel, pitch, velocity,_clock?.Time ?? win32Timestamp/1000f));
            }
            else if (ShortMsg.IsNoteOff(dwParam1, dwParam2))
            {
                if (NoteOff == null) return;

                ShortMsg.DecodeNoteOff(dwParam1, dwParam2, out channel, out pitch,out velocity, out win32Timestamp);
                NoteOff(new NoteOffMessage(this, channel, pitch, velocity,_clock?.Time ?? win32Timestamp/1000f));
            }
            else if (ShortMsg.IsControlChange(dwParam1, dwParam2))
            {
                Control control;
                ShortMsg.DecodeControlChange(dwParam1, dwParam2, out channel, out control, out value, out win32Timestamp);

                var msg = new ControlChangeMessage(this, channel, control, value, _clock?.Time ?? win32Timestamp/1000f);
                _nrpnWatcher.ReceivedControlChange(msg);
            }
            else if (ShortMsg.IsProgramChange(dwParam1, dwParam2))
            {
                if (ProgramChange == null) return;

                Instrument instrument;
                ShortMsg.DecodeProgramChange(dwParam1, dwParam2, out channel,out instrument, out win32Timestamp);
                ProgramChange(new ProgramChangeMessage(this, channel, instrument,_clock?.Time ?? win32Timestamp/1000f));
            }
            else if (ShortMsg.IsPitchBend(dwParam1, dwParam2))
            {
                if (PitchBend == null) return;

                ShortMsg.DecodePitchBend(dwParam1, dwParam2, out channel,out value, out win32Timestamp);
                PitchBend(new PitchBendMessage(this, channel, value,_clock?.Time ?? win32Timestamp/1000f));
            }
        }

        private void HandleInputMimLongData(UIntPtr dwParam1, UIntPtr dwParam2)
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

        private void CheckOpen()
        {
            if (!_isOpen)
            {
                throw new InvalidOperationException("Device is not open.");
            }
        }

        private void CheckNotOpen()
        {
            if (_isOpen)
            {
                throw new InvalidOperationException("Device is open.");
            }
        }

        private void CheckReceiving()
        {
            if (!_isReceiving)
            {
                throw new DeviceException("device not receiving");
            }
        }

        private void CheckNotReceiving()
        {
            if (_isReceiving)
            {
                throw new DeviceException("device receiving");
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

        private class NrpnWatcher
        {
            private readonly InputDevice _device;
            private readonly Queue<ControlChangeMessage> _messageQueue;
            private bool _queueMessages;
            private Control _lastControl;
            
            public NrpnWatcher(InputDevice device)
            {
                _device = device;
                _messageQueue = new Queue<ControlChangeMessage>(4);
            }

            public void ReceivedControlChange(ControlChangeMessage msg)
            {
                var control = msg.Control;

                if (_queueMessages)
                {
                    // Check if messages are following the NRPN protocol
                    if (IsExpectedControl(control))
                    {
                        _messageQueue.Enqueue(msg);

                        // When we have all four NRPN messages, we can assemble and send it
                        if (_messageQueue.Count == 4)
                        {
                            SendNrpn();
                        }
                    }
                    else
                    {
                        // Messages cannot be a NRPN, release queue
                        ReleaseQueue();
                        SendControlChange(msg);
                    }
                }
                else if (msg.Control == Control.NonRegisteredParameterMSB)
                {
                    // Might be start of NRPN, start queueing messages
                    _queueMessages = true;

                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    SendControlChange(msg);
                }


                _lastControl = control;
            }

            private void ReleaseQueue()
            {
                while (_messageQueue.Count > 0)
                {
                    var msg = _messageQueue.Dequeue();
                    SendControlChange(msg);
                }

                _queueMessages = false;
            }

            private void SendControlChange(ControlChangeMessage msg)
            {
                _device.ControlChange?.Invoke(msg);
            }

            private void SendNrpn()
            {
                var nrpn = _device.Nrpn;
                if (nrpn != null)
                {
                    var firstMsg = _messageQueue.Peek();
                    var parameter = DequeueInt14();
                    var value = DequeueInt14();

                    var msg = new NrpnMessage(_device, firstMsg.Channel, parameter.Value, value.Value, firstMsg.Time);
                    nrpn(msg);
                }
                else
                {
                    _messageQueue.Clear();
                }

                // Stop queueing messages
                _queueMessages = false;
            }

            private Int14 DequeueInt14()
            {
                if (_messageQueue.Count < 2)
                    throw new InvalidOperationException(
                        $"Cannot construct 14-bit Integer from message queue, as there are only {_messageQueue.Count} messages, needs at least 2");
                
                return new Int14
                {
                    MSB = _messageQueue.Dequeue().Value,
                    LSB = _messageQueue.Dequeue().Value
                };
            }

            private bool IsExpectedControl(Control control)
            {
                switch (control)
                {
                    case Control.NonRegisteredParameterLSB:
                        return _lastControl == Control.NonRegisteredParameterMSB;
                    case Control.DataEntryMSB:
                        return _lastControl == Control.NonRegisteredParameterLSB;
                    case Control.DataEntryLSB:
                        return _lastControl == Control.DataEntryMSB;
                    default:
                        return false;
                }
            }
        }
    }
}