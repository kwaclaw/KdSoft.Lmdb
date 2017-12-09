using System;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Statistics for a database in the environment
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public struct DbStat
    {
        /// <summary>
        /// Size of a database page. This is currently the same for all databases.
        /// </summary>
        public uint PageSize;

        /// <summary>
        /// Depth (height) of the B-tree
        /// </summary>
        public uint Depth;

        /// <summary>
        /// Number of internal (non-leaf) pages
        /// </summary>
        public IntPtr BranchPages;

        /// <summary>
        /// Number of leaf pages
        /// </summary>
        public IntPtr LeafPages;

        /// <summary>
        /// Number of overflow pages
        /// </summary>
        public IntPtr OverflowPages;

        /// <summary>
        /// Number of data items
        /// </summary>
        public IntPtr Entries;
    }
}
