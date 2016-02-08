using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tests.Models;
using Tom;

namespace Tests
{
    [TestClass]
    public class TomFooTests : IDisposable
    {
        private readonly Db db;

        public TomFooTests()
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
            await db.Foos.AddRangeAsync(Enumerable.Range(0, 500).Select(i => new Foo
            {
                Id = Guid.NewGuid(),
            }));
            db.Commit();
        }


        [TestMethod]
        public async Task UpdateFoo()
        {
            var foo = new Foo
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            };
            await db.Foos.AddAsync(foo);
            db.Commit();

            foo.Nvarchar = "Updated";
            await db.Foos.UpdateAsync(foo);
            db.Commit();
        }

        [TestMethod]
        public async Task UpdateRangeFoo()
        {
            var foos = Enumerable.Range(0, 500).Select(i => new Foo
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            }).ToList();
            await db.Foos.AddRangeAsync(foos);
            db.Commit();

            foreach (var item in foos)
            {
                item.Nvarchar = "Updated";
            }
            await db.Foos.UpdateRangeAsync(foos);
            db.Commit();
        }


        [TestMethod]
        public async Task RemoveFoo()
        {
            var foo = new Foo
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            };
            await db.Foos.AddAsync(foo);
            db.Commit();

            await db.Foos.RemoveAsync(foo);
            db.Commit();
        }

        [TestMethod]
        public async Task RemoveRangeFoo()
        {
            var foos = Enumerable.Range(0, 500).Select(i => new Foo
            {
                Id = Guid.NewGuid(),
            }).ToList();
            await db.Foos.AddRangeAsync(foos);
            db.Commit();

            await db.Foos.RemoveRangeAsync(foos);
            db.Commit();
        }
    }
}
