namespace Tom
{
    using System.Reflection;

    /// <summary>
    /// Describes a column for <see cref="ITable"/>'s.
    /// </summary>
    public class Column
    {
        public Column(PropertyInfo prop)
        {
            Field = new Field(prop.PropertyType, prop.Name);
            if (Field.IsMapped)
            {
                var fieldSettings = Settings.Current.GetFieldSettings(prop.PropertyType);
                FieldArguments = fieldSettings.FieldArguments;
                FieldDefault = fieldSettings.FieldDefault;
            }
        }

        public Field Field { get; private set; }
        public string FieldArguments { get; private set; }
        public string FieldDefault { get; set; }

        /// <summary>
        /// Configure this column to be secure.
        /// </summary>
        public void Secure()
        {
            var fieldSettings = Settings.Current.GetFieldSettings(typeof(byte[]));
            FieldArguments = fieldSettings.FieldArguments;
            FieldDefault = fieldSettings.FieldDefault;
            Field.Secure();
        }
    }
}
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Tom.Helpers;

    /// <summary>
    /// Configure fields and execute parameterized SQL commands for a common
    /// type, <see cref="TCommandModel"/>.
    /// </summary>
    /// <typeparam name="TCommandModel"></typeparam>
    public class Command<TCommandModel>
    {
        private readonly Field[] _Fields;

        /// <summary>
        /// Construct a <see cref="Command{TCommandModel}"/>.
        /// </summary>
        public Command()
            : this(Field.CreateFieldsFromType(typeof(TCommandModel)))
        { }

        /// <summary>
        /// Construct a <see cref="Command{TCommandModel}"/>, manually
        /// setting <paramref name="fields"/>.
        /// </summary>
        /// <param name="fields"></param>
        public Command(IEnumerable<Field> fields)
        {
            _Fields = fields.ToArray();
        }

        /// <summary>
        /// Execute <paramref name="command"/> without any command models.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(SqlConnection connection, string command, SqlTransaction transaction = null)
        {
            var tx = transaction ?? connection.BeginTransaction();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = command;

                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    if (transaction == null)
                    {
                        tx.Commit();
                    }

                    return affectedRows;
                }
            }
            finally
            {
                if (transaction == null)
                {
                    tx.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="command"/> with <paramref name="commandModels"/>.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        /// <param name="commandModels"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(SqlConnection connection, string command, IEnumerable<TCommandModel> commandModels = null, SqlTransaction transaction = null)
        {
            if (commandModels == null || !commandModels.Any())
            {
                return await ExecuteAsync(connection, command, transaction);
            }

            var tx = transaction ?? connection.BeginTransaction();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    var executionParameters = FieldParameter.CreateParameters(Fields).ToArray();

                    cmd.Transaction = tx;
                    cmd.CommandText = command;
                    cmd.Parameters.AddRange(executionParameters.Select(o => o.SqlParameter).ToArray());

                    int affectedRows = 0;
                    foreach (TCommandModel commandModel in commandModels)
                    {
                        FieldParameter.UpdateSqlParameterValues(executionParameters, commandModel);
                        affectedRows += await cmd.ExecuteNonQueryAsync();
                    }
                    if (transaction == null)
                    {
                        tx.Commit();
                    }

                    return affectedRows;
                }
            }
            finally
            {
                if (transaction == null)
                {
                    tx.Dispose();
                }
            }
        }

        public async Task<IList<TCommandModel>> ListAsync(SqlConnection connection,
            string query,
            object parameterModel = null,
            int page = Settings.DefaultPage,
            int pageSize = Settings.DefaultPageSize)
        {
            string[] inUseParameters = GetInUseParameters(query);

            SqlParameter[] sqlparameters = parameterModel == null ?
                new SqlParameter[0] :
                parameterModel.GetType().GetProperties()
                    .Where(o => inUseParameters.Contains(o.Name, StringComparer.InvariantCultureIgnoreCase))
                    .Select(o => new SqlParameter(o.Name, value: o.GetValue(parameterModel)))
                    .ToArray();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.Parameters.AddRange(sqlparameters);
                if (page > 0)
                {
                    cmd.CommandText += @"
OFFSET (@ListAsyncCurrentPage - 1) ROWS
FETCH NEXT @ListAsyncPageSize ROWS ONLY";
                    cmd.Parameters.AddRange(new[]
                    {
                        new SqlParameter("ListAsyncCurrentPage", page),
                        new SqlParameter("ListAsyncPageSize", pageSize),
                    });
                }
                try
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<TCommandModel>();
                        while (await reader.ReadAsync())
                        {
                            var props = typeof(TCommandModel).GetProperties().ToDictionary(o => o.Name);

                            var model = Activator.CreateInstance<TCommandModel>();
                            foreach (var field in Fields)
                            {
                                object sqlvalue = reader[field.Name];
                                object value = field.GetObjectValue(sqlvalue);
                                props[field.Name].SetValue(model, value);
                            }
                            results.Add(model);
                        }
                        return results;
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Invalid usage of the option NEXT in the FETCH statement."))
                    {
                        ex.Data.Add("PossiblyMissingOrderBy", "Paging results requires an ORDER BY statement.");
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns mapped fields. See <see cref="UnmappedFields"/> for
        /// unmapped fields.
        /// </summary>
        public IEnumerable<Field> Fields { get { return _Fields.Where(o => o.IsMapped); } }

        /// <summary>
        /// Returns unmapped fields.
        /// </summary>
        public IEnumerable<Field> UnmappedFields { get { return _Fields.Where(o => !o.IsMapped); } }

        /// <summary>
        /// Configure a specific field.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="parameterAction"></param>
        /// <returns></returns>
        public Command<TCommandModel> ConfigureField(string fieldName, Action<Field> parameterAction)
        {
            var field = _Fields.Single(o => o.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
            parameterAction(field);
            return this;
        }

        /// <summary>
        /// Configure a specific field using a strongly-typed <paramref name="fieldSelector"/>.
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="parameterAction"></param>
        /// <returns></returns>
        public Command<TCommandModel> ConfigureField(
            Expression<Func<TCommandModel, object>> fieldSelector,
            Action<Field> parameterAction)
        {
            string name = fieldSelector.GetName();
            return ConfigureField(name, parameterAction) as Command<TCommandModel>;
        }

        /// <summary>
        /// Configure all fields, and optionally <paramref name="filter"/>
        /// which fields will be configured.
        /// </summary>
        /// <param name="parameterAction"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public Command<TCommandModel> ConfigureAllFields(Action<Field> parameterAction, Func<Field, bool> filter = null)
        {
            foreach (var param in _Fields.Where(filter ?? (o => true)))
            {
                parameterAction(param);
            }
            return this;
        }

        public string ToFieldNamesText()
        {
            return string.Join(", ", Fields.Select(o => o.Name));
        }

        public string ToParameterNamesText()
        {
            return string.Join(", ", Fields.Select(o => "@" + o.Name));
        }

        public string ToUpdateFieldsText()
        {
            return string.Join(", ", Fields.Select(o => "[" + o.Name + "] = @" + o.Name));
        }

        private static readonly Regex ParameterRegex = new Regex("@([A-Za-z0-9_]+)", RegexOptions.IgnoreCase);

        private static string[] GetInUseParameters(string query)
        {
            return ParameterRegex.Matches(query).Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
        }
    }
}
namespace Tom
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;

    public class Field
    {
        public Field(Type type, string name)
        {
            var fieldSettings = Settings.Current.GetFieldSettings(type);
            IsSecure = false;
            IsMapped = fieldSettings.IsMapped;
            IsSerialized = fieldSettings.IsSerialized;
            Type = type;
            Name = name;
            IsNullable = fieldSettings.IsNullable;
            SqlDbType = fieldSettings.SqlDbType;
            EmptyValueFactory = fieldSettings.EmptyValueFactory;
            GetObjectValue = value => DefaultGetObjectValue(this, value);
            GetSqlParameterValue = value => DefaultGetSqlParameterValue(this, value);
        }

        /// <summary>
        /// A secured field's value will be encrypted when stored in the
        /// database, and decrypted when read from the database.
        /// </summary>
        public bool IsSecure { get; private set; }

        public bool IsMapped { get; set; }
        public Type Type { get; private set; }
        public string Name { get; private set; }
        public bool IsNullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public Func<object> EmptyValueFactory { get; set; }

        /// <summary>
        /// Configure this field as secured.
        /// </summary>
        public virtual void Secure()
        {
            SqlDbType = Settings.Current.GetFieldSettings(typeof(byte[])).SqlDbType;
            IsSecure = true;
        }

        public Func<object, object> GetObjectValue { get; set; }
        public Func<object, object> GetSqlParameterValue { get; set; }
        public bool IsSerialized { get; private set; }

        public static object DefaultGetObjectValue(Field field, object sqlvalue)
        {
            var objectValue = sqlvalue == DBNull.Value ? null : sqlvalue;

            if (field.IsSecure && objectValue != null)
            {
                var decryptedBytes = Settings.Current.Encryptor.Decrypt(objectValue as byte[]);
                if (field.IsSerialized)
                {
                    objectValue = decryptedBytes;
                }
                else if (field.Type == typeof(byte[]))
                {
                    objectValue = decryptedBytes;
                }
                else if (field.Type == typeof(DateTime) || field.Type == typeof(DateTime?))
                {
                    var ticks = BitConverter.ToInt64(decryptedBytes, 0);
                    objectValue = new DateTime(ticks);
                }
                else
                {
                    string text = Encoding.UTF8.GetString(decryptedBytes);

                    if (field.Type == typeof(Guid) || field.Type == typeof(Guid?))
                    {
                        objectValue = Guid.Parse(text);
                    }
                    else if (field.Type == typeof(DateTimeOffset) || field.Type == typeof(DateTimeOffset?))
                    {
                        objectValue = DateTimeOffset.Parse(text);
                    }
                    else if (field.Type.IsGenericType && field.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        objectValue = Convert.ChangeType(text, field.Type.GetGenericArguments()[0]);
                    }
                    else
                    {
                        objectValue = Convert.ChangeType(text, field.Type);
                    }
                }
            }

            if (field.IsSerialized && objectValue != null)
            {
                switch (field.SqlDbType)
                {
                    case SqlDbType.NVarChar:
                        objectValue = JsonConvert.DeserializeObject((string)objectValue, field.Type);
                        break;
                    case SqlDbType.VarBinary:
                        byte[] data = (byte[])objectValue;
                        using (MemoryStream ms = new MemoryStream(data))
                        using (var reader = new BsonReader(ms))
                        {
                            reader.ReadRootValueAsArray =
                                typeof(IEnumerable).IsAssignableFrom(field.Type) &&
                                !typeof(IDictionary).IsAssignableFrom(field.Type) &&
                                field.Type != typeof(string);

                            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                            objectValue = jsonSerializer.Deserialize(reader, field.Type);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot deserialize from SqlDbType.{0}. Only NVarChar and VarBinary are supported.", field.SqlDbType));
                }
            }

            return objectValue;
        }

        public static object DefaultGetSqlParameterValue(Field field, object objectValue)
        {
            object parameterValue = objectValue ?? field.EmptyValueFactory();

            if (field.IsSerialized && parameterValue != null)
            {
                switch (field.SqlDbType)
                {
                    case SqlDbType.NVarChar:
                        parameterValue = JsonConvert.SerializeObject(parameterValue);
                        break;
                    case SqlDbType.VarBinary:
                        using (MemoryStream ms = new MemoryStream())
                        using (BsonWriter writer = new BsonWriter(ms))
                        {
                            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                            jsonSerializer.Serialize(writer, parameterValue);
                            parameterValue = ms.ToArray();
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot serialize to SqlDbType.{0}. Only NVarChar abd VarBinary are supported.", field.SqlDbType));
                }
            }

            if (field.IsSecure && parameterValue != null)
            {
                if (parameterValue is byte[])
                {
                    parameterValue = Settings.Current.Encryptor.Encrypt((byte[])parameterValue);
                }
                else if (parameterValue is DateTime?)
                {
                    // Ticks seems to be the only reliable way to precisely encrypt DateTime.
                    byte[] clearBytes = BitConverter.GetBytes(((DateTime?)parameterValue).Value.Ticks);
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
                else if (parameterValue is DateTimeOffset?)
                {
                    byte[] clearBytes = Encoding.UTF8.GetBytes(((DateTimeOffset?)parameterValue).Value.ToString("O"));
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
                else
                {
                    byte[] clearBytes = Encoding.UTF8.GetBytes(parameterValue.ToString());
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
            }

            return parameterValue ?? DBNull.Value;
        }

        public static IEnumerable<Field> CreateFieldsFromType(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException("modelType");
            }

            var props = modelType.GetProperties();
            var fields = new Field[props.Length];

            var i = 0;
            foreach (var prop in props)
            {
                var field = new Field(prop.PropertyType, prop.Name);

                fields[i++] = field;
            }

            return fields;
        }
    }
}
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;

    /// <summary>
    /// Creates a <see cref="System.Data.SqlClient.SqlParameter"/> based on a
    /// <see cref="Tom.Field"/>, and pairs the two together. Useful when 
    /// executing a parameterized SQL command with a list of values.
    /// </summary>
    public class FieldParameter
    {
        public FieldParameter(Field field)
        {
            Field = field;
            SqlParameter = new SqlParameter
            {
                ParameterName = field.Name,
                SqlDbType = field.SqlDbType,
                IsNullable = field.IsNullable,
            };
        }

        public Field Field { get; private set; }
        public SqlParameter SqlParameter { get; private set; }


        public static IEnumerable<FieldParameter> CreateParameters(IEnumerable<Field> fields)
        {
            return fields.Select(o => new FieldParameter(o));
        }

        public static void UpdateSqlParameterValues<TCommandModel>(IEnumerable<FieldParameter> fieldParameters, TCommandModel commandModel)
        {
            if (commandModel == null)
            {
                throw new ArgumentNullException("commandModel");
            }

            var props = typeof(TCommandModel).GetProperties().ToDictionary(o => o.Name);
            foreach (var param in fieldParameters)
            {
                var value = props[param.Field.Name].GetValue(commandModel);
                var sqlvalue = param.Field.GetSqlParameterValue(value);
                param.SqlParameter.Value = sqlvalue;
            }
        }
    }
}
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public static class Sequel
    {
        private static readonly Regex ParameterRegex = new Regex("@([A-Za-z0-9_]+)", RegexOptions.IgnoreCase);

        public static async Task<IList<TModel>> ListAsync<TModel>(this SqlConnection connection,
            string query,
            object parameterModel = null,
            int page = 0,
            int pageSize = 25)
        {
            string[] inUseParameters = GetInUseParameters(query);

            SqlParameter[] sqlparameters = parameterModel == null ?
                new SqlParameter[0] :
                parameterModel.GetType().GetProperties()
                    .Where(o => inUseParameters.Contains(o.Name, StringComparer.InvariantCultureIgnoreCase))
                    .Select(o => new SqlParameter(o.Name, value: o.GetValue(parameterModel)))
                    .ToArray();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.Parameters.AddRange(sqlparameters);
                if (page > 0)
                {
                    cmd.CommandText += @"
OFFSET (@ListAsyncCurrentPage - 1) ROWS
FETCH NEXT @ListAsyncPageSize ROWS ONLY";
                    cmd.Parameters.AddRange(new[]
                    {
                        new SqlParameter("ListAsyncCurrentPage", page),
                        new SqlParameter("ListAsyncPageSize", pageSize),
                    });
                }
                try
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var fields = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
                        var props = typeof(TModel).GetProperties().ToDictionary(o => o.Name);

                        var results = new List<TModel>();
                        while (await reader.ReadAsync())
                        {
                            var model = Activator.CreateInstance<TModel>();
                            foreach (var name in fields)
                            {
                                object value = reader[name];
                                props[name].SetValue(model, value == DBNull.Value ? null : value);
                            }
                            results.Add(model);
                        }
                        return results;
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Invalid usage of the option NEXT in the FETCH statement."))
                    {
                        ex.Data.Add("PossiblyMissingOrderBy", "Paging results requires an ORDER BY statement.");
                    }
                    throw;
                }
            }
        }

        private static string[] GetInUseParameters(string query)
        {
            return ParameterRegex.Matches(query).Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
        }

        public static async Task<int> ExecuteAsync(this SqlConnection connection,
            string command,
            SqlTransaction transaction = null)
        {
            var tx = transaction ?? connection.BeginTransaction();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = command;

                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    if (transaction == null)
                    {
                        tx.Commit();
                    }

                    return affectedRows;
                }
            }
            finally
            {
                if (transaction == null)
                {
                    tx.Dispose();
                }
            }
        }

        public static async Task<int> ExecuteAsync<TCommandModel>(this SqlConnection connection,
            string command,
            IEnumerable<TCommandModel> fieldModels = null,
            SqlTransaction transaction = null)
        {
            if (fieldModels != null && fieldModels.Any())
            {
                var tomCommand = new Command<TCommandModel>();
                return await tomCommand.ExecuteAsync(connection, command, fieldModels, transaction);
            }
            else
            {
                return await connection.ExecuteAsync(command, transaction);
            }
        }
    }
}
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Tom.Helpers;

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
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Tom.Helpers;

    public interface ITable
    {
        Type ModelType { get; }
        string TableName { get; }
        IEnumerable<Column> Columns { get; }
        IEnumerable<Column> UnmappedColumns { get; }
        IEnumerable<Column> PrimaryKey { get; }
    }

    public interface ITable<TModel> : ITable
    {
        /// <summary>
        /// Configure a <see cref="Column"/> and its <see cref="Field"/>.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="columnAction"></param>
        /// <returns></returns>
        ITable<TModel> ConfigureColumn(Expression<Func<TModel, object>> selector, Action<Column> columnAction);

        /// <summary>
        /// Configure <see cref="ITable.Columns"/> with an optional <paramref name="filter"/> .
        /// </summary>
        /// <param name="columnAction"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        ITable<TModel> ConfigureAllColumns(Action<Column> columnAction, Func<Column, bool> filter = null);

        /// <summary>
        /// List all models.
        /// </summary>
        /// <param name="orderBy"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IList<TModel>> ListAsync(string orderBy = null, int page = Settings.DefaultPage, int pageSize = Settings.DefaultPageSize);

        /// <summary>
        /// List a filtered set of models.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="parameters"></param>
        /// <param name="orderBy"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IList<TModel>> ListAsync(string where, object parameters, string orderBy = null, int page = Settings.DefaultPage, int pageSize = Settings.DefaultPageSize);

        /// <summary>
        /// Add a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task AddAsync(TModel model);

        /// <summary>
        /// Add a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        Task AddRangeAsync(IEnumerable<TModel> models);

        /// <summary>
        /// Update a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task UpdateAsync(TModel model);

        /// <summary>
        /// Update a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        Task UpdateRangeAsync(IEnumerable<TModel> models);

        /// <summary>
        /// Remove a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task RemoveAsync(TModel model);

        /// <summary>
        /// Remove a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        Task RemoveRangeAsync(IEnumerable<TModel> models);
    }

    public class Table<TModel> : ITable<TModel>
    {
        private readonly Column[] _Columns;

        public Table(TomBase tom)
        {
            Tom = tom;
            ModelType = typeof(TModel);
            TableName = ModelType.Name;
            _Columns = ModelType.GetProperties()
                .Select(p => new Column(p))
                .ToArray();
            Command = new Command<TModel>(Columns.Select(o => o.Field));

            var pk = Columns.First();
            PrimaryKey = new[] { pk };
            if (pk.Field.SqlDbType == System.Data.SqlDbType.UniqueIdentifier)
            {
                pk.FieldDefault = Settings.Current.NewDbValues[typeof(Guid)];
            }
        }

        public ITable<TModel> ConfigureColumn(Expression<Func<TModel, object>> selector, Action<Column> columnAction)
        {
            string name = selector.GetName();
            columnAction(_Columns.Single(o => o.Field.Name == name));
            return this;
        }

        public ITable<TModel> ConfigureAllColumns(Action<Column> columnAction, Func<Column, bool> filter = null)
        {
            foreach (var param in _Columns.Where(filter ?? (c => true)))
            {
                columnAction(param);
            }
            return this;
        }

        /// <summary>
        /// List all <see cref="TModel"/>s.
        /// </summary>
        /// <param name="orderBy"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IList<TModel>> ListAsync(string orderBy = null, int page = Settings.DefaultPage, int pageSize = Settings.DefaultPageSize)
        {
            using (var cx = new SqlConnection(Tom.ConnectionString))
            {
                await cx.OpenAsync();

                var results = await Command.ListAsync(cx,
                    string.Format(
                        "select {0} from dbo.[{1}] order by {2}",
                        Command.ToFieldNamesText(),
                        TableName,
                        orderBy ?? string.Join(", ", PrimaryKey.Select(o => "[" + o.Field.Name + "]"))
                    ),
                    page: page,
                    pageSize: pageSize
                );

                return results;
            };
        }

        /// <summary>
        /// List a filtered set of <see cref="TModel"/>.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="parameters"></param>
        /// <param name="orderBy"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IList<TModel>> ListAsync(string where, object parameters, string orderBy = null, int page = Settings.DefaultPage, int pageSize = Settings.DefaultPageSize)
        {
            using (var cx = new SqlConnection(Tom.ConnectionString))
            {
                await cx.OpenAsync();

                var results = await Command.ListAsync(cx,
                    string.Format(
                        "select {0} from dbo.[{1}] {2} order by {3}",
                        Command.ToFieldNamesText(),
                        TableName,
                        "where " + where,
                        orderBy ?? GetPrimaryKeySelect()
                    ),
                    parameters
                );

                return results;
            };
        }

        /// <summary>
        /// Add a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task AddAsync(TModel model)
        {
            await AddRangeAsync(new[] { model });
        }

        /// <summary>
        /// Add a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task AddRangeAsync(IEnumerable<TModel> models)
        {
            var work = await Tom.WorkAsync();
            await Command.ExecuteAsync(
                work.Connection,
                string.Format(
                    "insert into dbo.{0} ({1}) values ({2})",
                    TableName, Command.ToFieldNamesText(), Command.ToParameterNamesText()
                ),
                models,
                work.Transaction
            );
        }

        /// <summary>
        /// Update a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task UpdateAsync(TModel model)
        {
            await UpdateRangeAsync(new[] { model });
        }

        /// <summary>
        /// Update a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task UpdateRangeAsync(IEnumerable<TModel> models)
        {
            var work = await Tom.WorkAsync();
            await Command.ExecuteAsync(
                work.Connection,
                string.Format(
                    "update dbo.{0} set {1} where {2}",
                    TableName,
                    Command.ToUpdateFieldsText(),
                    GetPrimaryKeyFilter()
                ),
                models,
                work.Transaction
            );
        }

        /// <summary>
        /// Remove a <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task RemoveAsync(TModel model)
        {
            await RemoveRangeAsync(new[] { model });
        }

        /// <summary>
        /// Remove a range of <typeparamref name="TModel"/> within a transaction.
        /// Call <see cref="TomBase.Commit"/> to save changes.
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task RemoveRangeAsync(IEnumerable<TModel> models)
        {
            var work = await Tom.WorkAsync();
            await Command.ExecuteAsync(
                work.Connection,
                string.Format(
                    "delete from dbo.{0} where {1}",
                    TableName,
                    GetPrimaryKeyFilter()
                ),
                models,
                work.Transaction
            );
        }

        private string GetPrimaryKeyFilter()
        {
            return string.Join(", ", PrimaryKey.Select(o => "[" + o.Field.Name + "] = @" + o.Field.Name));
        }

        private string GetPrimaryKeySelect()
        {
            return string.Join(", ", PrimaryKey.Select(o => "[" + o.Field.Name + "]"));
        }

        public TomBase Tom { get; private set; }
        public Type ModelType { get; private set; }
        public string TableName { get; set; }
        public IEnumerable<Column> Columns { get { return _Columns.Where(o => o.Field.IsMapped); } }
        public IEnumerable<Column> UnmappedColumns { get { return _Columns.Where(o => !o.Field.IsMapped); } }
        public IEnumerable<Column> PrimaryKey { get; set; }
        public Command<TModel> Command { get; private set; }
    }
}
namespace Tom
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class TomBase : IDisposable
    {
        public TomBase(string connectionStringOrName)
        {
            ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringOrName] != null ?
                ConfigurationManager.ConnectionStrings[connectionStringOrName].ConnectionString :
                connectionStringOrName;

            Roots = CreateRoots();
            Configure();
        }

        /// <summary>
        /// Create <see cref="Roots"/> with default configurations.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ITable> CreateRoots()
        {
            var roots = new List<ITable>();

            // Step one: create roots.
            var rootProps = GetType().GetProperties()
                .Where(p => typeof(ITable).IsAssignableFrom(p.PropertyType));
            foreach (var prop in rootProps)
            {
                ITable root;
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(ITable<>))
                {
                    Type concretePropertyType = typeof(Table<>).MakeGenericType(prop.PropertyType.GetGenericArguments()[0]);
                    root = Activator.CreateInstance(concretePropertyType, args: new object[] { this }) as ITable;
                }
                else
                {
                    root = Activator.CreateInstance(prop.PropertyType, args: new object[] { this }) as ITable;
                }
                prop.SetValue(this, root);
                roots.Add(root);
            }

            // Step two: configure roots and columns based on other roots and columns.
            var modelTypes = roots.ToDictionary(o => o.ModelType);
            foreach (var root in roots)
            {
                foreach (var column in root.Columns)
                {
                    Type modelType = column.Field.Type;
                    if (column.Field.Type.IsGenericType)
                    {
                        if (column.Field.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            modelType = column.Field.Type.GetGenericArguments()[0];
                        }
                        else if (column.Field.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            modelType = column.Field.Type.GetGenericArguments()[0];
                        }
                    }
                    else
                    {
                        Type ienumOf = column.Field.Type.GetInterface("System.Collections.Generic.IEnumerable`1");
                        if (ienumOf != null)
                        {
                            modelType = ienumOf.GetGenericArguments()[0];
                        }
                    }

                    // References to a root model type are not mapped by
                    // default. Instead it is expected that there will be a
                    // foreign key of some sort.
                    //
                    // Example where ReferenceToSomeRootId will be mapped but
                    // the ReferenceToSomeRootId property will not:
                    // public class SomeRoot { Guid Id }
                    // public class OtherRoot { Guid Id, Guid ReferenceToSomeRootId, SomeRoot ReferenceToSomeRoot }
                    if (modelTypes.ContainsKey(modelType))
                    {
                        column.Field.IsMapped = false;
                    }
                }
            }

            return roots;
        }

        /// <summary>
        /// Override to configure <see cref="Roots"/>.
        /// </summary>
        protected virtual void Configure()
        {
            // Intentionally blank.
        }

        public string ConnectionString { get; private set; }
        public IEnumerable<ITable> Roots { get; private set; }
        public Work Work { get; private set; }

        /// <summary>
        /// Returns the open connection, or opens a connection and begins a
        /// <see cref="Tom.Work"/> if a connection is not open. Call <see cref="Commit"/>
        /// after performing create, update, delete operations to save changes.
        /// </summary>
        /// <returns></returns>
        public async Task<Work> WorkAsync()
        {
            if (Work == null)
            {
                Work = await Work.CreateAsync(ConnectionString);
            }

            return Work;
        }

        /// <summary>
        /// Commits the current transaction on the open connection, and then
        /// closes and disposes the transaction and connection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="WorkAsync"/> must be called prior to calling this method.
        /// </exception>
        public virtual void Commit()
        {
            if (Work == null)
            {
                throw new InvalidOperationException("No open connection. `WorkAsync` must be called prior to calling this method.");
            }

            // Commit...
            Work.Transaction.Commit();
            // and cleanup.
            Work.Dispose();
            Work = null;
        }

        /// <summary>
        /// Dispose and nulls <see cref="Work"/> and connection if the
        /// connection is not null.
        /// </summary>
        public void Dispose()
        {
            if (Work != null)
            {
                Work.Dispose();
                Work = null;
            }
        }
    }
}
namespace Tom
{
    using System;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    public class Work : IDisposable
    {
        public SqlConnection Connection { get; private set; }
        public SqlTransaction Transaction { get; private set; }

        public static async Task<Work> CreateAsync(string connectionString)
        {
            var uow = new Work();

            uow.Connection = new SqlConnection(connectionString);
            await uow.Connection.OpenAsync();
            uow.Transaction = uow.Connection.BeginTransaction();

            return uow;
        }

        public void Dispose()
        {
            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}
namespace Tom.Helpers
{
    using System;
    using System.Linq.Expressions;

    public static class ExpressionHelpers
    {
        public static string GetName<TModel>(this Expression<Func<TModel, object>> exp)
        {
            MemberExpression body = exp.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
namespace Tom.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Symmetric data encryption and decryption using <see cref="AesCryptoServiceProvider"/>.
    /// </summary>
    public class SymmetricEncryption
    {
        /// <summary>
        /// Keys can be 16, 24, or 32 bytes long.
        /// </summary>
        public static readonly int[] ValidKeyLengths = new[] { 16, 24, 32 };

        public enum KeyLength
        {
            K128 = 16,
            K192 = 24,
            K256 = 32,
        }

        /// <summary>
        /// Create a key that can be passed into the constructor, and used for
        /// encrypting and decrypting data.
        /// </summary>
        /// <param name="keyLength"></param>
        /// <returns></returns>
        public static byte[] CreateKey(KeyLength keyLength = KeyLength.K128)
        {
            using (var aes = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[(int)keyLength];
                aes.GetBytes(bytes);
                return bytes;
            }
        }

        private readonly byte[] Key;

        /// <summary>
        /// Constructs a symmetric data encryptor with <paramref name="key"/>
        /// in the format "0 255 0 ...".
        /// </summary>
        /// <param name="key">
        /// Expected format is "0 255 0 ..."
        /// </param>
        public SymmetricEncryption(string key)
            : this(key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(k => Convert.ToByte(k)).ToArray())
        { }

        /// <summary>
        /// Constructs a symmetric data encryptor with <paramref name="key"/>.
        /// </summary>
        /// <param name="key"></param>
        public SymmetricEncryption(byte[] key)
        {
            if (!ValidKeyLengths.Contains(key.Length))
            {
                throw new ArgumentException("The key byte array must contain 16, 24, or 32 bytes.", "key");
            }

            Key = key;
        }

        /// <summary>
        /// Decrypts <paramref name="encryptedData"/>.
        /// </summary>
        /// <param name="encryptedData">
        /// The first byte is the length of the IV. The next bytes are the IV.
        /// All subsequent bytes are the encrypted data.
        /// </param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            int ivLength = (int)encryptedData.First();
            byte[] iv = encryptedData.Skip(1).Take(ivLength).ToArray();
            byte[] encrypted = encryptedData.Skip(1 + ivLength).ToArray();

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = Key;
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, 0, encrypted.Length);
                    cs.FlushFinalBlock();

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts <paramref name="clearData"/> with a unique IV.
        /// </summary>
        /// <param name="clearData"></param>
        /// <returns>
        /// The first byte is the length of the IV. The next bytes are the IV.
        /// All subsequent bytes are the encrypted data.
        /// </returns>
        public byte[] Encrypt(byte[] clearData)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = Key;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearData, 0, clearData.Length);
                    cs.FlushFinalBlock();

                    byte[] result = new byte[1 + aes.IV.Length + ms.Length];
                    Array.Copy(new[] { (byte)aes.IV.Length }, result, 1);
                    Array.Copy(aes.IV, 0, result, 1, aes.IV.Length);
                    Array.Copy(ms.ToArray(), 0, result, 1 + aes.IV.Length, ms.Length);

                    return result;
                }
            }
        }
    }
}
namespace Tom.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SchemaGenie
    {
        public SchemaGenie(TomBase tomBase)
        {
            TomBase = tomBase;
        }

        public TomBase TomBase { get; private set; }

        /// <summary>
        /// Generate the schema of <see cref="TomBase"/>.
        /// </summary>
        /// <returns></returns>
        public string CreateSchema()
        {
            var sb = new StringBuilder();

            sb.AppendFormat(
@"-- Generated {0} by {1}

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
GO
", DateTimeOffset.Now, Environment.UserName);

            foreach (var root in TomBase.Roots)
            {
                sb.AppendLine(CreateTable(root));
            }

            return sb.ToString();
        }

        public string CreateTable(ITable root)
        {
            var fieldDeclarations = root.Columns.Select(o => CreateField(root, o));
            string fieldsText = string.Join(",\n    ", fieldDeclarations);

            string primaryKeyText = string.Join(", ", root.PrimaryKey.Select(o => o.Field.Name));

            string sql = string.Format(@"
CREATE TABLE [dbo].[{0}] (
    {1},
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED (
        {2}
    )
    WITH (
        PAD_INDEX = OFF, 
        STATISTICS_NORECOMPUTE = OFF, 
        IGNORE_DUP_KEY = OFF, 
        ALLOW_ROW_LOCKS = ON, 
        ALLOW_PAGE_LOCKS = ON
    ) ON [PRIMARY]
) ON [PRIMARY]
GO", root.TableName, fieldsText, primaryKeyText);
            return sql;
        }

        public string CreateField(ITable table, Column column)
        {
            string declaration = string.Format("[{0}] [{1}]{2} {3} {4}",
                column.Field.Name,
                column.Field.SqlDbType.ToString().ToLower(),
                column.FieldArguments,
                column.Field.IsNullable ? "NULL" : "NOT NULL",
                column.FieldDefault == null ? null : ("CONSTRAINT [DF_" + table.TableName + "_" + column.Field.Name + "]  DEFAULT " + column.FieldDefault));

            return declaration;
        }
    }
}
