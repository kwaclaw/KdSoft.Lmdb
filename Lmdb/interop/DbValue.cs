using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public unsafe struct DbValue
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
        public ReadOnlySpan<byte> ToReadOnlySpan() {
            return new ReadOnlySpan<byte>(Data, unchecked((int)Size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ToSpan() {
            return new Span<byte>(Data, unchecked((int)Size));
        }
    }
}
