namespace KdSoft.Lmdb
{
    /// <summary>
    /// Multi-value cursor operation types.
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value
    public enum MultiValueCursorOperation
#pragma warning restore CA1008 // Enums should have zero value
    {
        /// <summary>
        /// Position at first data item of current key. Only for MDB_DUPSORT
        /// </summary>
        FirstDuplicate = DbCursorOp.MDB_FIRST_DUP,

        /// <summary>
        /// Position at key/data pair. Only for MDB_DUPSORT
        /// </summary>
        GetBoth = DbCursorOp.MDB_GET_BOTH,

        /// <summary>
        /// position at key, nearest data. Only for MDB_DUPSORT
        /// </summary>
        GetBothRange = DbCursorOp.MDB_GET_BOTH_RANGE,

        /// <summary>
        /// Return all the duplicate data items at the current cursor position. Only for MDB_DUPFIXED
        /// </summary>
        GetMultiple = DbCursorOp.MDB_GET_MULTIPLE,

        /// <summary>
        /// Position at last data item of current key. Only for MDB_DUPSORT
        /// </summary>
        LastDuplicate = DbCursorOp.MDB_LAST_DUP,

        /// <summary>
        /// Position at next data item of current key. Only for MDB_DUPSORT
        /// </summary>
        NextDuplicate = DbCursorOp.MDB_NEXT_DUP,

        /// <summary>
        /// Return all duplicate data items at the next cursor position. Only for MDB_DUPFIXED
        /// </summary>
        NextMultiple = DbCursorOp.MDB_NEXT_MULTIPLE,

        /// <summary>
        /// Position at first data item of next key. Only for MDB_DUPSORT
        /// </summary>
        NextNoDuplicate = DbCursorOp.MDB_NEXT_NODUP,

        /// <summary>
        /// Position at previous data item of current key. Only for MDB_DUPSORT
        /// </summary>
        PreviousDuplicate = DbCursorOp.MDB_PREV_DUP,

        /// <summary>
        /// Position at last data item of previous key. Only for MDB_DUPSORT
        /// </summary>
        PreviousNoDuplicate = DbCursorOp.MDB_PREV_NODUP,
    }

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
    public enum MultiValueCursorDeleteOption
    {
        /// <summary>
        /// Only for MDB_DUPSORT
        /// For put: don't write if the key and data pair already exist.
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        NoDuplicateData = (int)DbLibConstants.MDB_NODUPDATA
    }
}
