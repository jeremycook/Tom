﻿namespace Tom
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
            SqlParameter[] sqlparameters = GetSqlParameters(query, parameterModel);

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

        public static async Task<int> ScalarAsync(this SqlConnection connection, string query, object parameterModel = null)
        {
            return await connection.ScalarAsync<int>(query, parameterModel);
        }

        public static async Task<TOut> ScalarAsync<TOut>(this SqlConnection connection, string query, object parameterModel = null)
        {
            SqlParameter[] sqlparameters = GetSqlParameters(query, parameterModel);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.Parameters.AddRange(sqlparameters);
                return (TOut)(await cmd.ExecuteScalarAsync());
            }
        }

        private static SqlParameter[] GetSqlParameters(string query, object parameterModel)
        {
            string[] inUseParameters = GetInUseParameters(query);

            SqlParameter[] sqlparameters = parameterModel == null ?
                new SqlParameter[0] :
                parameterModel.GetType().GetProperties()
                    .Where(o => inUseParameters.Contains(o.Name, StringComparer.InvariantCultureIgnoreCase))
                    .Select(o => new SqlParameter(o.Name, value: o.GetValue(parameterModel)))
                    .ToArray();

            return sqlparameters;
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
