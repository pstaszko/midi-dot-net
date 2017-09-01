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
using System.Collections.Generic;
using Midi.Devices;
using Midi.Enums;
using Midi.Instruments;
using Midi.Messages;

namespace MidiExamples
{
    public class Example05 : ExampleBase
    {
        public Example05()
            : base("Example05.cs", "Prints notes and chords as they are played.")
        {
        }

        public override void Run()
        {
            // Prompt user to choose an input device (or if there is only one, use that one).
            var inputDevice = ExampleUtil.ChooseInputDeviceFromConsole();
            if (inputDevice == null)
            {
                Console.WriteLine("No input devices, so can't run this example.");
                ExampleUtil.PressAnyKeyToContinue();
                return;
            }
            inputDevice.Open();
            inputDevice.StartReceiving(null);

            var summarizer = new Summarizer(inputDevice);
            ExampleUtil.PressAnyKeyToContinue();
            inputDevice.StopReceiving();
            inputDevice.Close();
            inputDevice.RemoveAllEventHandlers();
        }

        public class Summarizer
        {
            private readonly Dictionary<Pitch, bool> _pitchesPressed;

            private IInputDevice _inputDevice;

            public Summarizer(IInputDevice inputDevice)
            {
                _inputDevice = inputDevice;
                _pitchesPressed = new Dictionary<Pitch, bool>();
                inputDevice.NoteOn += NoteOn;
                inputDevice.NoteOff += NoteOff;
                PrintStatus();
            }

            private void PrintStatus()
            {
                Console.Clear();
                Console.WriteLine("Play notes and chords on the MIDI input device, and watch");
                Console.WriteLine("their names printed here.  Press any QUERTY key to quit.");
                Console.WriteLine();

                // Print the currently pressed notes.
                var pitches = new List<Pitch>(_pitchesPressed.Keys);
                pitches.Sort();
                Console.Write("Notes: ");
                for (var i = 0; i < pitches.Count; ++i)
                {
                    var pitch = pitches[i];
                    if (i > 0)
                    {
                        Console.Write(", ");
                    }
                    Console.Write("{0}", pitch.NotePreferringSharps());
                    if (pitch.NotePreferringSharps() != pitch.NotePreferringFlats())
                    {
                        Console.Write(" or {0}", pitch.NotePreferringFlats());
                    }
                }
                Console.WriteLine();
                // Print the currently held down chord.
                var chords = Chord.FindMatchingChords(pitches);
                Console.Write("Chords: ");
                for (var i = 0; i < chords.Count; ++i)
                {
                    var chord = chords[i];
                    if (i > 0)
                    {
                        Console.Write(", ");
                    }
                    Console.Write("{0}", chord);
                }
                Console.WriteLine();
            }

            public void NoteOn(NoteOnMessage msg)
            {
                lock (this)
                {
                    _pitchesPressed[msg.Pitch] = true;
                    PrintStatus();
                }
            }

            public void NoteOff(NoteOffMessage msg)
            {
                lock (this)
                {
                    _pitchesPressed.Remove(msg.Pitch);
                    PrintStatus();
                }
            }
        }
    }
}