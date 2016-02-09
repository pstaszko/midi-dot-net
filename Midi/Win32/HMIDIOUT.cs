using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     Win32 handle for a MIDI output device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct HMIDIOUT
    {
        public IntPtr handle;
    }
}