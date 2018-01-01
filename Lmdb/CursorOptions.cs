namespace KdSoft.Lmdb
{
    /// <summary>
    /// Cursor operation types.
    /// </summary>
    public enum CursorOperation
    {
        /// <summary>
        /// Position at first key/data item
        /// </summary>
        First = DbCursorOp.MDB_FIRST,

        /// <summary>
        /// Return key/data at current cursor position
        /// </summary>
        GetCurrent = DbCursorOp.MDB_GET_CURRENT,

        /// <summary>
        /// Position at last key/data item
        /// </summary>
        Last = DbCursorOp.MDB_LAST,

        /// <summary>
        /// Position at next data item
        /// </summary>
        Next = DbCursorOp.MDB_NEXT,

        /// <summary>
        /// Position at previous data item
        /// </summary>
        Previous = DbCursorOp.MDB_PREV,

        /// <summary>
        /// Position at specified key
        /// </summary>
        Set = DbCursorOp.MDB_SET,

        /// <summary>
        /// Position at specified key, return key + data
        /// </summary>
        SetKey = DbCursorOp.MDB_SET_KEY,

        /// <summary>
        /// Position at first key greater than or equal to specified key.
        /// </summary>
        SetRange = DbCursorOp.MDB_SET_RANGE
    }

    /// <summary>
    /// Special options for cursor put operation.
    /// </summary>
    public enum CursorPutOption
    {
        /// <summary>
        /// No special behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Overwrite the current key/data pair
        /// </summary>
        Current = (int)DbLibConstants.MDB_CURRENT,

        /// <summary>
        /// For put: Don't write if the key already exists.
        /// </summary>
        NoOverwrite = (int)DbLibConstants.MDB_NOOVERWRITE,

        /// <summary>
        /// For put: Just reserve space for data, don't copy it. Return a pointer to the reserved space.
        /// </summary>
        ReserveSpace = (int)DbLibConstants.MDB_RESERVE,

        /// <summary>
        /// Data is being appended, don't split full pages.
        /// </summary>
        AppendData = (int)DbLibConstants.MDB_APPEND,
    }

    /// <summary>
    /// Cursor delete operation options.
    /// </summary>
    public enum CursorDeleteOption
    {
        /// <summary>
        /// No special behavior
        /// </summary>
        None = 0,
    }
}
