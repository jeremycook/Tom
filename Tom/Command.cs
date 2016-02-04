using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tom.Internal;

namespace Tom
{
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
            var field = Fields.Single(o => o.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
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
            foreach (var param in Fields.Where(filter ?? (o => true)))
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
