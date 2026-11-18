using System;

namespace CollisionJam.Core
{
    /// <summary>
    /// An immutable item identifier. Values are compared ordinally and are not trimmed.
    /// A valid value does not imply that an item is registered or available for pickup.
    /// The default value is invalid.
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        private readonly string value;

        public ItemId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An item ID must contain a non-whitespace value.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(ItemId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ItemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }
    }
}
