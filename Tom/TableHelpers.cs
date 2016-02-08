using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public static class TableHelpers
    {
        /// <summary>
        /// Returns exactly one <typeparamref name="TModel"/> or 
        /// <see cref="null"/>. Throws if more than one matching <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="paramaterModel"></param>
        /// <returns></returns>
        public static async Task<TModel> SingleOrDefaultAsync<TModel>(this ITable<TModel> table, string where = null, object paramaterModel = null)
        {
            var list = await table.ListAsync(where, parameters: paramaterModel);
            return list.SingleOrDefault();
        }

        /// <summary>
        /// Query a scalar value.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="table"></param>
        /// <param name="select"></param>
        /// <param name="where"></param>
        /// <param name="paramaterModel"></param>
        /// <returns></returns>
        public static async Task<TResult> ScalarAsync<TResult>(this ITable table, string select, string where = null, object paramaterModel = null)
        {
            if (select == null)
            {
                throw new ArgumentNullException("select");
            }

            var result = await table.Tom.ScalarAsync<TResult>(
                "select " + select + " from dbo.[" + table.TableName + "]" +
                (where != null ? " where " + where : ""),
                paramaterModel);

            return result;
        }

        /// <summary>
        /// Query a scalar <see cref="int"/> value.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="table"></param>
        /// <param name="select"></param>
        /// <param name="where"></param>
        /// <param name="paramaterModel"></param>
        /// <returns></returns>
        public static async Task<int> ScalarAsync(this ITable table, string select, string where = null, object paramaterModel = null)
        {
            if (select == null)
            {
                throw new ArgumentNullException("select");
            }

            var result = await table.Tom.ScalarAsync<int>(
                "select " + select + " from dbo.[" + table.TableName + "]" +
                (where != null ? " where " + where : ""),
                paramaterModel);

            return result;
        }

        /// <summary>
        /// Returns the number of matching items.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="paramaterModel"></param>
        /// <returns></returns>
        public static async Task<int> CountAsync(this ITable table, string where = null, object paramaterModel = null)
        {
            return await table.ScalarAsync<int>("count(*)", where, paramaterModel);
        }

        /// <summary>
        /// Returns <see cref="true"/> if there are more than zero items.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="paramaterModel"></param>
        /// <returns></returns>
        public static async Task<bool> AnyAsync(this ITable table, string where = null, object paramaterModel = null)
        {
            return (await table.CountAsync(where, paramaterModel)) > 0;
        }
    }
}
