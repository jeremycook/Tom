using System;
using System.Collections.Generic;

namespace Tests.Models
{
    public class Secure
    {
        public Guid Id { get; set; }

        public Guid Guid { get; set; }
        public Guid? GuidNull { get; set; }

        public decimal Decimal { get; set; }
        public decimal? DecimalNull { get; set; }

        public double Float { get; set; }
        public double? FloatNull { get; set; }

        public int Int { get; set; }
        public int? IntNull { get; set; }

        public DateTime DateTime2 { get; set; }
        public DateTime? DateTime2Null { get; set; }

        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTimeOffset? DateTimeOffsetNull { get; set; }

        public bool Bit { get; set; }
        public bool? BitNull { get; set; }

        public string Nvarchar { get; set; }

        public byte[] Varbinary { get; set; }

        public SecureFi Fi { get; set; }
        public IEnumerable<SecureFee> Fees { get; set; }

        public Guid? BarId { get; set; }
        public Bar Bar { get; set; }

        public IEnumerable<Guid> BarIds { get; set; }
        public IEnumerable<Bar> Bars { get; set; }
    }

    public class SecureFi
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class SecureFee
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
