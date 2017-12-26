using System;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public unsafe struct DbValue
    {
        public DbValue(void* data, long size) {
            this.Size = (IntPtr)size;
            this.Data = data;
        }
        public DbValue(void* data, int size) {
            this.Size = (IntPtr)size;
            this.Data = data;
        }

        public readonly IntPtr Size;
        public readonly void* Data;

        public ReadOnlySpan<byte> ToReadOnlySpan() {
            return new ReadOnlySpan<byte>(Data, unchecked((int)Size));
        }

        public Span<byte> ToSpan() {
            return new Span<byte>(Data, unchecked((int)Size));
        }
    }
}
