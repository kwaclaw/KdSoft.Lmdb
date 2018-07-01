using System;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Flags to open a multi-value database with.
    /// </summary>
    [Flags]
    public enum MultiValueDatabaseOptions
    {
        /// <summary>
        /// No special options. Equivalent to <see cref="DuplicatesSort"/>.
        /// </summary>
        None = (int)DbLibConstants.MDB_DUPSORT,

        /// <summary>
        /// MDB_DUPSORT. Duplicate keys may be used in the database.
        /// (Or, from another perspective, keys may have multiple data items, stored in sorted order.)
        /// By default keys must be unique and may have only a single data item.
        /// </summary>
        DuplicatesSort = (int)DbLibConstants.MDB_DUPSORT,

        /// <summary>
        /// MDB_DUPFIXED. This flag may only be used in combination with MDB_DUPSORT.
        /// This option tells the library that the data items for this database are all the same size, which allows further optimizations in storage and retrieval.
        /// When all data items are the same size, the MDB_GET_MULTIPLE and MDB_NEXT_MULTIPLE cursor operations may be used to retrieve multiple items at once.
        /// </summary>
        DuplicatesFixed = (int)DbLibConstants.MDB_DUPFIXED,

        /// <summary>
        /// MDB_REVERSEDUP. This option specifies that duplicate data items should be compared as strings in reverse order.
        /// </summary>
        ReverseDuplicates = (int)DbLibConstants.MDB_REVERSEDUP,
    }

    /// <summary>
    /// Special options for multi-value put operation.
    /// </summary>
    [Flags]
    public enum MultiValuePutOptions
    {
        /// <summary>
        /// Only for MDB_DUPSORT
        /// For put: don't write if the key and data pair already exist.
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        NoDuplicateData = (int)DbLibConstants.MDB_NODUPDATA,

        /// <summary>
        /// Duplicate data is being appended, don't split full pages.
        /// </summary>
        AppendDuplicateData = (int)DbLibConstants.MDB_APPENDDUP
    }
}