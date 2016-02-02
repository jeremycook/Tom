using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tom.Internal;

namespace Tom
{
    public interface IRoot
    {
        Type ModelType { get; }
        string TableName { get; }
        IEnumerable<Column> Columns { get; }
        IEnumerable<Column> PrimaryKey { get; }
    }

    public class Root<TModel> : IRoot
    {
        public Root(TomBase tom)
        {
            Tom = tom;
            ModelType = typeof(TModel);
            TableName = ModelType.Name;
            Columns = ModelType.GetProperties()
                .Select(p => new Column(p))
                .ToArray();
            Command = new TomCommand<TModel>(Columns);

            var pk = Columns.First();
            PrimaryKey = new[] { pk };
            if (pk.SqlDbType == System.Data.SqlDbType.UniqueIdentifier)
            {
                pk.DefaultFieldValue = "(newid())";
            }
        }

        public Root<TModel> ConfigureColumn(Expression<Func<TModel, object>> selector, Action<Column> columnAction)
        {
            string name = selector.GetName();
            columnAction(Columns.Single(o => o.FieldName == name));
            return this;
        }

        public Root<TModel> ConfigureAllColumns(Action<Column> columnAction, Func<Column, bool> filter = null)
        {
            foreach (var param in Columns.Where(filter ?? (c => true)))
            {
                columnAction(param);
            }
            return this;
        }

        /// <summary>
        /// List all models.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<TModel>> ListAsync()
        {
            using (var cx = new SqlConnection(Tom.ConnectionString))
            {
                await cx.OpenAsync();

                var mappedColumns = Columns.Where(o => o.IsMapped).ToArray();
                var results = await cx.ListAsync<TModel>(string.Format(
                    "select {0} from dbo.[{1}]",
                    string.Join(", ", mappedColumns.Select(o => "[" + o.FieldName + "]")),
                    TableName
                ));

                return results;
            };
        }

        /// <summary>
        /// List a filtered set of models.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TModel>> ListAsync(string filter, object parameters)
        {
            using (var cx = new SqlConnection(Tom.ConnectionString))
            {
                await cx.OpenAsync();

                var mappedColumns = Columns.Where(o => o.IsMapped).ToArray();
                var results = await cx.ListAsync<TModel>(string.Format(
                        "select {0} from dbo.[{1}] where {2}",
                        string.Join(", ", mappedColumns.Select(o => "[" + o.FieldName + "]")),
                        TableName,
                        filter
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
            var whereClause = string.Join(", ", PrimaryKey.Select(o => "[" + o.FieldName + "] = @" + o.FieldName));

            var work = await Tom.WorkAsync();
            await Command.ExecuteAsync(
                work.Connection,
                string.Format(
                    "update dbo.{0} set {1} where {2}",
                    TableName,
                    Command.ToUpdateFieldsText(),
                    whereClause
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

            string command = string.Format("delete from dbo.[{0}] where {1}",
                TableName,
                string.Join(", ", PrimaryKey.Select(o => "[" + o.FieldName + "] = @" + o.FieldName))
            );

            await work.Connection.ExecuteAsync(command, args: models as IEnumerable<object>, transaction: work.Transaction);
        }

        public TomBase Tom { get; private set; }
        public Type ModelType { get; private set; }
        public string TableName { get; set; }
        public IEnumerable<Column> Columns { get; private set; }
        public IEnumerable<Column> PrimaryKey { get; set; }
        public TomCommand<TModel> Command { get; private set; }
    }
}