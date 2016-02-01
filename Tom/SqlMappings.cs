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

            { typeof(string), SqlDbType.NVarChar },

            { typeof(byte[]), SqlDbType.VarBinary },
        };

        public static readonly Dictionary<Type, string> Arguments = new Dictionary<Type, string>
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

            { typeof(string), "(100)" },

            { typeof(byte[]), null },
        };

        public static readonly Dictionary<Type, string> DefaultValues = new Dictionary<Type, string>
        {
            { typeof(Guid), "(newid())" },
            { typeof(Guid?), null },

            { typeof(int), "(0)" },
            { typeof(int?), null },

            { typeof(decimal), "(0)" },
            { typeof(decimal?), null },

            { typeof(double), "(0)" },
            { typeof(double?), null },

            { typeof(DateTime), "(getutcdate())" },
            { typeof(DateTime?), null },

            { typeof(DateTimeOffset), "(getutcdate())" },
            { typeof(DateTimeOffset?), null },

            { typeof(string), "('')" },

            { typeof(byte[]), null },
        };

        public static readonly Dictionary<Type, string> InitValues = new Dictionary<Type, string>
        {
            { typeof(Guid), "(newid())" },
            { typeof(Guid?), "(newid())" },
        };
    }
}
