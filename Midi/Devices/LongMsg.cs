// Copyright (c) 2011, Justin Ryan

using System;
using System.Runtime.InteropServices;
using Midi.Common;
using Midi.Win32;

namespace Midi.Devices
{
    /// <summary>
    ///     Utility functions for encoding and decoding short messages.
    /// </summary>
    internal static class LongMsg
    {
        /// <summary>
        ///     Returns true if the given long message describes a SysEx message.
        /// </summary>
        /// <param name="dwParam1">The dwParam1 arg passed to MidiInProc.</param>
        /// <param name="dwParam2">The dwParam2 arg passed to MidiInProc.</param>
        public static bool IsSysEx(UIntPtr dwParam1, UIntPtr dwParam2)
        {
            var newPtr = dwParam1.ToIntPtr();

            try
            {
                Marshal.PtrToStructure<MIDIHDR>(newPtr);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Decodes a SysEx long message.
        /// </summary>
        /// <param name="dwParam1">The dwParam1 arg passed to MidiInProc.</param>
        /// <param name="dwParam2">The dwParam2 arg passed to MidiInProc.</param>
        /// <param name="data">The SysEx data to send.</param>
        /// <param name="timestamp">
        ///     Filled in with the timestamp in microseconds since
        ///     midiInStart().
        /// </param>
        public static void DecodeSysEx(UIntPtr dwParam1, UIntPtr dwParam2, out byte[] data, out uint timestamp)
        {
            //if (!IsSysEx(dwParam1, dwParam2))
            //{
            //    throw new ArgumentException("Not a SysEx message.");
            //}
            var newPtr = dwParam1.ToIntPtr();
            var header = (MIDIHDR) Marshal.PtrToStructure(newPtr, typeof (MIDIHDR));
            data = new byte[header.dwBytesRecorded];
            for (var i = 0; i < header.dwBytesRecorded; i++)
            {
                //Array.Resize<byte>(ref data, data.Length + 1);
                //data[data.Length - 1] = System.Runtime.InteropServices.Marshal.ReadByte(header.lpData, i);
                data[i] = Marshal.ReadByte(header.lpData, i);
            }
            timestamp = (uint) dwParam2;
        }

        /*
        /// <summary>
        /// Encodes a SysEx long message.
        /// </summary>
        /// <param name="data">SysEx Data.</param>
        /// <returns>A value that can be passed to midiOutShortMsg.</returns>
        /// <exception cref="ArgumentOutOfRangeException">pitch is not in MIDI range.</exception>
        //public static UInt32 EncodeSysEx(Byte[] data)
        //{
        //}
        */
    }
}