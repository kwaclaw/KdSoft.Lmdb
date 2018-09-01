using System;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>Exception class representing errors returned from MLDB API calls,
    /// or inappropriate use of the .NET bindings.</summary>
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class LmdbException: Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        readonly DbRetCode errorCode;

        public LmdbException() { }

        public LmdbException(string message) : base(message) { }

        public LmdbException(string message, Exception e) : base(message, e) { }

        public LmdbException(DbRetCode errorCode) {
            this.errorCode = errorCode;
        }

        public LmdbException(DbRetCode errorCode, string message) : base(message) {
            this.errorCode = errorCode;
        }

        /// <summary>Native error code.</summary>
        public DbRetCode ErrorCode {
            get { return errorCode; }
        }
    }
}
