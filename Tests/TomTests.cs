using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Models;
using System.Threading.Tasks;
using System.Linq;
using Tom;
using System.Data.SqlClient;

namespace Tests
{
    [TestClass]
    public class TomTests : IDisposable
    {
        private readonly Db db;

        public TomTests()
        {
            db = new Db("Db");

            using (var cx = new SqlConnection(db.ConnectionString))
            {
                cx.Open();
                cx.ExecuteAsync("truncate table dbo.Foo").Wait();
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }

        [TestMethod]
        public async Task AddFoo()
        {
            var foo = new Foo
            {
                Id = Guid.NewGuid(),
            };
            await db.Foos.AddAsync(foo);
            db.Commit();
        }

        [TestMethod]
        public async Task AddRangeFoo()
        {
            await db.Foos.AddRangeAsync(Enumerable.Range(0, 1000).Select(i => new Foo
            {
                Id = Guid.NewGuid(),
            }));
            db.Commit();
        }
    }
}
