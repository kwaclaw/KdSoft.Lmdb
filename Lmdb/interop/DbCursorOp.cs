namespace KdSoft.Lmdb
{
    public enum DbCursorOp
	{
#pragma warning disable CA1707 // Identifiers should not contain underscores
        MDB_FIRST,              /* Position at first key/data item */
        MDB_FIRST_DUP,          /* Position at first data item of current key. Only for #MDB_DUPSORT */
		MDB_GET_BOTH,           /* Position at key/data pair. Only for #MDB_DUPSORT */
		MDB_GET_BOTH_RANGE,     /* position at key, nearest data. Only for #MDB_DUPSORT */
		MDB_GET_CURRENT,        /* Return key/data at current cursor position */
		MDB_GET_MULTIPLE,       /* Return key and up to a page of duplicate data items from current cursor position.
								   Move cursor to prepare for #MDB_NEXT_MULTIPLE. Only for #MDB_DUPFIXED */
		MDB_LAST,               /* Position at last key/data item */
		MDB_LAST_DUP,           /* Position at last data item of current key. Only for #MDB_DUPSORT */
		MDB_NEXT,               /* Position at next data item */
		MDB_NEXT_DUP,           /* Position at next data item of current key. Only for #MDB_DUPSORT */
		MDB_NEXT_MULTIPLE,      /* Return key and up to a page of duplicate data items from next cursor position.
                                   Move cursor to prepare for #MDB_NEXT_MULTIPLE. Only for #MDB_DUPFIXED */
		MDB_NEXT_NODUP,         /* Position at first data item of next key */
		MDB_PREV,               /* Position at previous data item */
		MDB_PREV_DUP,           /* Position at previous data item of current key. Only for #MDB_DUPSORT */
		MDB_PREV_NODUP,         /* Position at last data item of previous key */
		MDB_SET,                /* Position at specified key */
		MDB_SET_KEY,            /* Position at specified key, return key + data */
		MDB_SET_RANGE,          /* Position at first key greater than or equal to specified key. */
		MDB_PREV_MULTIPLE		/* Position at previous page and return key and up to a page of duplicate data items. Only for #MDB_DUPFIXED */
#pragma warning restore CA1707 // Identifiers should not contain underscores
	}
}
