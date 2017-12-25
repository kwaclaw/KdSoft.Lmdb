using System;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Statistics for a database in the environment
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public struct EnvironmentInfo
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
