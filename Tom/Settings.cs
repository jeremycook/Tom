using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tom.Helpers;

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

        public SymmetricEncryption Encryptor { get; private set; }

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

        public virtual FieldSettings GetFieldSettings(Type type)
        {
            return FieldSettings.ContainsKey(type) ?
                FieldSettings[type] :
                FieldSettings[typeof(object)];
        }

        public readonly Dictionary<Type, FieldSettings> FieldSettings = new Dictionary<Type, FieldSettings>
        {
            { typeof(Guid), new FieldSettings { SqlDbType = SqlDbType.UniqueIdentifier, IsNullable = false, FieldDefault = "('00000000-0000-0000-0000-000000000000')", EmptyValueFactory = () => default(Guid) } },
            { typeof(Guid?), new FieldSettings { SqlDbType = SqlDbType.UniqueIdentifier, IsNullable = true } },

            { typeof(int), new FieldSettings { SqlDbType = SqlDbType.Int, IsNullable = false, FieldDefault = "(0)", EmptyValueFactory = () => default(int) } },
            { typeof(int?), new FieldSettings { SqlDbType = SqlDbType.Int, IsNullable = true } },

            { typeof(decimal), new FieldSettings { SqlDbType = SqlDbType.Decimal, IsNullable = false, FieldArguments = "(18, 0)", FieldDefault = "(0)", EmptyValueFactory = () => default(decimal) } },
            { typeof(decimal?), new FieldSettings { SqlDbType = SqlDbType.Decimal, IsNullable = true, FieldArguments = "(18, 0)" } },

            { typeof(double), new FieldSettings { SqlDbType = SqlDbType.Float, IsNullable = false, FieldDefault = "(0)", EmptyValueFactory = () => default(double) } },
            { typeof(double?), new FieldSettings { SqlDbType = SqlDbType.Float, IsNullable = true } },

            { typeof(DateTime), new FieldSettings { SqlDbType = SqlDbType.DateTime2, IsNullable = false, FieldArguments = "(7)", FieldDefault = "('0001-01-01T00:00:00.0000000')", EmptyValueFactory = () => default(DateTime) } },
            { typeof(DateTime?), new FieldSettings { SqlDbType = SqlDbType.DateTime2, IsNullable = true, FieldArguments = "(7)" } },

            { typeof(DateTimeOffset), new FieldSettings { SqlDbType = SqlDbType.DateTimeOffset, IsNullable = false, FieldArguments = "(7)", FieldDefault = "('0001-01-01T00:00:00.0000000+00:00')", EmptyValueFactory = () => default(DateTimeOffset) } },
            { typeof(DateTimeOffset?), new FieldSettings { SqlDbType = SqlDbType.DateTimeOffset, IsNullable = true, FieldArguments = "(7)" } },

            { typeof(bool), new FieldSettings { SqlDbType = SqlDbType.Bit, IsNullable = false, FieldDefault = "(0)", EmptyValueFactory = () => default(bool) } },
            { typeof(bool?), new FieldSettings { SqlDbType = SqlDbType.Bit, IsNullable = true } },

            { typeof(string), new FieldSettings { SqlDbType = SqlDbType.NVarChar, IsNullable = true, FieldArguments = "(100)" } },

            { typeof(byte[]), new FieldSettings { SqlDbType = SqlDbType.VarBinary, IsNullable = true, FieldArguments = "(max)" } },

            { typeof(object), new FieldSettings { SqlDbType = SqlDbType.NVarChar, IsNullable = true, FieldArguments = "(max)", IsSerialized = true } },
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
    }

    public class FieldSettings
    {
        public FieldSettings()
        {
            IsMapped = true;
            IsNullable = true;
            EmptyValueFactory = () => null;
        }

        public string FieldArguments { get; set; }
        public string FieldDefault { get; set; }

        public bool IsMapped { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsNullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public Func<object> EmptyValueFactory { get; set; }
    }
}
