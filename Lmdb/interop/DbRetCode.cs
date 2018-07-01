namespace KdSoft.Lmdb.Interop
{
    /// <summary>General LMDB API return code.</summary>
    /// <remarks>Also includes framework specific custom codes such as those returned from a call-back.</remarks>
    public enum DbRetCode
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        // BerkeleyDB uses -30800 to -30999, we'll go under them

        /// <summary> key/data pair already exists </summary>
        KEYEXIST = -30799,

        /// <summary> key/data pair not found (EOF) </summary>
        NOTFOUND = -30798,

        /// <summary> Requested page not found - this usually indicates corruption </summary>
        PAGE_NOTFOUND = -30797,

        /// <summary> Located page was wrong type </summary>
        CORRUPTED = -30796,

        /// <summary> Update of meta page failed or environment had fatal error </summary>
        PANIC = -30795,

        /// <summary> Environment version mismatch </summary>
        VERSION_MISMATCH = -30794,

        /// <summary> File is not a valid LMDB file </summary>
        INVALID = -30793,

        /// <summary> Environment mapsize reached </summary>
        MAP_FULL = -30792,

        /// <summary> Environment maxdbs reached </summary>
        DBS_FULL = -30791,

        /// <summary> Environment maxreaders reached </summary>
        READERS_FULL = -30790,

        /// <summary> Too many TLS keys in use - Windows only </summary>
        TLS_FULL = -30789,

        /// <summary> Txn has too many dirty pages </summary>
        TXN_FULL = -30788,

        /// <summary> Cursor stack too deep - internal error </summary>
        CURSOR_FULL = -30787,

        /// <summary> Page has not enough space - internal error </summary>
        PAGE_FULL = -30786,

        /// <summary> Database contents grew beyond environment mapsize </summary>
        MAP_RESIZED = -30785,

        /** <summary> Operation and DB incompatible, or DB type changed. This can mean:
         *	<ul>
         *	<li>The operation expects an #MDB_DUPSORT / #MDB_DUPFIXED database.</li>
         *	<li>Opening a named DB when the unnamed DB has #MDB_DUPSORT / #MDB_INTEGERKEY.</li>
         *	<li>Accessing a data record as a database, or vice versa.</li>
         *	<li>The database was dropped and recreated with different flags.</li>
         *	</ul>
         *	</summary>
         */
        INCOMPATIBLE = -30784,

        /// <summary> Invalid reuse of reader locktable slot </summary>
        BAD_RSLOT = -30783,

        /// <summary> Transaction must abort, has a child, or is invalid </summary>
        BAD_TXN = -30782,

        /// <summary> Unsupported size of key/DB name/data, or wrong DUPFIXED size </summary>
        BAD_VALSIZE = -30781,

        /// <summary> The specified DBI was changed unexpectedly </summary>
        BAD_DBI = -30780,

        /// <summary> The last defined error code </summary>
        LAST_ERRCODE = BAD_DBI,

        /// <summary> Successful result </summary>
        SUCCESS = 0,

        /* Error Codes defined in C runtime (errno.h) */
        EPERM = 1,
        ENOENT = 2,
        ESRCH = 3,
        EINTR = 4,
        EIO = 5,
        ENXIO = 6,
        E2BIG = 7,
        ENOEXEC = 8,
        EBADF = 9,
        ECHILD = 10,
        EAGAIN = 11,
        ENOMEM = 12,
        EACCES = 13,
        EFAULT = 14,
        EBUSY = 16,
        EEXIST = 17,
        EXDEV = 18,
        ENODEV = 19,
        ENOTDIR = 20,
        EISDIR = 21,
        ENFILE = 23,
        EMFILE = 24,
        ENOTTY = 25,
        EFBIG = 27,
        ENOSPC = 28,
        ESPIPE = 29,
        EROFS = 30,
        EMLINK = 31,
        EPIPE = 32,
        EDOM = 33,
        EDEADLK = 36,
        ENAMETOOLONG = 38,
        ENOLCK = 39,
        ENOSYS = 40,
        ENOTEMPTY = 41,

        /* Error codes used in the Secure CRT functions */
        EINVAL = 22,
        ERANGE = 34,
        EILSEQ = 42,
        STRUNCATE = 80
#pragma warning restore CA1707 // Identifiers should not contain underscores
    };
}
