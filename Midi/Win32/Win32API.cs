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

// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     C# wrappers for the Win32 MIDI API.
    /// </summary>
    /// Because .NET does not provide MIDI support itself, in C# we must use P/Invoke to wrap the
    /// Win32 API.  That API consists of the MMSystem.h C header and the winmm.dll library.  The API
    /// is described in detail here: http://msdn.microsoft.com/en-us/library/ms712733(VS.85).aspx.
    /// The P/Invoke interop mechanism is described here:
    /// http://msdn.microsoft.com/en-us/library/aa288468(VS.71).aspx.
    /// 
    /// This file covers the subset of the MIDI protocol needed to manage input and output devices
    /// and send and receive Note On/Off, Control Change, Pitch Bend and Program Change messages.
    /// Other portions of the MIDI protocol (such as sysex events) are supported in the Win32 API
    /// but are not wrapped here.
    /// 
    /// Some of the C functions are not typesafe when wrapped, so those wrappers are made private
    /// and typesafe variants are provided.
    internal static class Win32API
    {
        /// <summary>
        ///     Callback invoked when a MIDI event is received from an input device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711612(VS.85).aspx
        public delegate void MidiInProc(HMIDIIN hMidiIn, MidiInMessage wMsg, UIntPtr dwInstance,
            UIntPtr dwParam1, UIntPtr dwParam2);

        /// <summary>
        ///     Callback invoked when a MIDI output device is opened, closed, or finished with a buffer.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711637(VS.85).aspx
        public delegate void MidiOutProc(HMIDIOUT hmo, MidiOutMessage wMsg, UIntPtr dwInstance,
            UIntPtr dwParam1, UIntPtr dwParam2);

        // The following constants come from MMSystem.h.

        /// <summary>
        ///     Max length of a manufacturer name in the Win32 API.
        /// </summary>
        public const uint MaxPNameLen = 32;

        /// <summary>
        ///     Returns the number of MIDI output devices on this system.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711627(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiOutGetNumDevs();

        /// <summary>
        ///     Fills in the capabilities struct for a specific output device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711621(VS.85).aspx
        public static MMRESULT midiOutGetDevCaps(UIntPtr uDeviceId, out MidiOutCaps caps)
        {
            return midiOutGetDevCaps(uDeviceId, out caps,
                (uint) Marshal.SizeOf(typeof (MidiOutCaps)));
        }

        /// <summary>
        ///     Opens a MIDI output device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711632(VS.85).aspx
        public static MMRESULT midiOutOpen(out HMIDIOUT lphmo, UIntPtr uDeviceId,
            MidiOutProc dwCallback, UIntPtr dwCallbackInstance)
        {
            //return midiOutOpen(out lphmo, uDeviceID, dwCallback, dwCallbackInstance,
            //    dwCallback == null ? MidiOpenFlags.CALLBACK_NULL : MidiOpenFlags.CALLBACK_FUNCTION);

            return midiOutOpen(out lphmo, uDeviceId, dwCallback, dwCallbackInstance,
                dwCallback == null
                    ? MidiOpenFlags.CALLBACK_NULL
                    : MidiOpenFlags.CALLBACK_FUNCTION & MidiOpenFlags.MIDI_IO_STATUS);
        }

        /// <summary>
        ///     Turns off all notes and sustains on a MIDI output device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/dd798479(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutReset(HMIDIOUT hmo);

        /// <summary>
        ///     Closes a MIDI output device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711620(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutClose(HMIDIOUT hmo);

        /// <summary>
        ///     Sends a short MIDI message (anything but sysex or stream).
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711640(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutShortMsg(HMIDIOUT hmo, uint dwMsg);

        /// <summary>
        ///     Sends a long MIDI message (sysex).
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711629(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutLongMsg(HMIDIOUT hmo, IntPtr lpMidiOutHdr, uint cbMidiOutHdr);

        // MMRESULT midiOutLongMsg(HMIDIOUT hmo, LPMIDIHDR lpMidiOutHdr, UINT cbMidiOutHdr);

        /// <summary>
        ///     Prepares a long message for sending
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711634(v=VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutPrepareHeader(HMIDIOUT hmo, IntPtr lpMidiOutHdr, uint cbMidiOutHdr);

        /// <summary>
        ///     Frees header space after sending long message
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711641(v=VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutUnprepareHeader(HMIDIOUT hmo, IntPtr lpMidiOutHdr, uint cbMidiOutHdr);

        /// <summary>
        ///     Gets the error text for a return code related to an output device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711622(VS.85).aspx
        public static MMRESULT midiOutGetErrorText(MMRESULT mmrError, StringBuilder lpText)
        {
            return midiOutGetErrorText(mmrError, lpText, (uint) lpText.Capacity);
        }

        /// <summary>
        ///     Returns the number of MIDI input devices on this system.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711608(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInGetNumDevs();

        /// <summary>
        ///     Fills in the capabilities struct for a specific input device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711604(VS.85).aspx
        public static MMRESULT midiInGetDevCaps(UIntPtr uDeviceId, out MIDIINCAPS caps)
        {
            return midiInGetDevCaps(uDeviceId, out caps,
                (uint) Marshal.SizeOf(typeof (MIDIINCAPS)));
        }

        /// <summary>
        ///     Opens a MIDI input device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711610(VS.85).aspx
        public static MMRESULT midiInOpen(out HMIDIIN lphMidiIn, UIntPtr uDeviceId,
            MidiInProc dwCallback, UIntPtr dwCallbackInstance)
        {
            return midiInOpen(out lphMidiIn, uDeviceId, dwCallback, dwCallbackInstance,
                dwCallback == null ? MidiOpenFlags.CALLBACK_NULL : MidiOpenFlags.CALLBACK_FUNCTION);
        }

        /// <summary>
        ///     Starts input on a MIDI input device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711614(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInStart(HMIDIIN hMidiIn);

        /// <summary>
        ///     Stops input on a MIDI input device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711615(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInStop(HMIDIIN hMidiIn);

        /// <summary>
        ///     Resets input on a MIDI input device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711613(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInReset(HMIDIIN hMidiIn);

        /// <summary>
        ///     Closes a MIDI input device.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711602(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInClose(HMIDIIN hMidiIn);

        /// <summary>
        ///     Gets the error text for a return code related to an input device.
        /// </summary>
        /// NOTE: This is adapted from the original Win32 function in order to make it typesafe.
        /// 
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711605(VS.85).aspx
        public static MMRESULT midiInGetErrorText(MMRESULT mmrError, StringBuilder lpText)
        {
            return midiInGetErrorText(mmrError, lpText, (uint) lpText.Capacity);
        }

        /// <summary>
        ///     Send a buffer to and input device in order to receive SysEx messages.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/dd798450(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInAddBuffer(HMIDIIN hMidiIn, IntPtr lpMidiInHdr, uint cbMidiInHdr);

        /// <summary>
        ///     Prepare an input buffer before passing to midiInAddBuffer.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/dd798459(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInPrepareHeader(HMIDIIN hMidiIn, IntPtr headerPtr, uint cbMidiInHdr);

        /// <summary>
        ///     Clean up preparation performed by midiInPrepareHeader.
        /// </summary>
        /// Win32 docs: http://msdn.microsoft.com/en-us/library/dd798464(VS.85).aspx
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInUnprepareHeader(HMIDIIN hMidiIn, IntPtr headerPtr, uint cbMidiInHdr);

        // The bindings in this section are not typesafe, so we make them private and privide
        // typesafe variants above.

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiOutGetDevCaps(UIntPtr uDeviceId, out MidiOutCaps caps,
            uint cbMidiOutCaps);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiOutOpen(out HMIDIOUT lphmo, UIntPtr uDeviceId,
            MidiOutProc dwCallback, UIntPtr dwCallbackInstance, MidiOpenFlags dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiOutGetErrorText(MMRESULT mmrError, StringBuilder lpText,
            uint cchText);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInGetDevCaps(UIntPtr uDeviceId, out MIDIINCAPS caps,
            uint cbMidiInCaps);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInOpen(out HMIDIIN lphMidiIn, UIntPtr uDeviceId,
            MidiInProc dwCallback, UIntPtr dwCallbackInstance, MidiOpenFlags dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInGetErrorText(MMRESULT mmrError, StringBuilder lpText,
            uint cchText);
    }
}