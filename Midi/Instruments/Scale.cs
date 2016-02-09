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
using Midi.Enums;

namespace Midi.Instruments
{
    /// <summary>
    ///     A scale based on a pattern and a tonic note.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For our purposes, a scale is defined by a tonic and then the pattern that it uses to
    ///         ascend up to the next tonic.  The tonic is described with a <see cref="Note" /> because it is
    ///         not specific to any one octave.  The ascending pattern is provided by the
    ///         <see cref="ScalePattern" /> class.
    ///     </para>
    ///     <para>
    ///         This class comes with a collection of predefined patterns, such as
    ///         <see cref="Major" /> and <see cref="Scale.HarmonicMinor" />.
    ///     </para>
    /// </remarks>
    public class Scale
    {
        /// <summary>
        ///     Pattern for Major scales.
        /// </summary>
        public static ScalePattern Major =
            new ScalePattern("Major", new[] {0, 2, 4, 5, 7, 9, 11});

        /// <summary>
        ///     Pattern for Natural Minor scales.
        /// </summary>
        public static ScalePattern NaturalMinor =
            new ScalePattern("Natural Minor", new[] {0, 2, 3, 5, 7, 8, 10});

        /// <summary>
        ///     Pattern for Harmonic Minor scales.
        /// </summary>
        public static ScalePattern HarmonicMinor =
            new ScalePattern("Harmonic Minor", new[] {0, 2, 3, 5, 7, 8, 11});

        /// <summary>
        ///     Pattern for Melodic Minor scale as it ascends.
        /// </summary>
        public static ScalePattern MelodicMinorAscending =
            new ScalePattern("Melodic Minor (ascending)",
                new[] {0, 2, 3, 5, 7, 9, 11});

        /// <summary>
        ///     Pattern for Melodic Minor scale as it descends.
        /// </summary>
        public static ScalePattern MelodicMinorDescending =
            new ScalePattern("Melodic Minor (descending)",
                new[] {0, 2, 3, 5, 7, 8, 10});

        /// <summary>
        ///     Pattern for Chromatic scales.
        /// </summary>
        public static ScalePattern Chromatic =
            new ScalePattern("Chromatic",
                new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11});

        /// <summary>
        ///     Array of all the built-in scale patterns.
        /// </summary>
        public static ScalePattern[] Patterns =
        {
            Major,
            NaturalMinor,
            HarmonicMinor,
            MelodicMinorAscending,
            MelodicMinorDescending,
            Chromatic
        };

        private readonly int[] _positionInOctaveToSequenceIndex; // for each PositionInOctave, the 0-indexed

        /// <summary>
        ///     Constructs a scale from its tonic and its pattern.
        /// </summary>
        /// <param name="tonic">The tonic note.</param>
        /// <param name="pattern">The scale pattern.</param>
        /// <exception cref="ArgumentNullException">tonic or pattern is null.</exception>
        public Scale(Note tonic, ScalePattern pattern)
        {
            if (tonic == null || pattern == null)
            {
                throw new ArgumentNullException();
            }
            Tonic = tonic;
            Pattern = pattern;
            _positionInOctaveToSequenceIndex = new int[12];
            NoteSequence = new Note[pattern.Ascent.Length];
            int numAccidentals;
            Build(Tonic, Pattern, _positionInOctaveToSequenceIndex, NoteSequence,
                out numAccidentals);
        }

        /// <summary>
        ///     The scale's human-readable name, such as "G# Major" or "Eb Melodic Minor (ascending)".
        /// </summary>
        public string Name => $"{Tonic} {Pattern}";

        /// <summary>The tonic of this scale.</summary>
        public Note Tonic { get; }

        /// <summary>The pattern of this scale.</summary>
        public ScalePattern Pattern { get; }

        /// <summary>
        ///     The sequence of notes in this scale.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This sequence begins at the tonic and ascends, stopping before the next tonic.
        ///     </para>
        /// </remarks>
        public Note[] NoteSequence { get; }

        /// <summary>
        ///     Returns true if pitch is in this scale.
        /// </summary>
        /// <param name="pitch">The pitch to test.</param>
        /// <returns>True if pitch is in this scale.</returns>
        public bool Contains(Pitch pitch)
        {
            return ScaleDegree(pitch) != -1;
        }

        /// <summary>
        ///     Returns the scale degree of the given pitch in this scale.
        /// </summary>
        /// <param name="pitch">The pitch to test.</param>
        /// <returns>
        ///     The scale degree of pitch in this scale, where 1 is the tonic.  Returns -1
        ///     if pitch is not in this scale.
        /// </returns>
        public int ScaleDegree(Pitch pitch)
        {
            var result = _positionInOctaveToSequenceIndex[pitch.PositionInOctave()];
            return result == -1 ? -1 : result + 1;
        }

        /// <summary>
        ///     ToString returns the scale's human-readable name.
        /// </summary>
        /// <returns>
        ///     The scale's name, such as "G# Major" or "Eb Melodic Minor (ascending)".
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Equality operator does value equality because Scale is immutable.
        /// </summary>
        public static bool operator ==(Scale a, Scale b)
        {
            return a != null && (ReferenceEquals(a, b) || a.Equals(b));
        }

        /// <summary>
        ///     Inequality operator does value inequality because Chord is immutable.
        /// </summary>
        public static bool operator !=(Scale a, Scale b)
        {
            return a != null && !(ReferenceEquals(a, b) || a.Equals(b));
        }

        /// <summary>
        ///     Value equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Scale;
            if ((object) other == null)
            {
                return false;
            }

            return ReferenceEquals(this, obj) || (Tonic == other.Tonic && Pattern == other.Pattern);
        }

        /// <summary>
        ///     Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return Tonic.GetHashCode() + Pattern.GetHashCode();
        }

        /// <summary>
        ///     Builds a scale.
        /// </summary>
        /// <param name="tonic">The tonic.</param>
        /// <param name="pattern">The scale pattern.</param>
        /// <param name="positionInOctaveToSequenceIndex">
        ///     Must have 12 elements, and is filled with
        ///     the 0-indexed scale position (or -1) for each position in the octave.
        /// </param>
        /// <param name="noteSequence">
        ///     Must have pattern.Ascent.Length elements, and is filled with
        ///     the notes for each scale degree.
        /// </param>
        /// <param name="numAccidentals">
        ///     Filled with the total number of accidentals in the built
        ///     scale.
        /// </param>
        private static void Build(Note tonic, ScalePattern pattern,
            int[] positionInOctaveToSequenceIndex, Note[] noteSequence, out int numAccidentals)
        {
            numAccidentals = 0;
            for (var i = 0; i < 12; ++i)
            {
                positionInOctaveToSequenceIndex[i] = -1;
            }
            var tonicPitch = tonic.PitchInOctave(0);
            for (var i = 0; i < pattern.Ascent.Length; ++i)
            {
                var pitch = tonicPitch + pattern.Ascent[i];
                Note note;
                if (pattern.Ascent.Length == 7)
                {
                    var letter = (char) (i + tonic.Letter);
                    if (letter > 'G')
                    {
                        letter = (char) (letter - 7);
                    }
                    note = pitch.NoteWithLetter(letter);
                }
                else
                {
                    note = pitch.NotePreferringSharps();
                }
                noteSequence[i] = note;
                positionInOctaveToSequenceIndex[pitch.PositionInOctave()] = i;
            }
        }

        // position of that pitch in noteSequence,

        // or -1 if it's not in the scale.
    }
}