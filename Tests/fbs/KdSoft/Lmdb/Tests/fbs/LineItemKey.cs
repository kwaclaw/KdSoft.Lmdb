// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace KdSoft.Lmdb.Tests.fbs
{

using global::System;
using global::System.Collections.Generic;
using global::Google.FlatBuffers;

public struct LineItemKey : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_12_0(); }
  public static LineItemKey GetRootAsLineItemKey(ByteBuffer _bb) { return GetRootAsLineItemKey(_bb, new LineItemKey()); }
  public static LineItemKey GetRootAsLineItemKey(ByteBuffer _bb, LineItemKey obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public LineItemKey __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string ProdCode { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetProdCodeBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
  public ArraySegment<byte>? GetProdCodeBytes() { return __p.__vector_as_arraysegment(4); }
#endif
  public byte[] GetProdCodeArray() { return __p.__vector_as_array<byte>(4); }
  public int OrderId { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : (int)0; } }

  public static Offset<KdSoft.Lmdb.Tests.fbs.LineItemKey> CreateLineItemKey(FlatBufferBuilder builder,
      StringOffset prod_codeOffset = default(StringOffset),
      int order_id = 0) {
    builder.StartTable(2);
    LineItemKey.AddOrderId(builder, order_id);
    LineItemKey.AddProdCode(builder, prod_codeOffset);
    return LineItemKey.EndLineItemKey(builder);
  }

  public static void StartLineItemKey(FlatBufferBuilder builder) { builder.StartTable(2); }
  public static void AddProdCode(FlatBufferBuilder builder, StringOffset prodCodeOffset) { builder.AddOffset(0, prodCodeOffset.Value, 0); }
  public static void AddOrderId(FlatBufferBuilder builder, int orderId) { builder.AddInt(1, orderId, 0); }
  public static Offset<KdSoft.Lmdb.Tests.fbs.LineItemKey> EndLineItemKey(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<KdSoft.Lmdb.Tests.fbs.LineItemKey>(o);
  }
  public static void FinishLineItemKeyBuffer(FlatBufferBuilder builder, Offset<KdSoft.Lmdb.Tests.fbs.LineItemKey> offset) { builder.Finish(offset.Value); }
  public static void FinishSizePrefixedLineItemKeyBuffer(FlatBufferBuilder builder, Offset<KdSoft.Lmdb.Tests.fbs.LineItemKey> offset) { builder.FinishSizePrefixed(offset.Value); }
};


}