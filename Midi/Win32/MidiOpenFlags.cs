using System;

// ReSharper disable InconsistentNaming

namespace Midi.Win32
{
    /// <summary>
    ///     Flags passed to midiInOpen() and midiOutOpen().
    /// </summary>
    [Flags]
    internal enum MidiOpenFlags : uint
    {
        CALLBACK_TYPEMASK = 0x70000,
        CALLBACK_NULL = 0x00000,
        CALLBACK_WINDOW = 0x10000,
        CALLBACK_TASK = 0x20000,
        CALLBACK_FUNCTION = 0x30000,
        CALLBACK_THREAD = CALLBACK_TASK,
        CALLBACK_EVENT = 0x50000,
        MIDI_IO_STATUS = 0x00020
    }
}