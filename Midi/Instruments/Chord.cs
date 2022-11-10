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
using Midi.Enums;

namespace Midi.Instruments
{
    /// <summary>
    ///     A chord.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A chord is defined by its root note, the sequence of semitones, the sequence of
    ///         letters, and the inversion.  The root note is described with a <see cref="Note" />
    ///         because we want to be able to talk about the chord independent of any one octave.  The
    ///         pattern of semitones and letters is given by the <see cref="Pattern" /> nested class.  The
    ///         inversion is an integer indicating how many rotations the pattern has undergone.
    ///     </para>
    ///     <para>
    ///         This class comes with a collection of predefined chord patterns, such as
    ///         <see cref="Major" /> and <see cref="Chord.Minor" />.
    ///     </para>
    /// </remarks>
    public class Chord
    {
        /// <summary>
        ///     Pattern for Major chords.
        /// </summary>
        public static readonly ChordPattern Major =
            new ChordPattern("Major", "", new[] { 0, 4, 7 }, new[] { 0, 2, 4 });

        /// <summary>
        ///     Pattern for Minor chords.
        /// </summary>
        public static readonly ChordPattern Minor =
            new ChordPattern("Minor", "m", new[] { 0, 3, 7 }, new[] { 0, 2, 4 });

        /// <summary>
        ///     Pattern for Seventh chords.
        /// </summary>
        public static readonly ChordPattern Seventh =
            new ChordPattern("Seventh", "7", new[] { 0, 4, 7, 10 }, new[] { 0, 2, 4, 6 });

        /// <summary>
        ///     Pattern for Augmented chords.
        /// </summary>
        public static readonly ChordPattern Augmented =
            new ChordPattern("Augmented", "aug", new[] { 0, 4, 8 }, new[] { 0, 2, 4 });

        /// <summary>
        ///     Pattern for Diminished chords.
        /// </summary>
        public static readonly ChordPattern Diminished =
            new ChordPattern("Diminished", "dim", new[] { 0, 3, 6 }, new[] { 0, 2, 4 });

        /// <summary>
        ///     Array of all the built-in chord patterns.
        /// </summary>
        public static readonly ChordPattern[] Patterns =
        {
            Major,
            Minor,
            Seventh,
            Augmented,
            Diminished
        };

        // is contained in this chord.
        private readonly bool[] _positionInOctaveToContains; // for each PositionInOctave, true if that pitch

        /// <summary>
        ///     Constructs a chord from its root note, pattern, and inversion.
        /// </summary>
        /// <param name="root">The root note of the chord.</param>
        /// <param name="pattern">The chord pattern.</param>
        /// <param name="inversion">
        ///     The inversion, in [0..N-1] where N is the number of notes
        ///     in pattern.
        /// </param>
        /// <exception cref="ArgumentNullException">pattern is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">inversion is out of range.</exception>
        public Chord(Note root, ChordPattern pattern, int inversion)
        {
            if (pattern == null) {
                throw new ArgumentNullException();
            }
            if (inversion < 0 || inversion >= pattern.Ascent.Length) {
                throw new ArgumentOutOfRangeException(nameof(inversion));
            }
            Root = root;
            Pattern = pattern;
            Inversion = inversion;
            _positionInOctaveToContains = new bool[12];
            var uninvertedSequence = new Note[pattern.Ascent.Length];
            Build(root, pattern, _positionInOctaveToContains,
                uninvertedSequence);
            NoteSequence = new Note[pattern.Ascent.Length];
            RotateArrayLeft(uninvertedSequence, NoteSequence, inversion);
        }

        /// <summary>
        ///     Constructs a chord from a string.
        /// </summary>
        /// <param name="name">
        ///     The name to parse.  This is the same format as the Name property:
        ///     a letter in ['A'..'G'], an optional series of accidentals (#'s or b's), then an
        ///     optional inversion specified as a '/' followed by another note name.  If the
        ///     inversion is present it must be one of the notes in the chord.
        /// </param>
        /// <exception cref="ArgumentNullException">name is null.</exception>
        /// <exception cref="ArgumentException">cannot parse a chord from name.</exception>
        public Chord(string name)
        {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0) {
                throw new ArgumentException("name is empty.");
            }
            var pos = 0;
            Root = Note.ParseNote(name, ref pos);
            Pattern = null;
            foreach (var p in Patterns) {
                if (pos + p.Abbreviation.Length > name.Length) {
                    continue;
                }
                if (string.Compare(name, pos, p.Abbreviation, 0, p.Abbreviation.Length) != 0) {
                    continue;
                }
                if (pos + p.Abbreviation.Length == name.Length ||
                    name[pos + p.Abbreviation.Length] == '/') {
                    pos += p.Abbreviation.Length;
                    Pattern = p;
                    break;
                }
            }
            if (Pattern == null) {
                throw new ArgumentException("name does not match a known chord pattern.");
            }
            // At this point, we know the note and pattern (but not yet the inversion).  Build
            // the chord prior to inversion.
            _positionInOctaveToContains = new bool[12];
            var uninvertedSequence = new Note[Pattern.Ascent.Length];
            Build(Root, Pattern, _positionInOctaveToContains,
                uninvertedSequence);
            NoteSequence = new Note[Pattern.Ascent.Length];
            // Now see if there's an inversion.
            Inversion = 0;
            if (pos < name.Length) {
                if (name[pos] != '/') {
                    throw new ArgumentException($"unexpected character '{name[pos]}' in name.");
                }
                pos++;
                var bass = Note.ParseNote(name, ref pos);
                if (name.Length > pos) {
                    throw new ArgumentException($"unexpected character '{name[pos]}' in name.");
                }
                Inversion = Array.IndexOf(uninvertedSequence, bass);
                if (Inversion == -1) {
                    throw new ArgumentException("invalid bass note for inversion.");
                }
            }
            RotateArrayLeft(uninvertedSequence, NoteSequence, Inversion);
        }

        /// <summary>
        ///     The name of this chord.
        /// </summary>
        public string Name
        {
            get {
                if (Inversion == 0) {
                    return $"{Root}{Pattern.Abbreviation}";
                }
                return $"{Root}{Pattern.Abbreviation}/{NoteSequence[0]}";
            }
        }

        /// <summary>The root note of this chord.</summary>
        public Note Root { get; }

        /// <summary>The bass note of this chord.</summary>
        public Note Bass => NoteSequence[0];

        /// <summary>The pattern of this chord.</summary>
        public ChordPattern Pattern { get; }

        /// <summary>The inversion of this chord.</summary>
        public int Inversion { get; }

        /// <summary>
        ///     The sequence of notes in this chord.
        /// </summary>
        public Note[] NoteSequence { get; }

        /// <summary>
        ///     Returns a list of chords which match the set of input pitches.
        /// </summary>
        /// <param name="pitches">Notes being analyzed.</param>
        /// <returns>A (possibly empty) list of chords.</returns>
        public static List<Chord> FindMatchingChords(List<Pitch> pitches)
        {
            var sorted = pitches.ToArray();
            Array.Sort(sorted);
            var semitonesAboveBass = new int[sorted.Length];
            for (var i = 0; i < sorted.Length; ++i) {
                semitonesAboveBass[i] = sorted[i] - sorted[0];
            }

            var result = new List<Chord>();
            foreach (var pattern in Patterns) {
                var semitoneSequence = pattern.Ascent;
                if (semitoneSequence.Length != semitonesAboveBass.Length) {
                    continue;
                }
                for (var inversion = 0; inversion < semitoneSequence.Length; ++inversion) {
                    var invertedSequence = new int[semitoneSequence.Length];
                    RotateArrayLeft(semitoneSequence, invertedSequence, inversion);
                    if (inversion != 0) {
                        for (var i = 0; i < semitoneSequence.Length - inversion; ++i) {
                            invertedSequence[i] -= 12;
                        }
                    }
                    var iSemitonesAboveBass = new int[invertedSequence.Length];
                    for (var i = 0; i < invertedSequence.Length; ++i) {
                        iSemitonesAboveBass[i] = invertedSequence[i] - invertedSequence[0];
                    }
                    var equals = true;
                    for (var i = 0; i < iSemitonesAboveBass.Length; ++i) {
                        if (iSemitonesAboveBass[i] != semitonesAboveBass[i]) {
                            equals = false;
                            break;
                        }
                    }
                    if (equals) {
                        var rootPitch =
                            inversion == 0 ? sorted[0] : sorted[sorted.Length - inversion];
                        var rootNote = rootPitch.NotePreferringSharps();
                        result.Add(new Chord(rootNote, pattern, inversion));
                        if (rootPitch.NotePreferringFlats() != rootNote) {
                            var otherRootNote = rootPitch.NotePreferringFlats();
                            result.Add(new Chord(otherRootNote, pattern, inversion));
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Returns true if this chord contains the specified pitch.
        /// </summary>
        /// <param name="pitch">The pitch to test.</param>
        /// <returns>True if this chord contains the pitch.</returns>
        public bool Contains(Pitch pitch)
        {
            return _positionInOctaveToContains[pitch.PositionInOctave()];
        }

        /// <summary>
        ///     ToString returns the chord name.
        /// </summary>
        /// <returns>The chord's name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Equality operator does value equality because Chord is immutable.
        /// </summary>
        public static bool operator ==(Chord a, Chord b)
        {
            return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
        }

        /// <summary>
        ///     Inequality operator does value inequality because Chord is immutable.
        /// </summary>
        public static bool operator !=(Chord a, Chord b)
        {
            return !(a == b);
        }

        /// <summary>
        ///     Value equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            var c = obj as Chord;
            if ((object)c == null) {
                return false;
            }

            return ReferenceEquals(this, obj) || (Root == c.Root && Pattern == c.Pattern &&
                                                  Inversion == c.Inversion);
        }

        /// <summary>
        ///     Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return Root.GetHashCode() + Inversion.GetHashCode() +
                   Pattern.GetHashCode();
        }

        private static void Build(Note root, ChordPattern pattern,
            bool[] positionInOctaveToContains, Note[] noteSequence)
        {
            for (var i = 0; i < 12; ++i) {
                positionInOctaveToContains[i] = false;
            }
            var rootPitch = root.PitchInOctave(0);
            for (var i = 0; i < pattern.Ascent.Length; ++i) {
                var pitch = rootPitch + pattern.Ascent[i];
                var letter = (char)(pattern.LetterOffsets[i] + root.Letter);
                while (letter > 'G') {
                    letter = (char)(letter - 7);
                }
                noteSequence[i] = pitch.NoteWithLetter(letter);
                positionInOctaveToContains[pitch.PositionInOctave()] = true;
            }
        }

        /// <summary>
        ///     Fills dest with a rotated version of source.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="dest">
        ///     The dest array, which must have the same length and underlying type
        ///     as source.
        /// </param>
        /// <param name="rotation">The number of elements to rotate to the left by.</param>
        private static void RotateArrayLeft(Array source, Array dest, int rotation)
        {
            if (source.Length != dest.Length) {
                throw new ArgumentException("source and dest lengths differ.");
            }
            if (rotation == 0) {
                source.CopyTo(dest, 0);
            } else {
                for (var i = 0; i < source.Length; ++i) {
                    dest.SetValue(source.GetValue((rotation + i) % source.Length), i);
                }
            }
        }
    }
}