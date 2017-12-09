using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Lmdb
{
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public struct DbValue
    {
        public IntPtr Size;
        public IntPtr Data;
    }
}
