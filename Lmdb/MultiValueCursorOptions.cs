namespace KdSoft.Lmdb
{
    /// <summary>
    /// Special options for cursor put operation in multi-value databases.
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value
    public enum MultiValueCursorPutOption
#pragma warning restore CA1008 // Enums should have zero value
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
        AppendDuplicateData = (int)DbLibConstants.MDB_APPENDDUP,

        /// <summary>
        /// Store multiple data items in one call. Only for MDB_DUPFIXED.
        /// </summary>
        //MultipleData = (int)LibConstants.MDB_MULTIPLE
    }

    /// <summary>
    /// Multi-value cursor delete operation options.
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value
    public enum MultiValueCursorDeleteOption
#pragma warning restore CA1008 // Enums should have zero value
    {
        /// <summary>
        /// Only for MDB_DUPSORT
        /// For put: don't write if the key and data pair already exist.
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        NoDuplicateData = (int)DbLibConstants.MDB_NODUPDATA
    }
}
