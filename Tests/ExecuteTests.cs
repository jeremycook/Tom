using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tom;

namespace Tests
{
    [TestClass]
    public class ExecuteTests : IDisposable
    {
        private readonly SqlConnection sql;

        public ExecuteTests()
        {
            sql = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            sql.Open();

            sql.ExecuteAsync("truncate table dbo.Foo").Wait();
        }

        public void Dispose()
        {
            sql.Dispose();
        }

        [TestMethod]
        public async Task Execute()
        {
            await sql.ExecuteAsync("select * from dbo.Foo");
        }

        [TestMethod]
        public async Task ExecuteWithObjects()
        {
            await sql.ExecuteAsync(
                "insert into Foo (Guid, Int, DateTime2, DateTimeOffset, Nvarchar) values (@Guid, @Int, @DateTime2, @DateTimeOffset, @Nvarchar)",
                Enumerable.Range(0, 1000).Select(i =>
                    new
                    {
                        Guid = Guid.NewGuid(),
                        Int = i,
                        DateTime2 = DateTime.UtcNow,
                        DateTimeOffset = DateTimeOffset.Now,
                        Nvarchar = "Objected",
                    }
                )
            );
        }
    }
}
