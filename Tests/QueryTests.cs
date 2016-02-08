using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
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

            sql.ExecuteAsync("truncate table dbo.Foo").Wait();
            sql.ExecuteAsync(
                "insert into Foo (Guid, Int, DateTime2, DateTimeOffset) values (@Guid, @Int, @DateTime2, @DateTimeOffset)",
                Enumerable.Range(0, 1000).Select(i =>
                    new
                    {
                        Guid = Guid.NewGuid(),
                        Int = i,
                        DateTime2 = DateTime.UtcNow,
                        DateTimeOffset = DateTimeOffset.Now,
                    }
                )
            ).Wait();
        }

        public void Dispose()
        {
            sql.Dispose();
        }

        [TestMethod]
        public async Task List()
        {
            var result = await sql.ListAsync<Foo>("select Id, Int, Nvarchar from dbo.Foo");

            Assert.AreEqual(1000, result.Count);
        }

        [TestMethod]
        public async Task ListFirstPage()
        {
            var result = await sql.ListAsync<Foo>(
                "select Id, Int, Nvarchar from dbo.Foo order by Int",
                page: 1);

            Assert.AreEqual(25, result.Count);
        }

        [TestMethod]
        public async Task ListWithObjectFilter()
        {
            var result = await sql.ListAsync<Foo>(
                "select Id, Int, Nvarchar from dbo.Foo where Int between @First and @last",
                new { First = 500, Last = 599, NotUsed = Guid.Empty });

            Assert.AreEqual(100, result.Count);
        }

        [TestMethod]
        public async Task ListWithObjectFilterFirstPage()
        {
            var result = await sql.ListAsync<Foo>(
                "select Id, Int, Nvarchar from dbo.Foo where Int between @First and @last order by Int",
                new { First = 500, Last = 599, NotUsed = Guid.Empty },
                page: 1);

            Assert.AreEqual(25, result.Count);
        }

        [TestMethod]
        public async Task Scalar()
        {
            int count = await sql.ScalarAsync("select count(*) from dbo.Foo");

            Assert.AreEqual(1000, count);
        }

        [TestMethod]
        public async Task ScalarWithFilter()
        {
            int count = await sql.ScalarAsync(
                "select count(*) from dbo.Foo where Int between @First and @last",
                new { First = 500, Last = 599, NotUsed = Guid.Empty }
            );

            Assert.AreEqual(100, count);
        }
    }
}
