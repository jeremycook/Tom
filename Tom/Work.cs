using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Tom
{
    public class Work : IDisposable
    {
        public SqlConnection Connection { get; private set; }
        public SqlTransaction Transaction { get; private set; }

        public static async Task<Work> CreateAsync(string connectionString)
        {
            var uow = new Work();

            uow.Connection = new SqlConnection(connectionString);
            await uow.Connection.OpenAsync();
            uow.Transaction = uow.Connection.BeginTransaction();

            return uow;
        }

        public void Dispose()
        {
            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}