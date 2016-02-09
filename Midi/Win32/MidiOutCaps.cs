using System.Runtime.InteropServices;

namespace Midi.Win32
{
    /// <summary>
    ///     Struct representing the capabilities of an output device.
    /// </summary>
    /// Win32 docs: http://msdn.microsoft.com/en-us/library/ms711619(VS.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct MidiOutCaps
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) Win32API.MaxPNameLen)] public string szPname;
        public MidiDeviceType wTechnology;
        public ushort wVoices;
        public ushort wNotes;
        public ushort wChannelMask;
        public MidiExtraFeatures dwSupport;
    }
}