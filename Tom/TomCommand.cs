using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tom.Internal;

namespace Tom
{
    public class TomCommand
    {
        private readonly TomParameter[] _Parameters;

        public static IEnumerable<TomParameter> CreateParameters(Type parameterModelType)
        {
            var props = parameterModelType.GetProperties();
            var parameters = new TomParameter[props.Length];

            var i = 0;
            foreach (var prop in props)
            {
                var param = new TomParameter
                {
                    FieldName = prop.Name,
                    SqlDbType = SqlMappings.SqlDbTypes[prop.PropertyType],
                    IsNullable = prop.PropertyType.IsGenericType &&
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>),
                    EmptyValueFactory = SqlMappings.EmptyValueFactories[prop.PropertyType],
                };

                parameters[i++] = param;
            }

            return parameters;
        }

        public TomCommand(Type parameterModelType)
            : this(CreateParameters(parameterModelType))
        { }

        public TomCommand(IEnumerable<TomParameter> parameters)
        {
            _Parameters = parameters.ToArray();
        }

        public TomCommand ConfigureAllParameters(Action<TomParameter> parameterAction, Func<TomParameter, bool> filter = null)
        {
            foreach (var param in Parameters.Where(filter ?? (c => true)))
            {
                parameterAction(param);
            }
            return this;
        }

        public TomCommand ConfigureParameter(string fieldName, Action<TomParameter> parameterAction)
        {
            var parameter = Parameters.Single(o => o.FieldName == fieldName);
            parameterAction(parameter);
            return this;
        }

        public async Task<int> ExecuteAsync(SqlConnection connection, string command, IEnumerable<object> parameterModels = null, SqlTransaction transaction = null)
        {
            if (parameterModels == null || !parameterModels.Any())
            {
                return await connection.ExecuteAsync(command, transaction);
            }

            var tx = transaction ?? connection.BeginTransaction();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    var executionParameters = ExecutionParameter.CreateParameters(Parameters).ToArray();

                    cmd.Transaction = tx;
                    cmd.CommandText = command;
                    cmd.Parameters.AddRange(executionParameters.Select(o => o.SqlParameter).ToArray());

                    int affectedRows = 0;
                    foreach (var parameterModel in parameterModels)
                    {
                        ExecutionParameter.UpdateSqlParameterValues(executionParameters, parameterModel);
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

        /// <summary>
        /// Returns mapped parameters. See <see cref="UnmappedParameters"/> for
        /// unmapped parameters.
        /// </summary>
        public IEnumerable<TomParameter> Parameters { get { return _Parameters.Where(o => o.IsMapped); } }

        /// <summary>
        /// Returns unmapped parameters.
        /// </summary>
        public IEnumerable<TomParameter> UnmappedParameters { get { return _Parameters.Where(o => !o.IsMapped); } }

        public string ToFieldNamesText()
        {
            return string.Join(", ", Parameters.Select(o => o.FieldName));
        }

        public string ToParameterNamesText()
        {
            return string.Join(", ", Parameters.Select(o => "@" + o.FieldName));
        }

        public string ToUpdateFieldsText()
        {
            return string.Join(", ", Parameters.Select(o => "[" + o.FieldName + "] = @" + o.FieldName));
        }
    }

    public class TomCommand<TParameterModel> : TomCommand
    {
        public TomCommand()
            : base(CreateParameters(typeof(TParameterModel)))
        { }

        public TomCommand(IEnumerable<TomParameter> parameters)
            : base(parameters)
        { }

        public TomCommand<TParameterModel> ConfigureParameter(
            Expression<Func<TParameterModel, object>> selector,
            Action<TomParameter> parameterAction)
        {
            string name = selector.GetName();
            return ConfigureParameter(name, parameterAction) as TomCommand<TParameterModel>;
        }

        public new TomCommand<TParameterModel> ConfigureAllParameters(Action<TomParameter> parameterAction, Func<TomParameter, bool> filter = null)
        {
            foreach (var param in Parameters.Where(filter ?? (o => true)))
            {
                parameterAction(param);
            }
            return this;
        }

        public async Task<int> ExecuteAsync(SqlConnection connection, string command, IEnumerable<TParameterModel> parameterModels = null, SqlTransaction transaction = null)
        {
            return await ExecuteAsync(connection, command, parameterModels as IEnumerable<object>, transaction);
        }
    }

    public class TomParameter
    {
        private SqlDbType _SqlDbType;
        private Func<object> _EmptyValueFactory;

        public TomParameter()
        {
            IsMapped = true;
        }

        public string FieldName { get; set; }
        public bool IsNullable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="SqlDbType" /> cannot be set when <see cref="IsSecure"/> is true.
        /// </exception>
        public SqlDbType SqlDbType
        {
            get { return _SqlDbType; }
            set
            {
                if (IsSecure)
                {
                    throw new InvalidOperationException("SqlDbType cannot be set when IsSecure is true.");
                }
                _SqlDbType = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="EmptyValueFactory"/> cannot be set when <see cref="IsSecure"/> is true.
        /// </exception>
        public Func<object> EmptyValueFactory
        {
            get { return _EmptyValueFactory; }
            set
            {
                if (IsSecure)
                {
                    throw new InvalidOperationException("EmptyValueFactory cannot be set when IsSecure is true.");
                }
                _EmptyValueFactory = value;
            }
        }

        /// <summary>
        /// A secure parameter's value will be encrypted when stored in the
        /// database, and decrypted when read from the database.
        /// </summary>
        public bool IsSecure { get; private set; }

        public bool IsMapped { get; set; }

        /// <summary>
        /// Configure this parameter to be secure.
        /// </summary>
        public virtual void Secure()
        {
            SqlDbType = SqlMappings.SqlDbTypes[typeof(byte[])];
            EmptyValueFactory = SqlMappings.EmptyValueFactories[typeof(byte[])];
            IsSecure = true;
        }

        public virtual object GetDbValue(object value)
        {
            if (value == null)
            {
                return EmptyValueFactory();
            }
            else if (IsSecure)
            {
                // TODO: Actually encrypt value.
                return Encoding.UTF8.GetBytes(value.ToString());
            }
            else
            {
                return value;
            }
        }
    }

    public class ExecutionParameter
    {
        public ExecutionParameter(TomParameter tomParameter)
        {
            TomParameter = tomParameter;
            SqlParameter = new SqlParameter
            {
                ParameterName = tomParameter.FieldName,
                SqlDbType = tomParameter.SqlDbType,
                IsNullable = tomParameter.IsNullable,
            };
        }

        public SqlParameter SqlParameter { get; private set; }
        public TomParameter TomParameter { get; private set; }


        public static IEnumerable<ExecutionParameter> CreateParameters(IEnumerable<TomParameter> tomParameters)
        {
            return tomParameters.Select(o => new ExecutionParameter(o));
        }

        public static void UpdateSqlParameterValues(IEnumerable<ExecutionParameter> executionParameters, object parameterModel)
        {
            if (parameterModel == null)
            {
                throw new ArgumentNullException("parameterModel");
            }

            if (parameterModel is IDictionary<string, object>)
            {
                var dictionary = parameterModel as IDictionary<string, object>;
                foreach (var param in executionParameters)
                {
                    var value = param.TomParameter.GetDbValue(dictionary[param.TomParameter.FieldName]);
                    param.SqlParameter.Value = value;
                }
            }
            else
            {
                var props = parameterModel.GetType().GetProperties().ToDictionary(o => o.Name);
                foreach (var param in executionParameters)
                {
                    var value = param.TomParameter.GetDbValue(props[param.TomParameter.FieldName].GetValue(parameterModel));
                    param.SqlParameter.Value = value;
                }
            }
        }
    }
}
