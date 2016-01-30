using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Tom
{
    public static class Sequel
    {
        public static async Task<IEnumerable<TModel>> ListAsync<TModel>(this SqlConnection connection,
            string command)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = command;
                var reader = await cmd.ExecuteReaderAsync();

                var fields = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
                var map = typeof(TModel).GetProperties().ToDictionary(o => o.Name);

                var results = new List<TModel>();
                while (await reader.ReadAsync())
                {
                    var model = Activator.CreateInstance<TModel>();
                    foreach (var field in fields)
                    {
                        map[field].SetValue(model, reader[field]);
                    }
                    results.Add(model);
                }
                return results;
            }
        }

        public static async Task<IEnumerable<TModel>> ListAsync<TModel>(this SqlConnection connection,
            string command,
            object parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("paramiters");
            }

            SqlParameter[] sqlparameters = parameters.GetType().GetProperties()
                .Select(o => new SqlParameter(o.Name, value: o.GetValue(parameters)))
                .ToArray();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = command;
                cmd.Parameters.AddRange(sqlparameters);
                var reader = await cmd.ExecuteReaderAsync();

                var fields = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
                var map = typeof(TModel).GetProperties().ToDictionary(o => o.Name);

                var results = new List<TModel>();
                while (await reader.ReadAsync())
                {
                    var model = Activator.CreateInstance<TModel>();
                    foreach (var field in fields)
                    {
                        map[field].SetValue(model, reader[field]);
                    }
                    results.Add(model);
                }
                return results;
            }
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

        public static async Task<int> ExecuteAsync(this SqlConnection connection,
            string command,
            IEnumerable<object> args,
            SqlTransaction transaction = null)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            if (!args.Any())
            {
                return await connection.ExecuteAsync(command, transaction);
            }

            var tx = transaction ?? connection.BeginTransaction();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    QueryDefinition columnDefintion = QueryDefinition.Create(args.First());

                    cmd.Transaction = tx;
                    cmd.CommandText = command;
                    cmd.Parameters.AddRange(columnDefintion.Parameters);

                    int affectedRows = 0;
                    foreach (var column in columnDefintion.With(args))
                    {
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
    }
}
