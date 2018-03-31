namespace KdSoft.Lmdb
{
    /// <summary>General LMDB API return code.</summary>
    /// <remarks>Also includes framework specific custom codes such as those returned from a call-back.</remarks>
    public enum DbRetCode
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        // BerkeleyDB uses -30800 to -30999, we'll go under them

        /** key/data pair already exists */
        KEYEXIST = -30799,

        /** key/data pair not found (EOF) */
        NOTFOUND = -30798,

        /** Requested page not found - this usually indicates corruption */
        PAGE_NOTFOUND = -30797,

        /** Located page was wrong type */
        CORRUPTED = -30796,

        /** Update of meta page failed or environment had fatal error */
        PANIC = -30795,

        /** Environment version mismatch */
        VERSION_MISMATCH = -30794,

        /** File is not a valid LMDB file */
        INVALID = -30793,

        /** Environment mapsize reached */
        MAP_FULL = -30792,

        /** Environment maxdbs reached */
        DBS_FULL = -30791,

        /** Environment maxreaders reached */
        READERS_FULL = -30790,

        /** Too many TLS keys in use - Windows only */
        TLS_FULL = -30789,

        /** Txn has too many dirty pages */
        TXN_FULL = -30788,

        /** Cursor stack too deep - internal error */
        CURSOR_FULL = -30787,

        /** Page has not enough space - internal error */
        PAGE_FULL = -30786,

        /** Database contents grew beyond environment mapsize */
        MAP_RESIZED = -30785,

        /** Operation and DB incompatible, or DB type changed. This can mean:
         *	<ul>
         *	<li>The operation expects an #MDB_DUPSORT / #MDB_DUPFIXED database.
         *	<li>Opening a named DB when the unnamed DB has #MDB_DUPSORT / #MDB_INTEGERKEY.
         *	<li>Accessing a data record as a database, or vice versa.
         *	<li>The database was dropped and recreated with different flags.
         *	</ul>
         */
        INCOMPATIBLE = -30784,

        /** Invalid reuse of reader locktable slot */
        BAD_RSLOT = -30783,

        /** Transaction must abort, has a child, or is invalid */
        BAD_TXN = -30782,

        /** Unsupported size of key/DB name/data, or wrong DUPFIXED size */
        BAD_VALSIZE = -30781,

        /** The specified DBI was changed unexpectedly */
        BAD_DBI = -30780,

        /** The last defined error code */
        LAST_ERRCODE = BAD_DBI,

        /**	Successful result */
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
