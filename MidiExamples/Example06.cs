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
    /// <summary>
    ///     Simple arpeggiator.
    /// </summary>
    /// <remarks>
    ///     This example demonstrates input, output and Clock-based scheduling.  As Note On and
    ///     Note Off events are received from the input device, the Arpeggiator class schedules
    ///     arpeggiated chords or scales based on the note played.
    /// </remarks>
    public class Example06 : ExampleBase
    {
        public Example06()
            : base("Example06.cs", "Arpeggiator.")
        {
        }

        public override void Run()
        {
            // Create a clock running at the specified beats per minute.
            var beatsPerMinute = 180;
            var clock = new Clock(beatsPerMinute);

            // Prompt user to choose an output device (or if there is only one, use that one.
            var outputDevice = ExampleUtil.ChooseOutputDeviceFromConsole();
            if (outputDevice == null)
            {
                Console.WriteLine("No output devices, so can't run this example.");
                ExampleUtil.PressAnyKeyToContinue();
                return;
            }
            outputDevice.Open();

            // Prompt user to choose an input device (or if there is only one, use that one).
            var inputDevice = ExampleUtil.ChooseInputDeviceFromConsole();
            inputDevice?.Open();

            var arpeggiator = new Arpeggiator(inputDevice, outputDevice, clock);
            var drummer = new Drummer(clock, outputDevice, 4);

            clock.Start();
            inputDevice?.StartReceiving(clock);

            var done = false;
            while (!done)
            {
                Console.Clear();
                Console.WriteLine("BPM = {0}, Playing = {1}, Arpeggiator Mode = {2}",
                    clock.BeatsPerMinute, clock.IsRunning, arpeggiator.Status);
                Console.WriteLine("Escape : Quit");
                Console.WriteLine("Down : Slower");
                Console.WriteLine("Up: Faster");
                Console.WriteLine("Left: Previous Chord or Scale");
                Console.WriteLine("Right: Next Chord or Scale");
                Console.WriteLine("Space = Toggle Play");
                Console.WriteLine("Enter = Toggle Scales/Chords");
                var key = Console.ReadKey(true).Key;
                Pitch pitch;
                if (key == ConsoleKey.Escape)
                {
                    done = true;
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    clock.BeatsPerMinute -= 2;
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    clock.BeatsPerMinute += 2;
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    arpeggiator.Change(1);
                }
                else if (key == ConsoleKey.LeftArrow)
                {
                    arpeggiator.Change(-1);
                }
                else if (key == ConsoleKey.Spacebar)
                {
                    if (clock.IsRunning)
                    {
                        clock.Stop();
                        inputDevice?.StopReceiving();
                        outputDevice.SilenceAllNotes();
                    }
                    else
                    {
                        clock.Start();
                        inputDevice?.StartReceiving(clock);
                    }
                }
                else if (key == ConsoleKey.Enter)
                {
                    arpeggiator.ToggleMode();
                }
                else if (ExampleUtil.IsMockPitch(key, out pitch))
                {
                    // We've hit a QUERTY key which is meant to simulate a MIDI note, so
                    // send the Note On to the output device and tell the arpeggiator.
                    var noteOn = new NoteOnMessage(outputDevice, 0, pitch, 100,
                        clock.Time);
                    clock.Schedule(noteOn);
                    arpeggiator.NoteOn(noteOn);
                    // We don't get key release events for the console, so schedule a
                    // simulated Note Off one beat from now.
                    var noteOff = new NoteOffMessage(outputDevice, 0, pitch, 100,
                        clock.Time + 1);
                    CallbackMessage.CallbackType noteOffCallback = beatTime => { arpeggiator.NoteOff(noteOff); };
                    clock.Schedule(new CallbackMessage(beatTime => arpeggiator.NoteOff(noteOff),
                        noteOff.Time));
                }
            }

            if (clock.IsRunning)
            {
                clock.Stop();
                inputDevice?.StopReceiving();
                outputDevice.SilenceAllNotes();
            }

            outputDevice.Close();
            if (inputDevice != null)
            {
                inputDevice.Close();
                inputDevice.RemoveAllEventHandlers();
            }

            // All done.
        }

        private class Arpeggiator
        {
            private readonly Clock _clock;
            private readonly IOutputDevice _outputDevice;
            private readonly Dictionary<Pitch, List<Pitch>> _lastSequenceForPitch;
            private int _currentChordPattern;
            private int _currentScalePattern;
            private IInputDevice _inputDevice;
            private bool _playingChords;

            public Arpeggiator(IInputDevice inputDevice, IOutputDevice outputDevice, Clock clock)
            {
                _inputDevice = inputDevice;
                _outputDevice = outputDevice;
                _clock = clock;
                _currentChordPattern = 0;
                _currentScalePattern = 0;
                _playingChords = false;
                _lastSequenceForPitch = new Dictionary<Pitch, List<Pitch>>();

                if (inputDevice != null)
                {
                    inputDevice.NoteOn += NoteOn;
                    inputDevice.NoteOff += NoteOff;
                }
            }

            /// <summary>
            ///     String describing the arpeggiator's current configuration.
            /// </summary>
            public string Status
            {
                get
                {
                    lock (this)
                    {
                        if (_playingChords)
                        {
                            return "Chord: " + Chord.Patterns[_currentChordPattern].Name;
                        }
                        return "Scale: " + Scale.Patterns[_currentScalePattern].Name;
                    }
                }
            }

            /// <summary>
            ///     Toggle between playing chords and playing scales.
            /// </summary>
            public void ToggleMode()
            {
                lock (this)
                {
                    _playingChords = !_playingChords;
                }
            }

            /// <summary>
            ///     Changes the current chord or scale, whichever is the current mode.
            /// </summary>
            public void Change(int delta)
            {
                lock (this)
                {
                    if (_playingChords)
                    {
                        _currentChordPattern = _currentChordPattern + delta;
                        while (_currentChordPattern < 0)
                        {
                            _currentChordPattern += Chord.Patterns.Length;
                        }
                        while (_currentChordPattern >= Chord.Patterns.Length)
                        {
                            _currentChordPattern -= Chord.Patterns.Length;
                        }
                    }
                    else
                    {
                        _currentScalePattern = _currentScalePattern + delta;
                        while (_currentScalePattern < 0)
                        {
                            _currentScalePattern += Scale.Patterns.Length;
                        }
                        while (_currentScalePattern >= Scale.Patterns.Length)
                        {
                            _currentScalePattern -= Scale.Patterns.Length;
                        }
                    }
                }
            }

            public void NoteOn(NoteOnMessage msg)
            {
                lock (this)
                {
                    var pitches = new List<Pitch>();
                    if (_playingChords)
                    {
                        var chord = new Chord(msg.Pitch.NotePreferringSharps(),
                            Chord.Patterns[_currentChordPattern], 0);
                        var p = msg.Pitch;
                        for (var i = 0; i < chord.NoteSequence.Length; ++i)
                        {
                            p = chord.NoteSequence[i].PitchAtOrAbove(p);
                            pitches.Add(p);
                        }
                    }
                    else
                    {
                        var scale = new Scale(msg.Pitch.NotePreferringSharps(),
                            Scale.Patterns[_currentScalePattern]);
                        var p = msg.Pitch;
                        for (var i = 0; i < scale.NoteSequence.Length; ++i)
                        {
                            p = scale.NoteSequence[i].PitchAtOrAbove(p);
                            pitches.Add(p);
                        }
                        pitches.Add(msg.Pitch + 12);
                    }
                    _lastSequenceForPitch[msg.Pitch] = pitches;
                    for (var i = 1; i < pitches.Count; ++i)
                    {
                        _clock.Schedule(new NoteOnMessage(_outputDevice, msg.Channel,
                            pitches[i], msg.Velocity, msg.Time + i));
                    }
                }
            }

            public void NoteOff(NoteOffMessage msg)
            {
                if (!_lastSequenceForPitch.ContainsKey(msg.Pitch))
                {
                    return;
                }
                var pitches = _lastSequenceForPitch[msg.Pitch];
                _lastSequenceForPitch.Remove(msg.Pitch);
                for (var i = 1; i < pitches.Count; ++i)
                {
                    _clock.Schedule(new NoteOffMessage(_outputDevice, msg.Channel,
                        pitches[i], msg.Velocity, msg.Time + i));
                }
            }
        }

        private class Drummer
        {
            private readonly int _beatsPerMeasure;
            private readonly Clock _clock;
            private readonly List<Message> _messagesForOneMeasure;
            private IOutputDevice _outputDevice;

            public Drummer(Clock clock, IOutputDevice outputDevice, int beatsPerMeasure)
            {
                _clock = clock;
                _outputDevice = outputDevice;
                _beatsPerMeasure = beatsPerMeasure;
                _messagesForOneMeasure = new List<Message>();
                for (var i = 0; i < beatsPerMeasure; ++i)
                {
                    var percussion = i == 0 ? Percussion.PedalHiHat : Percussion.MidTom1;
                    var velocity = i == 0 ? 100 : 40;
                    _messagesForOneMeasure.Add(new PercussionMessage(outputDevice, percussion,
                        velocity, i));
                }
                _messagesForOneMeasure.Add(new CallbackMessage(
                    CallbackHandler, 0));
                clock.Schedule(_messagesForOneMeasure, 0);
            }

            private void CallbackHandler(float time)
            {
                // Round up to the next measure boundary.
                var timeOfNextMeasure = time + _beatsPerMeasure;
                _clock.Schedule(_messagesForOneMeasure, timeOfNextMeasure);
            }
        }
    }
}