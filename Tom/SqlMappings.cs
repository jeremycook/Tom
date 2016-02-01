using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public class SqlMappings
    {
        public static readonly Dictionary<Type, SqlDbType> SqlDbTypes = new Dictionary<Type, SqlDbType>
        {
            { typeof(Guid), SqlDbType.UniqueIdentifier },
            { typeof(Guid?), SqlDbType.UniqueIdentifier },

            { typeof(int), SqlDbType.Int },
            { typeof(int?), SqlDbType.Int },

            { typeof(decimal), SqlDbType.Decimal },
            { typeof(decimal?), SqlDbType.Decimal },

            { typeof(double), SqlDbType.Float },
            { typeof(double?), SqlDbType.Float },

            { typeof(DateTime), SqlDbType.DateTime2 },
            { typeof(DateTime?), SqlDbType.DateTime2 },

            { typeof(DateTimeOffset), SqlDbType.DateTimeOffset },
            { typeof(DateTimeOffset?), SqlDbType.DateTimeOffset },

            { typeof(bool), SqlDbType.Bit },
            { typeof(bool?), SqlDbType.Bit },

            { typeof(string), SqlDbType.NVarChar },

            { typeof(byte[]), SqlDbType.VarBinary },
        };

        public static readonly Dictionary<Type, string> FieldArguments = new Dictionary<Type, string>
        {
            { typeof(Guid), null },
            { typeof(Guid?), null },

            { typeof(int), null },
            { typeof(int?), null },

            { typeof(decimal), "(18, 0)" },
            { typeof(decimal?), "(18, 0)" },

            { typeof(double), null },
            { typeof(double?), null },

            { typeof(DateTime), "(7)" },
            { typeof(DateTime?), "(7)" },

            { typeof(DateTimeOffset), "(7)" },
            { typeof(DateTimeOffset?), "(7)" },

            { typeof(bool), null },
            { typeof(bool?), null },

            { typeof(string), "(100)" },

            { typeof(byte[]), null },
        };

        public static readonly Dictionary<Type, string> DefaultFieldValues = new Dictionary<Type, string>
        {
            { typeof(Guid), null },
            { typeof(Guid?), null },

            { typeof(int), "(0)" },
            { typeof(int?), null },

            { typeof(decimal), "(0)" },
            { typeof(decimal?), null },

            { typeof(double), "(0)" },
            { typeof(double?), null },

            { typeof(DateTime), null },
            { typeof(DateTime?), null },

            { typeof(DateTimeOffset), null },
            { typeof(DateTimeOffset?), null },

            { typeof(bool), "(0)" },
            { typeof(bool?), null },

            { typeof(string), "('')" },

            { typeof(byte[]), null },
        };

        public static readonly Dictionary<Type, Func<object>> NullValues = new Dictionary<Type, Func<object>>
        {
            { typeof(Guid?), () => DBNull.Value },
            { typeof(int?), () => DBNull.Value },
            { typeof(decimal?), () => DBNull.Value },
            { typeof(double?), () => DBNull.Value },
            { typeof(DateTime?), () => DBNull.Value },
            { typeof(DateTimeOffset?), () => DBNull.Value },
            { typeof(bool?), () => DBNull.Value },
            { typeof(string), () => "" },
            { typeof(byte[]), () => DBNull.Value },
        };

        public static readonly Dictionary<Type, string> InitFieldValues = new Dictionary<Type, string>
        {
            { typeof(Guid), "(newid())" },
            { typeof(Guid?), "(newid())" },

            { typeof(DateTime), "(sysutcdatetime())" },
            { typeof(DateTime?), "(sysutcdatetime())" },

            { typeof(DateTimeOffset), "(sysdatetimeoffset())" },
            { typeof(DateTimeOffset?), "(sysdatetimeoffset())" },
        };
    }
}
