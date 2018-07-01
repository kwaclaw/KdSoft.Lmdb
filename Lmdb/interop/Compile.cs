using System.Runtime.InteropServices;

namespace KdSoft.Lmdb.Interop
{
    static class Compile
    {
        public const int PackSize =
#if MDB_PACK
            1;
#elif MDB_PACK2
            2;
#elif MDB_PACK4
            4;
#elif MDB_PACK8
            8;
#elif MDB_PACK16
            16;
#else
            0;
#endif

        public const CallingConvention CallConv =
#if MDB_STDCALL
            CallingConvention.StdCall;
#elif MDB_CDECL
            CallingConvention.Cdecl;
#elif MDB_WINAPI
            CallingConvention.WinApi;
#else
            CallingConvention.Cdecl;
#endif
    }
}
