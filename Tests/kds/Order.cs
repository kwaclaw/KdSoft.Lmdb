using System;
using System.Collections.Generic;
using KdSoft.Serialization;

namespace KdSoft.Lmdb.Tests.kds
{
    public class Order
    {
        public Order () {
            LineItems = new List<LineItem>();
        }
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public List<LineItem> LineItems { get; }
    }

    public class OrderField: ReferenceField<Order>
    {
        public OrderField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

        protected override void DeserializeMembers(ReadOnlySpan<byte> source, Order instance) {
            instance.Id = Fmt.DeserializeStructDefault<int>(source);
            instance.CustomerId = Fmt.DeserializeStructDefault<int>(source);
            // we ignore itemnsIsNull here as our class does not allow a null LineItems property
            Fmt.DeserializeObjects<LineItem>(source, (item) => instance.LineItems.Add(item), out bool itemsIsNull);
        }

        protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref Order instance) {
            if (instance == null)
                instance = new Order();
        }

        protected override void SerializeValue(Span<byte> target, Order value) {
            Fmt.SerializeStruct(target, value.Id);
            Fmt.SerializeStruct(target, value.CustomerId);
            Fmt.SerializeObjects(target, value.LineItems);
        }

        protected override void SkipValue(ReadOnlySpan<byte> source) {
            if (Fmt.Skip<int>(source))
                if (Fmt.Skip<int>(source))
                    Fmt.SkipSequence<LineItem>(source);
        }
    }
}
