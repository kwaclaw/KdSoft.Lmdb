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

        /// <summary>Constructor.</summary>
        public LmdbException() { }

        /// <summary>Constructor.</summary>
        public LmdbException(string message) : base(message) { }

        /// <summary>Constructor.</summary>
        public LmdbException(string message, Exception e) : base(message, e) { }

        /// <summary>Constructor.</summary>
        /// <param name="errorCode">Native error code.</param>
        public LmdbException(DbRetCode errorCode) {
            this.errorCode = errorCode;
        }

        /// <summary>Constructor.</summary>
        /// <param name="errorCode">Native error code.</param>
        public LmdbException(DbRetCode errorCode, string message) : base(message) {
            this.errorCode = errorCode;
        }

        /// <summary>Native error code.</summary>
        public DbRetCode ErrorCode {
            get { return errorCode; }
        }
    }
}
