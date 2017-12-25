using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Lmdb
{
    [CLSCompliant(false)]
    public static unsafe class LibUtil
    {
        // calculates length of null-terminated byte string
        public static int ByteStrLen(byte* str) {
            byte* tmp = str;
            int len = 0;
            if (tmp != null) {
                while (*tmp != 0) tmp++;
                len = (int)(tmp - str);
            }
            return len;
        }

        // Copies null-terminated byte string into byte buffer.
        // Will resize buffer, if necessary, does nothing if ptr == null.
        public static int PtrToBuffer(byte* ptr, ref byte[] buffer) {
            int size = ByteStrLen(ptr);
            if (size > (buffer == null ? 0 : buffer.Length))
                Array.Resize<byte>(ref buffer, size);
            if (ptr != null)
                Marshal.Copy((IntPtr)ptr, buffer, 0, size);
            return size;
        }

        // Creates null-terminated UTF8 string in buffer.
        // Returns length, including null-terminator.
        // Will re-allocate buffer, if necessary.
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
            buffer[count] = 0;
            return count + 1;
        }

        // if str == null, then the buffer argument will be ignored
        public static int StrToUtf8(string str, ref byte[] buffer) {
            UTF8Encoding utf8 = new UTF8Encoding();
            return StrToUtf8(str, utf8, ref buffer);
        }

        // Converts null-terminated UTF-8 byte string to .NET string .
        // Will re-allocate buffer if necessary, buffer can be null.
        public static string Utf8PtrToString(byte* ptr, ref byte[] buffer) {
            int count = PtrToBuffer(ptr, ref buffer);
            if (count > 0)
                return new UTF8Encoding().GetString(buffer, 0, count);
            else
                return string.Empty;
        }

        public static string Utf8PtrToString(byte* ptr) {
            byte[] buffer = null;
            return Utf8PtrToString(ptr, ref buffer);
        }

        public static DateTime UnixToDateTime(IntPtr time_t) {
            return new System.DateTime(1970, 1, 1).AddSeconds((long)time_t);
        }

        public static IntPtr DateTimeToUnix(DateTime dt) {
            TimeSpan delta = dt - new System.DateTime(1970, 1, 1);
            return new IntPtr((long)delta.TotalSeconds);
        }

        // writes UInt32 value to byte array at given index, in big-endian byte order
        public static void UInt32ToBEBytes(UInt32 num, byte[] bytes, int index) {
            unchecked {
                bytes[index++] = (byte)(num >> 24);
                bytes[index++] = (byte)((num & 0x00FF0000) >> 16);
                bytes[index++] = (byte)((num & 0x0000FF00) >> 8);
                bytes[index++] = (byte)(num & 0x000000FF);
            }
        }

        // reads UInt32 value from byte array at given index, in big-endian byte order
        public static UInt32 BEBytesToUInt32(byte[] bytes, int index) {
            UInt32 result;
            unchecked {
                result = (UInt32)bytes[index++] << 24;
                result |= (UInt32)bytes[index++] << 16;
                result |= (UInt32)bytes[index++] << 8;
            }
            result |= bytes[index++];
            return result;
        }

    }
}
