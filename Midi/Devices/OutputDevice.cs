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
using System.Runtime.InteropServices;
using System.Text;
using Midi.Common;
using Midi.Enums;
using Midi.Win32;

namespace Midi.Devices
{
    
    public class OutputDevice : DeviceBase, IOutputDevice
    {
        // Access to the global state is guarded by lock(staticLock).

        // The fields initialized in the constructor never change after construction,
        // so they don't need to be guarded by a lock.
        public readonly UIntPtr _deviceId;
        public readonly ushort _pid;
        // ReSharper disable once NotAccessedField.Local
        public MidiOutCaps _caps;
        private HMIDIOUT _handle;
        private bool _isOpen;

        /// <summary>
        ///     Internal Constructor, only called by the getter for the InstalledDevices property.
        /// </summary>
        /// <param name="deviceId">Position of this device in the list of all devices.</param>
        /// <param name="caps">Win32 Struct with device metadata</param>
        internal OutputDevice(UIntPtr deviceId, MidiOutCaps caps)
            : base(caps.szPname)
        {
            //_pid = caps.wPid;
            _deviceId = deviceId;
            _caps = caps;
            _isOpen = false;
        }
        
        public bool IsOpen
        {
            get
            {
                lock (this)
                {
                    return _isOpen;
                }
            }
        }

		public int Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public void Open()
        {
            lock (this)
            {
                CheckNotOpen();
                CheckReturnCode(Win32API.midiOutOpen(out _handle, _deviceId, null, (UIntPtr) 0));
                _isOpen = true;
            }
        }
        
        public void Close()
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutClose(_handle));
                _isOpen = false;
            }
        }
        
        public void SilenceAllNotes()
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutReset(_handle));
            }
        }
        
        public void SendNoteOn(Channel channel, Pitch pitch, int velocity)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeNoteOn(channel,
                    pitch, velocity)));
            }
        }
        
        public void SendNoteOff(Channel channel, Pitch pitch, int velocity)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeNoteOff(channel,
                    pitch, velocity)));
            }
        }
        
        public void SendPercussion(Percussion percussion, int velocity)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeNoteOn(
                    Channel.Channel10, (Pitch) percussion,
                    velocity)));
            }
        }
        
        public void SendControlChange(Channel channel, Control control, int value)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeControlChange(
                    channel, control, value)));
            }
        }
        
        public void SendPitchBend(Channel channel, int value)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodePitchBend(channel,
                    value)));
            }
        }
        
        public void SendProgramChange(Channel channel, Instrument instrument)
        {
            lock (this)
            {
                CheckOpen();
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeProgramChange(
                    channel, instrument)));
            }
        }
        
        public void SendSysEx(byte[] data)
        {
            lock (this)
            {
                //Win32API.MMRESULT result;
                IntPtr ptr;
                var size = (uint) Marshal.SizeOf(typeof (MIDIHDR));
                var header = new MIDIHDR {lpData = Marshal.AllocHGlobal(data.Length)};
                for (var i = 0; i < data.Length; i++)
                    Marshal.WriteByte(header.lpData, i, data[i]);
                header.dwBufferLength = data.Length;
                header.dwBytesRecorded = data.Length;
                header.dwFlags = 0;

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

                //result = Win32API.midiOutPrepareHeader(handle, ptr, size);
                //if (result == 0) result = Win32API.midiOutLongMsg(handle, ptr, size);
                //if (result == 0) result = Win32API.midiOutUnprepareHeader(handle, ptr, size);
                CheckReturnCode(Win32API.midiOutPrepareHeader(_handle, ptr, size));
                CheckReturnCode(Win32API.midiOutLongMsg(_handle, ptr, size));
                CheckReturnCode(Win32API.midiOutUnprepareHeader(_handle, ptr, size));

                Marshal.FreeHGlobal(header.lpData);
                Marshal.FreeHGlobal(ptr);
            }
        }

        public void SendNrpn(Channel channel, int parameter, int value)
        {
            lock (this)
            {
                CheckOpen();

                var parameter14 = new Int14(parameter);
                var value14 = new Int14(value);

                // Parameter, MSB
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeControlChange(
                    channel, Control.NonRegisteredParameterMSB, parameter14.MSB)));

                // Parameter, LSB
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeControlChange(
                    channel, Control.NonRegisteredParameterLSB, parameter14.LSB)));

                // Value, MSB
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeControlChange(
                    channel, Control.DataEntryMSB, value14.MSB)));

                // Value, LSB
                CheckReturnCode(Win32API.midiOutShortMsg(_handle, ShortMsg.EncodeControlChange(
                    channel, Control.DataEntryLSB, value14.LSB)));
            }
        }

        private static void CheckReturnCode(MMRESULT rc)
        {
            if (rc != MMRESULT.MMSYSERR_NOERROR)
            {
                var errorMsg = new StringBuilder(128);
                rc = Win32API.midiOutGetErrorText(rc, errorMsg);
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
                throw new InvalidOperationException("device not open");
            }
        }

        private void CheckNotOpen()
        {
            if (_isOpen)
            {
                throw new InvalidOperationException("device open");
            }
        }
    }
}