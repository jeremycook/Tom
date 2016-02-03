using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tom
{
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
            IEnumerable<object> parameterModels = null,
            SqlTransaction transaction = null)
        {
            if (parameterModels != null && parameterModels.Any())
            {
                var tomCommand = new TomCommand(parameterModels.First().GetType());
                return await tomCommand.ExecuteAsync(connection, command, parameterModels, transaction);
            }
            else
            {
                var tomCommand = new TomCommand();
                return await tomCommand.ExecuteAsync(connection, command, transaction);
            }
        }
    }
}
