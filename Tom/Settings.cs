using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tom.Internal;

namespace Tom
{
    public class Settings
    {
        public const int DefaultPage = 0;
        public const int DefaultPageSize = 25;

        private static Settings _Current;
        public static Settings Current
        {
            get
            {
                if (_Current == null)
                {
                    throw new NullReferenceException("Tom.Settings.Current is null and must be set.");
                }
                return _Current;
            }
            set { _Current = value; }
        }

        internal SymmetricEncryption Encryptor { get; private set; }

        /// <summary>
        /// <see cref="Tom.Settings"/> constructor.
        /// </summary>
        /// <param name="encryptionKey">
        /// Expected format is "0 255 0 ..." where each number is an 8-bit byte.
        /// </param>
        public Settings(string encryptionKey)
        {
            Encryptor = new SymmetricEncryption(encryptionKey);
        }

        public readonly Dictionary<Type, SqlDbType> SqlDbTypes = new Dictionary<Type, SqlDbType>
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

        public readonly Dictionary<Type, string> FieldArguments = new Dictionary<Type, string>
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

            { typeof(byte[]), "(max)" },
        };

        public readonly Dictionary<Type, string> DefaultFieldValues = new Dictionary<Type, string>
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

        public readonly Dictionary<Type, string> NewDbValues = new Dictionary<Type, string>
        {
            { typeof(Guid), "(newid())" },
            { typeof(Guid?), "(newid())" },

            { typeof(DateTime), "(sysutcdatetime())" },
            { typeof(DateTime?), "(sysutcdatetime())" },

            { typeof(DateTimeOffset), "(sysdatetimeoffset())" },
            { typeof(DateTimeOffset?), "(sysdatetimeoffset())" },
        };

        public readonly Dictionary<Type, Func<object>> EmptyValueFactories = new Dictionary<Type, Func<object>>
        {
            { typeof(Guid), () => Guid.Empty },
            { typeof(Guid?), () => DBNull.Value },
            { typeof(int), () => 0 },
            { typeof(int?), () => DBNull.Value },
            { typeof(decimal), () => 0m },
            { typeof(decimal?), () => DBNull.Value },
            { typeof(double), () => 0d },
            { typeof(double?), () => DBNull.Value },
            { typeof(DateTime), () => DateTime.MinValue },
            { typeof(DateTime?), () => DBNull.Value },
            { typeof(DateTimeOffset), () => DateTimeOffset.MinValue },
            { typeof(DateTimeOffset?), () => DBNull.Value },
            { typeof(bool), () => false },
            { typeof(bool?), () => DBNull.Value },
            { typeof(string), () => "" },
            { typeof(byte[]), () => DBNull.Value },
        };

        public readonly Dictionary<Type, Func<object>> NewValueFactories = new Dictionary<Type, Func<object>>
        {
            { typeof(Guid), () => Guid.NewGuid() },
            { typeof(Guid?), () => Guid.NewGuid() },
            { typeof(int), () => 0 },
            { typeof(int?), () => 0 },
            { typeof(decimal), () => 0m },
            { typeof(decimal?), () => 0m },
            { typeof(double), () => 0d },
            { typeof(double?), () => 0d },
            { typeof(DateTime), () => DateTime.UtcNow },
            { typeof(DateTime?), () => DateTime.UtcNow },
            { typeof(DateTimeOffset), () => DateTimeOffset.Now },
            { typeof(DateTimeOffset?), () => DateTimeOffset.Now },
            { typeof(bool), () => false },
            { typeof(bool?), () => false },
            { typeof(string), () => "" },
            { typeof(byte[]), () => DBNull.Value },
        };

        public virtual bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public virtual SqlDbType GetSqlDbTypes(Type type)
        {
            return SqlDbTypes.ContainsKey(type) ?
                SqlDbTypes[type] :
                SqlDbType.Variant;
        }

        public virtual Func<object> GetEmptyValueFactories(Type type)
        {
            return EmptyValueFactories.ContainsKey(type) ?
                EmptyValueFactories[type] :
                () => DBNull.Value;
        }

        public virtual bool IsMapped(Type type)
        {
            return SqlDbTypes.ContainsKey(type);
        }
    }
}
