using System;
using System.Runtime.InteropServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Statistics for a database in the environment
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    // This struct does not have reference type fields, so the default equality comparison
    // does not use reflection and is therefore efficient, no need to override it.
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct EnvironmentInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// Address of map, if fixed
        /// </summary>
        public readonly IntPtr MapAddr;

        /// <summary>
        /// Size of the data memory map
        /// </summary>
        public readonly IntPtr MapSize;

        /// <summary>
        /// ID of the last used page
        /// </summary>
        public readonly IntPtr LastPgNo;

        /// <summary>
        /// ID of the last committed transaction
        /// </summary>
        public readonly IntPtr LastTxnId;

        /// <summary>
        /// max reader slots in the environment
        /// </summary>
        public readonly int MaxReaders;

        /// <summary>
        /// max reader slots used in the environment
        /// </summary>
        public readonly int NumReaders;


    }
}
