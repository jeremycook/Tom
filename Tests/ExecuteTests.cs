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
        public async Task ExecuteWithDictionaries()
        {
            await sql.ExecuteAsync(
                "insert into Foo (Int, Text) values (@Int, @Text)",
                Enumerable.Range(0, 1000).Select(i =>
                    new Dictionary<string, object> { { "Int", i }, { "Text", "Dictionaried" } }
                )
            );
        }

        [TestMethod]
        public async Task ExecuteWithObjects()
        {
            await sql.ExecuteAsync(
                "insert into Foo (Int, Text) values (@Int, @Text)",
                Enumerable.Range(1000, 1000).Select(i =>
                    new { Int = i, Text = "Objected" }
                )
            );
        }
    }
}
