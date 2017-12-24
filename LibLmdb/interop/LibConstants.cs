namespace KdSoft.Lmdb
{
    public static class LibConstants
    {
        /** mdb_env	Environment Flags */

        /** mmap at a fixed address (experimental) */
        public const uint MDB_FIXEDMAP = 0x01;
        /** no environment directory */
        public const uint MDB_NOSUBDIR = 0x4000;
        /** don't fsync after commit */
        public const uint MDB_NOSYNC = 0x10000;
        /** read only */
        public const uint MDB_RDONLY = 0x20000;
        /** don't fsync metapage after commit */
        public const uint MDB_NOMETASYNC = 0x40000;
        /** use writable mmap */
        public const uint MDB_WRITEMAP = 0x80000;
        /** use asynchronous msync when #MDB_WRITEMAP is used */
        public const uint MDB_MAPASYNC = 0x100000;
        /** tie reader locktable slots to #MDB_txn objects instead of to threads */
        public const uint MDB_NOTLS = 0x200000;
        /** don't do any locking, caller must manage their own locks */
        public const uint MDB_NOLOCK = 0x400000;
        /** don't do readahead (no effect on Windows) */
        public const uint MDB_NORDAHEAD = 0x800000;
        /** don't initialize malloc'd memory before writing to datafile */
        public const uint MDB_NOMEMINIT = 0x1000000;

        /**	mdb_dbi_open Database Flags */

        /** use reverse string keys */
        public const uint MDB_REVERSEKEY = 0x02;
        /** use sorted duplicates */
        public const uint MDB_DUPSORT = 0x04;
        /** numeric keys in native byte order, either unsigned int or #mdb_size_t.
         * (lmdb expects 32-bit int <= size_t <= 32/64-bit mdb_size_t.)
         *  The keys must all be of the same size. */
        public const uint MDB_INTEGERKEY = 0x08;
        /** with #MDB_DUPSORT, sorted dup items have fixed size */
        public const uint MDB_DUPFIXED = 0x10;
        /** with #MDB_DUPSORT, dups are #MDB_INTEGERKEY-style integers */
        public const uint MDB_INTEGERDUP = 0x20;
        /** with #MDB_DUPSORT, use reverse string dups */
        public const uint MDB_REVERSEDUP = 0x40;
        /** create DB if not already existing */
        public const uint MDB_CREATE = 0x40000;

        /**	mdb_put	Write Flags */

        /** For put: Don't write if the key already exists. */
        public const uint MDB_NOOVERWRITE = 0x10;
        /** Only for #MDB_DUPSORT<br>
         * For put: don't write if the key and data pair already exist.<br>
         * For mdb_cursor_del: remove all duplicate data items.
         */
        public const uint MDB_NODUPDATA = 0x20;
        /** For mdb_cursor_put: overwrite the current key/data pair */
        public const uint MDB_CURRENT = 0x40;
        /** For put: Just reserve space for data, don't copy it. Return a
         * pointer to the reserved space.
         */
        public const uint MDB_RESERVE = 0x10000;
        /** Data is being appended, don't split full pages. */
        public const uint MDB_APPEND = 0x20000;
        /** Duplicate data is being appended, don't split full pages. */
        public const uint MDB_APPENDDUP = 0x40000;
        /** Store multiple data items in one call. Only for #MDB_DUPFIXED. */
        public const uint MDB_MULTIPLE = 0x80000;

        /**	mdb_copy Copy Flags */

        /** Compacting copy: Omit free space from copy, and renumber all pages sequentially. */
        public const uint MDB_CP_COMPACT = 0x01;
    }
}
