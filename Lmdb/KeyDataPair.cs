using System;
using System.ComponentModel;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Represents a database entry/record as a stack-only data structure (ref struct).
    /// </summary>
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    public readonly ref struct KeyDataPair  // bogus warninsg: ref structs cannot implement interfaces!
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        /// <summary>Key bytes.</summary>
        public ReadOnlySpan<byte> Key { get; }


        /// <summary>Data bytes.</summary>
        public ReadOnlySpan<byte> Data { get; }

        /// <summary>Constructor.</summary>
        /// <param name="key">Key bytes.</param>
        /// <param name="data">Data bytes.</param>
        public KeyDataPair(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data) {
            this.Key = key;
            this.Data = data;
        }

        /// <summary>Constructor with empty data.</summary>
        /// <param name="key">Key bytes.</param>
        public KeyDataPair(ReadOnlySpan<byte> key) {
            this.Key = key;
            this.Data = default(ReadOnlySpan<byte>);
        }

        /// <summary>Equality comparison.</summary>
        public static bool Equals(KeyDataPair x, KeyDataPair y) {
            return x.Key == y.Key && x.Data == y.Data;
        }

        /// <summary>Equality operator.</summary>
        public static bool operator ==(KeyDataPair left, KeyDataPair right) {
            return Equals(left, right);
        }

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(KeyDataPair left, KeyDataPair right) {
            return !(left == right);
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
#pragma warning disable S3877 // Exceptions should not be thrown from unexpected methods
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

        /// <summary>
        /// This method is not supported as DbValue cannot be boxed. To compare two DbValues, use operator==.
        /// <exception cref="System.NotSupportedException">Always thrown by this method.</exception>
        /// </summary>
        [Obsolete("Equals() on KeyDataPair will always throw an exception. Use == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException("Not supported. Cannot call Equals() on KeyDataPair");

        /// <summary>
        /// This method is not supported as DbValue cannot be boxed. To compare two DbValues, use operator==.
        /// <exception cref="System.NotSupportedException">Always thrown by this method.</exception>
        /// </summary>
        [Obsolete("GetHashCode() on KeyDataPair will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException("Not supported. Cannot call GetHashCode() on KeyDataPair");

#pragma warning restore S3877 // Exceptions should not be thrown from unexpected methods
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
}
