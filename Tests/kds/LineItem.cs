using System;
using KdSoft.Serialization;

namespace KdSoft.Lmdb.Tests.kds
{
    public class LineItem
    {
        public LineItemKey Key { get; set; }
        public int Quantity { get; set; }

        public override bool Equals(object obj) {
            return Equals(obj as LineItem);
        }

        public bool Equals(LineItem other) {
            return object.Equals(this.Key, other.Key) && this.Quantity == other.Quantity;
        }

        public override int GetHashCode() {
            return (Key?.GetHashCode() ?? 0) ^ Quantity.GetHashCode();
        }
    }

    public class LineItemField: ReferenceField<LineItem>
    {
        public LineItemField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

        protected override void DeserializeMembers(ReadOnlySpan<byte> source, LineItem instance) {
            instance.Key = Fmt.DeserializeObject<LineItemKey>(source);
            instance.Quantity = Fmt.DeserializeStructDefault<int>(source);
        }

        protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref LineItem instance) {
            if (instance == null)
                instance = new LineItem();
        }

        protected override void SerializeValue(Span<byte> target, LineItem value) {
            Fmt.SerializeObject(target, value.Key);
            Fmt.SerializeStruct(target, value.Quantity);
        }

        protected override void SkipValue(ReadOnlySpan<byte> source) {
            if (Fmt.Skip<LineItemKey>(source))
                Fmt.Skip<int>(source);
        }
    }
}
