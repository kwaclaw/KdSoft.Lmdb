using System;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Statistics for a database in the environment
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public struct DbEnvInfo
    {
        /// <summary>
        /// Address of map, if fixed
        /// </summary>
        public IntPtr MapAddr;

        /// <summary>
        /// Size of the data memory map
        /// </summary>
        public IntPtr MapSize;

        /// <summary>
        /// ID of the last used page
        /// </summary>
        public IntPtr LastPgNo;

        /// <summary>
        /// ID of the last committed transaction
        /// </summary>
        public IntPtr LastTxnId;

        /// <summary>
        /// max reader slots in the environment
        /// </summary>
        public uint MaxReaders;

        /// <summary>
        /// max reader slots used in the environment
        /// </summary>
        public uint NumReaders;
    }
}
