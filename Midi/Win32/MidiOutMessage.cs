// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     "Midi Out Messages", passed to wMsg param of MidiOutProc.
    /// </summary>
    internal enum MidiOutMessage : uint
    {
        MOM_OPEN = 0x3C7,
        MOM_CLOSE = 0x3C8,
        MOM_DONE = 0x3C9
    }
}