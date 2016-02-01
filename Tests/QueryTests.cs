using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tests.Models;
using Tom;

namespace Tests
{
    [TestClass]
    public class QueryTests : IDisposable
    {
        private readonly SqlConnection sql;

        public QueryTests()
        {
            sql = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            sql.Open();
        }

        public void Dispose()
        {
            sql.Dispose();
        }

        [TestMethod]
        public async Task List()
        {
            await sql.ListAsync<Foo>("select Id, Int, Nvarchar from dbo.Foo");
        }

        [TestMethod]
        public async Task ListWithObjectFilter()
        {
            await sql.ListAsync<Foo>("select Id, Int, Nvarchar from dbo.Foo where Nvarchar = @Nvarchar",
               new { Nvarchar = "Needle" });
        }
    }
}
