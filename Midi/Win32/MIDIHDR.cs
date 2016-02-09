using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     Strut to hold outgoing long messages (sysex)
    /// </summary>
    /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711592(v=VS.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIDIHDR
    {
        public IntPtr lpData;
        public int dwBufferLength;
        public int dwBytesRecorded;
        public IntPtr dwUser;
        public int dwFlags;
        public IntPtr lpNext;
        public IntPtr reserved;
        public int dwOffset;

        //public IntPtr dwReserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public int[] reservedArray;
    }
}