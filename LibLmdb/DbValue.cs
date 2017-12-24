using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Lmdb
{
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public unsafe struct DbValue
    {
        public DbValue(long size, byte* data) {
            this.Size = (IntPtr)size;
            this.Data = (IntPtr)data;
        }

        public readonly IntPtr Size;
        public readonly IntPtr Data;
    }
}
