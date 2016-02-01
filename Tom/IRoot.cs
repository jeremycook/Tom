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
            var cx = await Tom.UseConnectionAsync();

            var mappedColumns = Columns.Where(o => o.Mapped).ToArray();
            string command = string.Format("insert into dbo.[{0}]\n({1})\nvalues ({2})",
                TableName,
                string.Join(", ", mappedColumns.Select(o => "[" + o.FieldName + "]")),
                string.Join(", ", mappedColumns.Select(o => "@" + o.FieldName))
            );

            await cx.ExecuteAsync(command, args: models as IEnumerable<object>, transaction: Tom.Transaction);
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
            var cx = await Tom.UseConnectionAsync();

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

            await cx.ExecuteAsync(command, args: models as IEnumerable<object>, transaction: Tom.Transaction);
        }

        public TomBase Tom { get; private set; }
        public Type ModelType { get; private set; }
        public string TableName { get; set; }
        public IEnumerable<Column> Columns { get; private set; }
        public IEnumerable<Column> PrimaryKey { get; set; }
    }
}