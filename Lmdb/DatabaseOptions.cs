using System;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Flags to open a database with.
    /// </summary>
    [Flags]
    public enum DatabaseOptions
    {
        /// <summary>
        /// No special options.
        /// </summary>
        None = 0,

        /// <summary>
        /// MDB_REVERSEKEY. Keys are strings to be compared in reverse order, from the end of the strings to the beginning.
        /// By default, Keys are treated as strings and compared from beginning to end.
        /// </summary>
        ReverseKey = (int)LibConstants.MDB_REVERSEKEY,

        /// <summary>
        /// MDB_INTEGERKEY. Keys are binary integers in native byte order. 
        /// Setting this option requires all keys to be the same size, typically sizeof(int) or sizeof(size_t).
        /// </summary>
        IntegerKey = (int)LibConstants.MDB_INTEGERKEY,

        /// <summary>
        /// Create the named database if it doesn't exist. This option is not allowed in a read-only transaction or a read-only environment.
        /// </summary>
        Create = (int)LibConstants.MDB_CREATE
    }

    /// <summary>
    /// Special options for put operation.
    /// </summary>
    [Flags]
    public enum PutOptions
    {
        /// <summary>
        /// No special behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// For put: Don't write if the key already exists.
        /// </summary>
        NoOverwrite = (int)LibConstants.MDB_NOOVERWRITE,

        /// <summary>
        /// For put: Just reserve space for data, don't copy it. Return a pointer to the reserved space.
        /// </summary>
        ReserveSpace = (int)LibConstants.MDB_RESERVE,

        /// <summary>
        /// Data is being appended, don't split full pages.
        /// </summary>
        AppendData = (int)LibConstants.MDB_APPEND,
    }
}