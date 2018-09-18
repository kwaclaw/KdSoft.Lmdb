using System;
using System.Runtime.InteropServices;
using System.Security;

namespace KdSoft.Lmdb.Interop
{
    /// <summary>Interface to the LMDB library.</summary>
    [SuppressUnmanagedCodeSecurity]
#pragma warning disable CA1060 // Move pinvokes to native methods class
    static unsafe class DbLib
#pragma warning restore CA1060 // Move pinvokes to native methods class
    {
        // we expect DllImport to translate this to a platform-typical library name
        const string libName = "lmdb";

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
        public static extern DbRetCode mdb_env_open(IntPtr env, string path, EnvironmentOptions flags, UnixFileModes mode);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copy(IntPtr env, string path);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copyfd(IntPtr env, IntPtr fd);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copy2(IntPtr env, string path, EnvironmentCopyOptions copyFlags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_copyfd2(IntPtr env, IntPtr fd, EnvironmentCopyOptions flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_stat(IntPtr env, out Statistics stat);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_env_info(IntPtr env, out EnvironmentInfo stat);

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
        public static extern DbRetCode mdb_stat(IntPtr txn, uint dbi, out Statistics stat);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_dbi_flags(IntPtr txn, uint dbi, out uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_dbi_close(IntPtr env, uint dbi);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_drop(IntPtr txn, uint dbi, bool del);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_set_compare(IntPtr txn, uint dbi, DbLibCompareFunction cmp);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_set_dupsort(IntPtr txn, uint dbi, DbLibCompareFunction cmp);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_get(IntPtr txn, uint dbi, in DbValue key, in DbValue data);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_put(IntPtr txn, uint dbi, in DbValue key, in DbValue data, uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_del(IntPtr txn, uint dbi, in DbValue key, in DbValue data);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_del(IntPtr txn, uint dbi, in DbValue key, IntPtr data);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern int mdb_cmp(IntPtr txn, uint dbi, in DbValue x, in DbValue y);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern int mdb_dcmp(IntPtr txn, uint dbi, in DbValue x, in DbValue y);

        #endregion

        #region Cursor

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_open(IntPtr txn, uint dbi, out IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern void mdb_cursor_close(IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_renew(IntPtr txn, IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern IntPtr mdb_cursor_txn(IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern IntPtr mdb_cursor_dbi(IntPtr cursor);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_get(IntPtr cursor, in DbValue key, DbValue* data, DbCursorOp op);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_put(IntPtr cursor, in DbValue key, DbValue* data, uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_del(IntPtr cursor, uint flags);

        [DllImport(libName, CallingConvention = Compile.CallConv)]
        public static extern DbRetCode mdb_cursor_count(IntPtr cursor, out IntPtr count);

        #endregion
    }
}
