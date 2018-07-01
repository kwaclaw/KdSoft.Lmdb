using System;

namespace KdSoft.Lmdb.Interop
{
    [CLSCompliant(false)]
    public static class DbLibConstants
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        // mdb_env	Environment Flags

        /// <summary>mmap at a fixed address (experimental)</summary>
        public const uint MDB_FIXEDMAP = 0x01;
        /// <summary>no environment directory</summary>
        public const uint MDB_NOSUBDIR = 0x4000;
        /// <summary>don't fsync after commit</summary>
        public const uint MDB_NOSYNC = 0x10000;
        /// <summary>read only</summary>
        public const uint MDB_RDONLY = 0x20000;
        /// <summary>don't fsync metapage after commit</summary>
        public const uint MDB_NOMETASYNC = 0x40000;
        /// <summary>use writable mmap</summary>
        public const uint MDB_WRITEMAP = 0x80000;
        /// <summary>use asynchronous msync when <see cref="MDB_WRITEMAP"/> is used</summary>
        public const uint MDB_MAPASYNC = 0x100000;
        /// <summary>tie reader locktable slots to #MDB_txn objects instead of to threads</summary>
        public const uint MDB_NOTLS = 0x200000;
        /// <summary>don't do any locking, caller must manage their own locks</summary>
        public const uint MDB_NOLOCK = 0x400000;
        /// <summary>don't do readahead (no effect on Windows)</summary>
        public const uint MDB_NORDAHEAD = 0x800000;
        /// <summary>don't initialize malloc'd memory before writing to datafile</summary>
        public const uint MDB_NOMEMINIT = 0x1000000;

        //	mdb_dbi_open Database Flags

        /// <summary>use reverse string keys</summary>
        public const uint MDB_REVERSEKEY = 0x02;
        /// <summary>use sorted duplicates</summary>
        public const uint MDB_DUPSORT = 0x04;
        /// <summary>
        /// numeric keys in native byte order, either unsigned int or mdb_size_t.
        /// (lmdb expects 32-bit int &lt;= size_t &lt;= 32/64-bit mdb_size_t.)
        /// The keys must all be of the same size.
        /// </summary>
        public const uint MDB_INTEGERKEY = 0x08;
        /// <summary>with <see cref="MDB_DUPSORT"/>, sorted dup items have fixed size</summary>
        public const uint MDB_DUPFIXED = 0x10;
        /// <summary>with <see cref="MDB_DUPSORT"/>, dups are <see cref="MDB_INTEGERKEY"/>-style integers</summary>
        public const uint MDB_INTEGERDUP = 0x20;
        /// <summary>with <see cref="MDB_DUPSORT"/>, use reverse string dups</summary>
        public const uint MDB_REVERSEDUP = 0x40;
        /// <summary>create DB if not already existing</summary>
        public const uint MDB_CREATE = 0x40000;

        //	mdb_put	Write Flags

        /// <summary>For put: Don't write if the key already exists.</summary>
        public const uint MDB_NOOVERWRITE = 0x10;
        /// <summary>
        /// Only for <see cref="MDB_DUPSORT"/>:<br/>
        /// For put: don't write if the key and data pair already exist.<br/>
        /// For mdb_cursor_del: remove all duplicate data items.
        /// </summary>
        public const uint MDB_NODUPDATA = 0x20;
        /// <summary>For mdb_cursor_put: overwrite the current key/data pair</summary>
        public const uint MDB_CURRENT = 0x40;
        /// <summary>For put: Just reserve space for data, don't copy it. Return a pointer to the reserved space.</summary>
        public const uint MDB_RESERVE = 0x10000;
        /// <summary>Data is being appended, don't split full pages.</summary>
        public const uint MDB_APPEND = 0x20000;
        /// <summary>Duplicate data is being appended, don't split full pages.</summary>
        public const uint MDB_APPENDDUP = 0x40000;
        /// <summary>Store multiple data items in one call. Only for <see cref="MDB_DUPFIXED"/>.</summary>
        public const uint MDB_MULTIPLE = 0x80000;

        //	mdb_copy Copy Flags

        /// <summary>Compacting copy: Omit free space from copy, and renumber all pages sequentially.</summary>
        public const uint MDB_CP_COMPACT = 0x01;
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
