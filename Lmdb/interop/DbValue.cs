using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace KdSoft.Lmdb.Interop
{
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
        public readonly IntPtr Size;
        public readonly void* Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, long size) {
            this.Data = data;
            this.Size = (IntPtr)size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, int size) {
            this.Data = data;
            this.Size = (IntPtr)size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, ulong size) {
            this.Data = data;
            this.Size = (IntPtr)size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbValue(void* data, uint size) {
            this.Data = data;
            this.Size = (IntPtr)size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToReadOnlySpan() {
            return new ReadOnlySpan<byte>(Data, unchecked((int)Size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ToSpan() {
            return new Span<byte>(Data, unchecked((int)Size));
        }

        public static bool Equals(DbValue x, DbValue y) {
            return x.Data == y.Data && x.Size == y.Size;
        }

        public static bool operator ==(DbValue left, DbValue right) {
            return Equals(left, right);
        }

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
