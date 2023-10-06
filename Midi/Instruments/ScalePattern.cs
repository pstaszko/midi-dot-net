using System;
using System.Linq;

namespace Midi.Instruments
{
    /// <summary>
    ///     Description of a scale's pattern as it ascends through an octave.
    /// </summary>
    /// <remarks>
    ///     This class describes the general behavior of a scale as it ascends from a tonic up to
    ///     the next tonic.  It is described in terms of semitones relative to the tonic; to apply it to
    ///     a particular tonic, pass one of these to the constructor of <see cref="Scale" />.
    /// </remarks>
    public class ScalePattern
    {
        /// <summary>
        ///     Constructs a scale pattern.
        /// </summary>
        /// <param name="name">The name of the scale pattern.</param>
        /// <param name="ascent">
        ///     The ascending pattern of the scale.  See the <see cref="Ascent" />
        ///     property for a detailed description and requirements.  This parameter is copied.
        /// </param>
        /// <exception cref="ArgumentNullException">name or ascent is null.</exception>
        /// <exception cref="ArgumentException">ascent is invalid.</exception>
        public ScalePattern(string name, int[] ascent)
        {
            if (name == null || ascent == null) {
                throw new ArgumentNullException();
            }
            // Make sure ascent is valid.
            if (!AscentIsValid(ascent)) {
                throw new ArgumentException("ascent is invalid.");
            }
            Name = $"{name}";
            Ascent = new int[ascent.Length];
            Array.Copy(ascent, Ascent, ascent.Length);
        }

        /// <summary>The name of the scale being described.</summary>
        public string Name { get; }

        /// <summary>The ascent of the scale.</summary>
        /// <remarks>
        ///     <para>
        ///         The ascent is expressed as a series of integers, each giving a semitone
        ///         distance above the tonic.  It must have at least two elements, start at zero (the
        ///         tonic), be monotonically increasing, and stay below 12 (the next tonic above).
        ///     </para>
        ///     <para>
        ///         The number of elements in the ascent tells us how many notes-per-octave in the
        ///         scale.  For example, a heptatonic scale will always have seven elements in the ascent.
        ///     </para>
        /// </remarks>
        public int[] Ascent { get; }

        /// <summary>
        ///     ToString returns the pattern name.
        /// </summary>
        /// <returns>The pattern's name, such as "Major" or "Melodic Minor (ascending)".</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Equality operator does value equality.
        /// </summary>
        public static bool operator ==(ScalePattern a, ScalePattern b)
        {
            return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals((object)b);
        }

        /// <summary>
        ///     Inequality operator does value inequality.
        /// </summary>
        public static bool operator !=(ScalePattern a, ScalePattern b)
        {
            return !(a == b);
        }

        /// <summary>
        ///     Value equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ScalePattern)obj);
        }

        /// <summary>
        ///     Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked {
                return ((Name?.GetHashCode() ?? 0) * 397) ^ (Ascent?.GetHashCode() ?? 0);
            }
        }

        protected bool Equals(ScalePattern other)
        {
            return string.Equals(Name, other.Name)
                && Ascent.SequenceEqual(other.Ascent);
        }

        /// <summary>Returns true if ascent is valid.</summary>
        private bool AscentIsValid(int[] ascent)
        {
            // Make sure it is non-empty, starts at zero, and ends before 12.
            if (ascent.Length < 2 || ascent[0] != 0 || ascent[ascent.Length - 1] >= 12) {
                return false;
            }
            // Make sure it's monotonically increasing.
            for (var i = 1; i < ascent.Length; ++i) {
                if (ascent[i] <= ascent[i - 1]) {
                    return false;
                }
            }
            return true;
        }
    }
}