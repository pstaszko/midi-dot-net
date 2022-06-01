// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     Flags for dwSupport field of MIDIOUTCAPS structure.
    /// </summary>
    public enum MidiExtraFeatures : uint
    {
        MIDICAPS_VOLUME = 0x0001,
        MIDICAPS_LRVOLUME = 0x0002,
        MIDICAPS_CACHE = 0x0004,
        MIDICAPS_STREAM = 0x0008
    }
}