using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace KdSoft.Lmdb.Interop
{
    /// <summary>
    /// Defines wrapper siganture for managed comparison functions that can be called from native code.
    /// </summary>
    /// <param name="x">Left <see cref="DbValue"/> to use in comparison.</param>
    /// <param name="y">Right <see cref="DbValue"/> to use in comparison.</param>
    /// <returns></returns>
    [CLSCompliant(false)]
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int DbLibCompareFunction(in DbValue x, in DbValue y);

    /// <summary>
    /// Fundamental data-exchange structure for native interop.
    /// </summary>
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    public unsafe readonly ref struct DbValue  // bogus warning: ref structs cannot implement interfaces!
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        /// <summary>Data size in bytes.</summary>
        public readonly IntPtr Size;

        /// <summary>Native data pointer</summary>
        public readonly void* Data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Pointer to value.</param>
        /// <param name="size">Size of value in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, long size) {
            this.Data = data;
            this.Size = unchecked((IntPtr)size);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Pointer to value.</param>
        /// <param name="size">Size of value in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, int size) {
            this.Data = data;
            this.Size = unchecked((IntPtr)size);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Pointer to value.</param>
        /// <param name="size">Size of value in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, ulong size) {
            this.Data = data;
            this.Size = unchecked((IntPtr)size);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Pointer to value.</param>
        /// <param name="size">Size of value in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, uint size) {
            this.Data = data;
            this.Size = unchecked((IntPtr)size);
        }

        /// <summary>
        /// Creates <see cref="DbValue"/> from <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of <c>Span</c> element.</typeparam>
        /// <param name="span"><c>Span</c> instance to create the <c>DbValue</c> from.</param>
        /// <returns>New <c>DbValue</c> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DbValue From<T>(ReadOnlySpan<T> span) where T: struct {
            return new DbValue(
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)),
                span.Length * Unsafe.SizeOf<T>()
            );
        }

        /// <summary>
        /// Creates <see cref="DbValue"/> from <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of <c>Span</c> element.</typeparam>
        /// <param name="span"><c>Span</c> instance to create the <c>DbValue</c> from.</param>
        /// <returns>New <c>DbValue</c> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DbValue From<T>(Span<T> span) where T: struct {
            return new DbValue(
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)),
                span.Length * Unsafe.SizeOf<T>()
            );
        }

        /// <summary>
        /// Creates <see cref="DbValue"/> from <see cref="ReadOnlySpan{T}">ReadOnlySpan&lt;byte></see>..
        /// </summary>
        /// <param name="span"><c>Span</c> instance to create the <c>DbValue</c> from.</param>
        /// <returns>New <c>DbValue</c> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DbValue From(ReadOnlySpan<byte> span) {
            return new DbValue(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);
        }

        /// <summary>
        /// Creates <see cref="DbValue"/> from <see cref="Span{T}">Span&lt;byte></see>.
        /// </summary>
        /// <param name="span"><c>Span</c> instance to create the <c>DbValue</c> from.</param>
        /// <returns>New <c>DbValue</c> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DbValue From(Span<byte> span) {
            return new DbValue(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);
        }

        /// <summary>
        /// Converts to <see cref="ReadOnlySpan{T}">ReadOnlySpan&lt;byte></see>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToReadOnlySpan() {
            return new ReadOnlySpan<byte>(Data, unchecked((int)Size));
        }

        /// <summary>
        /// Converts to <see cref="Span{T}">Span&lt;byte></see>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ToSpan() {
            return new Span<byte>(Data, unchecked((int)Size));
        }

        /// <summary>Equality comparison.</summary>
        public static bool Equals(DbValue x, DbValue y) {
            return x.Data == y.Data && x.Size == y.Size;
        }

        /// <summary>Equality operator.</summary>
        public static bool operator ==(DbValue left, DbValue right) {
            return Equals(left, right);
        }

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(DbValue left, DbValue right) {
            return !(left == right);
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
#pragma warning disable S3877 // Exceptions should not be thrown from unexpected methods
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        /// <summary>
        /// This method is not supported as DbValue cannot be boxed. To compare two DbValues, use operator==.
        /// <exception cref="System.NotSupportedException">Always thrown by this method.</exception>
        /// </summary>
        [Obsolete("Equals() on DbValue will always throw an exception. Use == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException("Not supported. Cannot call Equals() on DbValue");

        /// <summary>
        /// This method is not supported as DbValue cannot be boxed. To compare two DbValues, use operator==.
        /// <exception cref="System.NotSupportedException">Always thrown by this method.</exception>
        /// </summary>
        [Obsolete("GetHashCode() on DbValue will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException("Not supported. Cannot call GetHashCode() on DbValue");

#pragma warning restore S3877 // Exceptions should not be thrown from unexpected methods
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
}
