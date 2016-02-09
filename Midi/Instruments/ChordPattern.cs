using System;

namespace Midi.Instruments
{
    /// <summary>
    ///     Description of a chord's pattern starting at the root note.
    /// </summary>
    /// <remarks>
    ///     This class describes the ascending sequence of notes included in a chord, starting with
    ///     the root note.  It is described in terms of semitones relative to root and letters
    ///     relative to the root.  To apply it to particular tonic, pass one of these to the
    ///     constructor of <see cref="Chord" />.
    /// </remarks>
    public class ChordPattern
    {
        /// <summary>
        ///     Constructs a chord pattern.
        /// </summary>
        /// <param name="name">The name of the chord pattern.</param>
        /// <param name="abbreviation">
        ///     The abbreviation for the chord.  See the
        ///     <see cref="Abbreviation" /> property for details.
        /// </param>
        /// <param name="ascent">
        ///     Array encoding the notes in the chord.  See the
        ///     <see cref="Ascent" /> property for details.
        /// </param>
        /// <param name="letterOffsets">
        ///     Array encoding the sequence of letters in the chord.
        ///     Must be the same length as ascent.  See the <see cref="LetterOffsets" /> property for
        ///     details.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     ascent or letterOffsets is invalid, or they have
        ///     different lengths.
        /// </exception>
        /// <exception cref="ArgumentNullException">an argument is null.</exception>
        public ChordPattern(string name, string abbreviation, int[] ascent, int[] letterOffsets)
        {
            if (name == null || abbreviation == null || ascent == null || letterOffsets == null)
            {
                throw new ArgumentNullException();
            }
            if (ascent.Length != letterOffsets.Length || !IsSequenceValid(ascent) ||
                !IsSequenceValid(letterOffsets))
            {
                throw new ArgumentException();
            }
            Name = string.Copy(name);
            Abbreviation = string.Copy(abbreviation);
            Ascent = new int[ascent.Length];
            Array.Copy(ascent, Ascent, ascent.Length);
            LetterOffsets = new int[letterOffsets.Length];
            Array.Copy(letterOffsets, LetterOffsets, letterOffsets.Length);
        }

        /// <summary>
        ///     The name of the chord pattern.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Abbreviation for this chord pattern.
        /// </summary>
        /// <remarks>
        ///     This is the string used in the abbreviated name for a chord, placed immediately
        ///     after the tonic and before the slashed inversion (if there is one).  For example,
        ///     for minor chords the abbreviation is "m", as in "Am".
        /// </remarks>
        public string Abbreviation { get; }

        /// <summary>
        ///     The ascending note sequence of the chord, in semitones-above-the-root.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This sequence starts at zero (for the root) and is monotonically
        ///         increasing, each element representing a pitch in semitones above the root.
        ///     </para>
        /// </remarks>
        public int[] Ascent { get; }

        /// <summary>
        ///     The sequence of letters in the chord.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This array describes what sequence of letters appears in this chord.  Each
        ///         element is a "letter offset", a positive integer that tell you how many letters to
        ///         move up from the root for that note.  It must start at zero, representing the
        ///         letter for the root note.
        ///     </para>
        /// </remarks>
        public int[] LetterOffsets { get; }

        /// <summary>
        ///     ToString returns the pattern name.
        /// </summary>
        /// <returns>The pattern's name, such as "Major" or "Minor".</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Equality operator does value equality.
        /// </summary>
        public static bool operator ==(ChordPattern a, ChordPattern b)
        {
            return a != null && (ReferenceEquals(a, b) || a.Equals(b));
        }

        /// <summary>
        ///     Inequality operator does value inequality.
        /// </summary>
        public static bool operator !=(ChordPattern a, ChordPattern b)
        {
            return !(a == b);
        }

        /// <summary>
        ///     Value equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as ChordPattern;
            if ((object) other == null)
            {
                return false;
            }
            if (!Name.Equals(other.Name))
            {
                return false;
            }
            if (!Abbreviation.Equals(other.Abbreviation))
            {
                return false;
            }
            if (Ascent.Length != other.Ascent.Length)
            {
                return false;
            }
            for (var i = 0; i < Ascent.Length; ++i)
            {
                if (Ascent[i] != other.Ascent[i])
                {
                    return false;
                }
            }
            if (LetterOffsets.Length != other.LetterOffsets.Length)
            {
                return false;
            }
            for (var i = 0; i < LetterOffsets.Length; ++i)
            {
                if (LetterOffsets[i] != other.LetterOffsets[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            // TODO
            return 0;
        }

        /// <summary>
        ///     Returns true if sequence has at least two elements, starts at zero, and is monotonically
        ///     increasing.
        /// </summary>
        private bool IsSequenceValid(int[] sequence)
        {
            // Make sure it is non-empty and starts at zero.
            if (sequence.Length < 2 || sequence[0] != 0)
            {
                return false;
            }
            // Make sure it's monotonically increasing.
            for (var i = 1; i < sequence.Length; ++i)
            {
                if (sequence[i] <= sequence[i - 1])
                {
                    return false;
                }
            }
            return true;
        }
    }
}