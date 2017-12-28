using System;
using System.Runtime.InteropServices;
using System.Security;

namespace KdSoft.Lmdb
{
    /// <summary>Interface to the LMDB library.</summary>
    [CLSCompliant(false)]
    [SuppressUnmanagedCodeSecurity]
    public static class Lib
    {
        const string libName = "lmdb";

        [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
        public delegate int CompareFunction(in DbValue x, in DbValue y);

        [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
        public delegate void AssertFunction(IntPtr env, [MarshalAs(UnmanagedType.LPStr), In] string msg);

        [DllImport(libName, EntryPoint = "mdb_version", CallingConvention = Compile.CallConv)]
        static extern IntPtr _mdb_version(out int major, out int minor, out int patch);
        public static string mdb_version(out int major, out int minor, out int patch) {
            IntPtr verStr = _mdb_version(out major, out minor, out patch);
            return Marshal.PtrToStringAnsi(verStr);
        }

        [DllImport(libName, EntryPoint = "mdb_strerror", CallingConvention = Compile.CallConv)]
        static extern IntPtr _mdb_strerror(int err);
        public static string mdb_strerror(DbRetCode error) {
            IntPtr errStr = _mdb_strerror((int)error);
            return Marshal.PtrToStringAnsi(errStr);
        }

        #region MDB Environment

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_create(out IntPtr env);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_open(IntPtr env, string path, EnvironmentOptions flags, UnixFileMode mode);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copy(IntPtr env, string path);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copyfd(IntPtr env, IntPtr fd);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copy2(IntPtr env, string path, EnvironmentCopyOptions copyFlags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copyfd2(IntPtr env, IntPtr fd, EnvironmentCopyOptions flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_stat(IntPtr env, [MarshalAs(UnmanagedType.Struct)] out Statistics stat);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_info(IntPtr env, [MarshalAs(UnmanagedType.Struct)] out EnvironmentInfo stat);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_sync(IntPtr env, bool force);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_env_close(IntPtr env);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_flags(IntPtr env, EnvironmentOptions flags, bool onoff);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_get_flags(IntPtr env, out EnvironmentOptions flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_get_path(IntPtr env, [MarshalAs(UnmanagedType.LPStr)] out string path);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_get_fd(IntPtr env, out IntPtr fd);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_mapsize(IntPtr env, IntPtr size);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_maxreaders(IntPtr env, uint readers);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_get_maxreaders(IntPtr env, out uint readers);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_maxdbs(IntPtr env, uint dbs);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern int mdb_env_get_maxkeysize(IntPtr env);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_userctx(IntPtr env, IntPtr ctx);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern IntPtr mdb_env_get_userctx(IntPtr env);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_set_assert(IntPtr env, AssertFunction func);

        #endregion

        #region MDB Transaction

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_txn_begin(IntPtr env, IntPtr parent, TransactionModes flags, out IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern IntPtr mdb_txn_env(IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern IntPtr mdb_txn_id(IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_txn_commit(IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_txn_abort(IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_txn_reset(IntPtr txn);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_txn_renew(IntPtr txn);

        #endregion

        #region MDB Database

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_dbi_open(IntPtr txn, string name, uint flags, out uint db);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_stat(IntPtr txn, uint dbi, [MarshalAs(UnmanagedType.Struct)] out Statistics stat);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_dbi_flags(IntPtr txn, uint dbi, out uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_dbi_close(IntPtr env, uint dbi);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_drop(IntPtr txn, uint dbi, bool del);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_set_compare(IntPtr txn, uint dbi, CompareFunction cmp);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_set_dupsort(IntPtr txn, uint dbi, CompareFunction cmp);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_get(IntPtr txn, uint dbi, [MarshalAs(UnmanagedType.Struct), In] ref DbValue key, [MarshalAs(UnmanagedType.Struct)] ref DbValue data);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_put(IntPtr txn, uint dbi, [MarshalAs(UnmanagedType.Struct), In] ref DbValue key, [MarshalAs(UnmanagedType.Struct)] ref DbValue data, uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_del(IntPtr txn, uint dbi, [MarshalAs(UnmanagedType.Struct), In] ref DbValue key, [MarshalAs(UnmanagedType.Struct), In] ref DbValue data);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_del(IntPtr txn, uint dbi, [MarshalAs(UnmanagedType.Struct), In] ref DbValue key, IntPtr data);

        #endregion

        #region Cursor

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_open(IntPtr txn, uint dbi, out IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_cursor_close(IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_renew(IntPtr txn, IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_get(IntPtr cursor, [MarshalAs(UnmanagedType.Struct), In] ref DbValue key, [MarshalAs(UnmanagedType.Struct), Out] DbValue data, CursorOperation op);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_put(IntPtr cursor, [MarshalAs(UnmanagedType.Struct)] ref DbValue key, [MarshalAs(UnmanagedType.Struct), In] ref DbValue value, CursorPutOptions flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_del(IntPtr cursor, CursorDeleteOptions flags);

        #endregion
    }

    /// <summary>General LMDB API return code.</summary>
    /// <remarks>Also includes framework specific custom codes such as those returned from a call-back.</remarks>
    public enum DbRetCode
    {
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

        /** Unexpected problem - txn should abort */
        PROBLEM = -30779,

        /** The last defined error code */
        LAST_ERRCODE = PROBLEM,

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
    };
}
