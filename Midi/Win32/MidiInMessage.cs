// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     "Midi In Messages", passed to wMsg param of MidiInProc.
    /// </summary>
    internal enum MidiInMessage : uint
    {
        MIM_OPEN = 0x3C1,
        MIM_CLOSE = 0x3C2,
        MIM_DATA = 0x3C3,
        MIM_LONGDATA = 0x3C4,
        MIM_ERROR = 0x3C5,
        MIM_LONGERROR = 0x3C6,
        MIM_MOREDATA = 0x3CC
    }
}