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

    public enum MultiValueDatabaseOptions
    {
        /// <summary>
        /// No special options. Equivalent to <see cref="DuplicatesSort"/>.
        /// </summary>
        None = (int)LibConstants.MDB_DUPSORT,

        /// <summary>
        /// MDB_DUPSORT. Duplicate keys may be used in the database.
        /// (Or, from another perspective, keys may have multiple data items, stored in sorted order.)
        /// By default keys must be unique and may have only a single data item.
        /// </summary>
        DuplicatesSort = (int)LibConstants.MDB_DUPSORT,

        /// <summary>
        /// MDB_DUPFIXED. This flag may only be used in combination with MDB_DUPSORT.
        /// This option tells the library that the data items for this database are all the same size, which allows further optimizations in storage and retrieval.
        /// When all data items are the same size, the MDB_GET_MULTIPLE and MDB_NEXT_MULTIPLE cursor operations may be used to retrieve multiple items at once.
        /// </summary>
        DuplicatesFixed = (int)LibConstants.MDB_DUPFIXED,

        /// <summary>
        /// MDB_REVERSEDUP. This option specifies that duplicate data items should be compared as strings in reverse order.
        /// </summary>
        ReverseDuplicates = (int)LibConstants.MDB_REVERSEDUP,
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

    /// <summary>
    /// Special options for put operation.
    /// </summary>
    [Flags]
    public enum MultiValuePutOptions
    {
        /// <summary>
        /// Only for MDB_DUPSORT
        /// For put: don't write if the key and data pair already exist.
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        NoDuplicateData = (int)LibConstants.MDB_NODUPDATA,

        /// <summary>
        /// Duplicate data is being appended, don't split full pages.
        /// </summary>
        AppendDuplicateData = (int)LibConstants.MDB_APPENDDUP
    }
}