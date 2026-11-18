using System;

namespace CollisionJam.Core
{
    /// <summary>
    /// An immutable room or close-up identifier. Values are compared ordinally and are not trimmed.
    /// A valid value does not imply that a view exists or can be reached from the current view.
    /// The default value is invalid.
    /// </summary>
    public readonly struct ViewId : IEquatable<ViewId>
    {
        private readonly string value;

        public ViewId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A view ID must contain a non-whitespace value.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(ViewId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ViewId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ViewId left, ViewId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ViewId left, ViewId right)
        {
            return !left.Equals(right);
        }
    }
}
