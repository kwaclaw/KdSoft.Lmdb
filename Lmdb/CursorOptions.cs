namespace KdSoft.Lmdb
{
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
