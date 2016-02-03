using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tom.Internal;

namespace Tom
{
    public interface ITable
    {
        Type ModelType { get; }
        string TableName { get; }
        IEnumerable<Column> Columns { get; }
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
        /// <returns></returns>
        Task<IEnumerable<TModel>> ListAsync();

        /// <summary>
        /// List a filtered set of models.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<IEnumerable<TModel>> ListAsync(string filter, object parameters);

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
        public Table(TomBase tom)
        {
            Tom = tom;
            ModelType = typeof(TModel);
            TableName = ModelType.Name;
            Columns = ModelType.GetProperties()
                .Select(p => new Column(p))
                .ToArray();
            Command = new Command<TModel>(Columns.Select(o => o.Field));

            var pk = Columns.First();
            PrimaryKey = new[] { pk };
            if (pk.Field.SqlDbType == System.Data.SqlDbType.UniqueIdentifier)
            {
                pk.DefaultFieldValue = "(newid())";
            }
        }

        public ITable<TModel> ConfigureColumn(Expression<Func<TModel, object>> selector, Action<Column> columnAction)
        {
            string name = selector.GetName();
            columnAction(Columns.Single(o => o.Field.Name == name));
            return this;
        }

        public ITable<TModel> ConfigureAllColumns(Action<Column> columnAction, Func<Column, bool> filter = null)
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

                var results = await cx.ListAsync<TModel>(string.Format(
                    "select {0} from dbo.[{1}]",
                    Command.ToFieldNamesText(),
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

                var results = await cx.ListAsync<TModel>(string.Format(
                        "select {0} from dbo.[{1}] where {2}",
                        Command.ToFieldNamesText(),
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
            var whereClause = string.Join(", ", PrimaryKey.Select(o => "[" + o.Field.Name + "] = @" + o.Field.Name));

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
            var whereClause = string.Join(", ", PrimaryKey.Select(o => "[" + o.Field.Name + "] = @" + o.Field.Name));

            var work = await Tom.WorkAsync();
            await Command.ExecuteAsync(
                work.Connection,
                string.Format(
                    "delete from dbo.{0} where {1}",
                    TableName,
                    whereClause
                ),
                models,
                work.Transaction
            );
        }

        public TomBase Tom { get; private set; }
        public Type ModelType { get; private set; }
        public string TableName { get; set; }
        public IEnumerable<Column> Columns { get; private set; }
        public IEnumerable<Column> PrimaryKey { get; set; }
        public Command<TModel> Command { get; private set; }
    }
}