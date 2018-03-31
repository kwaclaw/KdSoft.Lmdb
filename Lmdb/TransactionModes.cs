using System;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Transaction open mode
    /// </summary>
    [Flags]
    public enum TransactionModes
    {
        /// <summary>
        /// Normal mode
        /// </summary>
        None = 0,

        /// <summary>
        /// MDB_RDONLY. Open the environment in read-only mode. 
        /// No write operations will be allowed. 
        /// MDB will still modify the lock file - except on read-only filesystems, where MDB does not use locks.
        /// </summary>
        ReadOnly = (int)DbLibConstants.MDB_RDONLY,
    }
}