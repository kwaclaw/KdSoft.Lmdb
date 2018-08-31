namespace KdSoft.Lmdb.Interop
{
    /// <summary>
    /// Cursor operation types.
    /// </summary>
    public enum DbCursorOp
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary>Position at first key/data item</summary>
        MDB_FIRST,
        /// <summary> Position at first data item of current key. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_FIRST_DUP,
        /// <summary> Position at key/data pair. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_GET_BOTH,
        /// <summary> position at key, nearest data. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_GET_BOTH_RANGE,
        /// <summary> Return key/data at current cursor position</summary>
        MDB_GET_CURRENT,
        /// <summary>
        /// Return up to a page of duplicate data items from current cursor position.
        /// Move cursor to prepare for <see cref="DbLibConstants.MDB_NEXT_MULTIPLE"/>.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        MDB_GET_MULTIPLE,
        /// <summary> Position at last key/data item </summary>
        MDB_LAST,
        /// <summary> Position at last data item of current key. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_LAST_DUP,
        /// <summary> Position at next data item </summary>
        MDB_NEXT,
        /// <summary> Position at next data item of current key. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_NEXT_DUP,
        /// <summary>
        /// Return up to a page of duplicate data items from next cursor position.
        /// Move cursor to prepare for <see cref="DbLibConstants.MDB_NEXT_MULTIPLE"/>.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        MDB_NEXT_MULTIPLE,
        /// <summary> Position at first data item of next key </summary>
        MDB_NEXT_NODUP,
        /// <summary> Position at previous data item </summary>
        MDB_PREV,
        /// <summary> Position at previous data item of current key. Only for <see cref="DbLibConstants.MDB_DUPSORT"/>. </summary>
        MDB_PREV_DUP,
        /// <summary> Position at last data item of previous key </summary>
        MDB_PREV_NODUP,
        /// <summary> Position at specified key </summary>
        MDB_SET,
        /// <summary> Position at specified key, return key + data </summary>
        MDB_SET_KEY,
        /// <summary> Position at first key greater than or equal to specified key. </summary>
        MDB_SET_RANGE,
        /// <summary>
        /// Position at previous page and return up to a page of duplicate data items.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        MDB_PREV_MULTIPLE
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
