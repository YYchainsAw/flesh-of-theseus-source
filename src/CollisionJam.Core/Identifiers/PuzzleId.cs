using System;

namespace CollisionJam.Core
{
    /// <summary>
    /// An immutable puzzle identifier. Values are compared ordinally and are not trimmed.
    /// A valid value does not imply that a puzzle rule is registered.
    /// The default value is invalid.
    /// </summary>
    public readonly struct PuzzleId : IEquatable<PuzzleId>
    {
        private readonly string value;

        public PuzzleId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A puzzle ID must contain a non-whitespace value.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(PuzzleId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is PuzzleId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PuzzleId left, PuzzleId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PuzzleId left, PuzzleId right)
        {
            return !left.Equals(right);
        }
    }
}
