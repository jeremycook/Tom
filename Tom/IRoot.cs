using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
                .Select(p => new Column(this, p))
                .ToArray();

            var pk = Columns.First();
            PrimaryKey = new[] { pk };
            if (pk.SqlDbType == System.Data.SqlDbType.UniqueIdentifier)
            {
                pk.DefaultValue = "(newid())";
            }
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

                var mappedColumns = Columns.Where(o => o.Mapped).ToArray();
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

                var mappedColumns = Columns.Where(o => o.Mapped).ToArray();
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

            var mappedColumns = Columns.Where(o => o.Mapped).ToArray();
            string command = string.Format("insert into dbo.[{0}]\n({1})\nvalues ({2})",
                TableName,
                string.Join(", ", mappedColumns.Select(o => "[" + o.FieldName + "]")),
                string.Join(", ", mappedColumns.Select(o => "@" + o.FieldName))
            );

            await work.Connection.ExecuteAsync(command, args: models as IEnumerable<object>, transaction: work.Transaction);
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

            var mappedColumns = Columns.Except(PrimaryKey)
                .Where(o => o.Mapped)
                .ToArray();

            string command = string.Format(
@"update dbo.[{0}]
set
    {1}
where {2}",
                TableName,
                string.Join(",\n    ", mappedColumns.Select(o => "[" + o.FieldName + "] = @" + o.FieldName)),
                string.Join(", ", PrimaryKey.Select(o => "[" + o.FieldName + "] = @" + o.FieldName))
            );

            await work.Connection.ExecuteAsync(command, args: models as IEnumerable<object>, transaction: work.Transaction);
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
    }
}