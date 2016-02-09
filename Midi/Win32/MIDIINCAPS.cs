using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     Struct representing the capabilities of an input device.
    /// </summary>
    /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711596(VS.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIDIINCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) Win32API.MaxPNameLen)] public string szPname;
        public uint dwSupport;
    }
}