using System;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Flags to open a database with.
    /// </summary>
    [Flags]
    public enum DatabaseFlags : uint
    {
        /// <summary>
        /// No special options.
        /// </summary>
        None = 0,

        /// <summary>
        /// MDB_REVERSEKEY. Keys are strings to be compared in reverse order, from the end of the strings to the beginning.
        /// By default, Keys are treated as strings and compared from beginning to end.
        /// </summary>
        ReverseKey = DbConst.MDB_REVERSEKEY,

        /// <summary>
        /// MDB_DUPSORT. Duplicate keys may be used in the database.
        /// (Or, from another perspective, keys may have multiple data items, stored in sorted order.)
        /// By default keys must be unique and may have only a single data item.
        /// </summary>
        DuplicatesSort = DbConst.MDB_DUPSORT,

        /// <summary>
        /// MDB_INTEGERKEY. Keys are binary integers in native byte order. 
        /// Setting this option requires all keys to be the same size, typically sizeof(int) or sizeof(size_t).
        /// </summary>
        IntegerKey = DbConst.MDB_INTEGERKEY,

        /// <summary>
        /// MDB_DUPFIXED. This flag may only be used in combination with MDB_DUPSORT.
        /// This option tells the library that the data items for this database are all the same size, which allows further optimizations in storage and retrieval.
        /// When all data items are the same size, the MDB_GET_MULTIPLE and MDB_NEXT_MULTIPLE cursor operations may be used to retrieve multiple items at once.
        /// </summary>
        DuplicatesFixed = DbConst.MDB_DUPFIXED,

        /// <summary>
        /// MDB_INTEGERDUP. This option specifies that duplicate data items are also integers, and should be sorted as such.
        /// </summary>
        IntegerDuplicates = DbConst.MDB_INTEGERDUP,

        /// <summary>
        /// MDB_REVERSEDUP. This option specifies that duplicate data items should be compared as strings in reverse order.
        /// </summary>
        ReverseDuplicates = DbConst.MDB_REVERSEDUP,

        /// <summary>
        /// Create the named database if it doesn't exist. This option is not allowed in a read-only transaction or a read-only environment.
        /// </summary>
        Create = DbConst.MDB_CREATE
    }

    /// <summary>
    /// Special options for put operation.
    /// </summary>
    [Flags]
    public enum PutOptions: uint
    {
        /// <summary>
        /// No special behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// For put: Don't write if the key already exists.
        /// </summary>
        NoOverwrite = DbConst.MDB_NOOVERWRITE,

        /// <summary>
        /// Only for MDB_DUPSORT
        /// For put: don't write if the key and data pair already exist.
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        NoDuplicateData = DbConst.MDB_NODUPDATA,

        /// <summary>
        /// For put: Just reserve space for data, don't copy it. Return a pointer to the reserved space.
        /// </summary>
        ReserveSpace = DbConst.MDB_RESERVE,

        /// <summary>
        /// Data is being appended, don't split full pages.
        /// </summary>
        AppendData = DbConst.MDB_APPEND,

        /// <summary>
        /// Duplicate data is being appended, don't split full pages.
        /// </summary>
        AppendDuplicateData = DbConst.MDB_APPENDDUP
    }
}