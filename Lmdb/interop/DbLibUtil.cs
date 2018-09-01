using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Lmdb.Interop
{
    /// <summary>
    /// Helpers for native interop.
    /// </summary>
    [CLSCompliant(false)]
    public static unsafe class DbLibUtil
    {
        /// <summary>
        /// calculates length of null-terminated byte string
        /// </summary>
        public static int ByteStrLen(byte* str) {
            byte* tmp = str;
            int len = 0;
            if (tmp != null) {
                while (*tmp != 0)
                    tmp++;
                len = (int)(tmp - str);
            }
            return len;
        }

        /// <summary>
        /// Copies null-terminated byte string into byte buffer.
        /// Will resize buffer, if necessary, does nothing if ptr == null.
        /// </summary>
        /// <param name="bytePtr">Null terminated bye string to copy.</param>
        /// <param name="buffer">Byte buffer to copy to.</param>
        /// <returns>New size of buffer.</returns>
        public static int PtrToBuffer(byte* bytePtr, ref byte[] buffer) {
            int size = ByteStrLen(bytePtr);
            if (size > (buffer == null ? 0 : buffer.Length))
                Array.Resize<byte>(ref buffer, size);
            if (bytePtr != null)
                Marshal.Copy((IntPtr)bytePtr, buffer, 0, size);
            return size;
        }

        /// <summary>
        /// Creates null-terminated UTF8 string in buffer. Will re-allocate buffer, if necessary.
        /// If str == null then the buffer will be ignored.
        /// </summary>
        /// <param name="str">String to convert to UTF-8 encoding.</param>
        /// <param name="utf8">Configured UTF-8 encoder.</param>
        /// <param name="buffer"></param>
        /// <returns>Byte length of UTF-8 encoded string, including null-terminator</returns>
        public static int StrToUtf8(string str, UTF8Encoding utf8, ref byte[] buffer) {
            if (str == null)
                return 0;
            int count = utf8.GetMaxByteCount(str.Length);
            // increment to next 32 bit boundary, that is, by at least one and at most 4 bytes
            count = ((count >> 2) + 1) << 2;
            if (count > (buffer == null ? 0 : buffer.Length))
                Array.Resize<byte>(ref buffer, count);
            count = utf8.GetBytes(str, 0, str.Length, buffer, 0);
            // add null terminator - we have space for at least one more byte
#pragma warning disable S2259 // Null pointers should not be dereferenced
            buffer[count] = 0;
#pragma warning restore S2259 // Null pointers should not be dereferenced
            return count + 1;
        }

        /// <summary>
        /// Creates null-terminated UTF8 string in buffer. Will re-allocate buffer, if necessary.
        /// If str == null then the buffer will be ignored.
        /// </summary>
        /// <param name="str">String to convert to UTF-8 encoding.</param>
        /// <param name="buffer"></param>
        /// <returns>Byte length of UTF-8 encoded string, including null-terminator</returns>
        public static int StrToUtf8(string str, ref byte[] buffer) {
            return StrToUtf8(str, (UTF8Encoding)Encoding.UTF8, ref buffer);
        }

        /// <summary>
        /// Converts null-terminated UTF-8 byte string to .NET string .
        /// Will re-allocate buffer if necessary, buffer can be null.
        /// </summary>
        /// <param name="utf8">Pointer to a null-terminated UTF-8 string.</param>
        /// <param name="buffer">Byte buffer to use for the decoding process.</param>
        /// <returns>.NET string.</returns>
        public static string Utf8PtrToString(byte* utf8, ref byte[] buffer) {
            int count = PtrToBuffer(utf8, ref buffer);
            if (count > 0)
                return Encoding.UTF8.GetString(buffer, 0, count);
            else
                return string.Empty;
        }

        /// <summary>
        /// Converts null-terminated UTF-8 byte string to .NET string .
        /// </summary>
        /// <param name="utf8">Pointer to a null-terminated UTF-8 string.</param>
        /// <returns>.NET string.</returns>
        public static string Utf8PtrToString(byte* utf8) {
            byte[] buffer = null;
            return Utf8PtrToString(utf8, ref buffer);
        }

        public static DateTime UnixToDateTime(IntPtr unixTime) {
            return new System.DateTime(1970, 1, 1).AddSeconds((long)unixTime);
        }

        public static IntPtr DateTimeToUnix(DateTime dt) {
            TimeSpan delta = dt - new System.DateTime(1970, 1, 1);
            return new IntPtr((long)delta.TotalSeconds);
        }

        /// <summary>
        /// Writes UInt32 value to byte array at given index, in big-endian byte order.
        /// </summary>
        /// <param name="num">UInt32 value to write.</param>
        /// <param name="bytes">Byte buffer to write the value into.</param>
        /// <param name="index">Index in byte buffer to start writing at.</param>
        public static void UInt32ToBEBytes(UInt32 num, byte[] bytes, int index) {
            unchecked {
                bytes[index++] = (byte)(num >> 24);
                bytes[index++] = (byte)((num & 0x00FF0000) >> 16);
                bytes[index++] = (byte)((num & 0x0000FF00) >> 8);
                bytes[index] = (byte)(num & 0x000000FF);
            }
        }

        /// <summary>
        /// Reads UInt32 value from byte array at given index, in big-endian byte order.
        /// </summary>
        /// <param name="bytes">Byte buffer to read from.</param>
        /// <param name="index">Index in byte buffer to start reading from.</param>
        /// <returns>UInt32 value read from byte buffer.</returns>
        public static UInt32 BEBytesToUInt32(byte[] bytes, int index) {
            UInt32 result;
            unchecked {
                result = (UInt32)bytes[index++] << 24;
                result |= (UInt32)bytes[index++] << 16;
                result |= (UInt32)bytes[index++] << 8;
            }
            result |= bytes[index];
            return result;
        }
    }
}
