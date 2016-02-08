using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tests.Models;
using Tom;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class TomReadTests : IDisposable
    {
        private readonly Db db;
        private readonly List<Foo> originals;

        public TomReadTests()
        {
            db = new Db("Db");

            using (var cx = new SqlConnection(db.ConnectionString))
            {
                cx.Open();
                cx.ExecuteAsync("truncate table dbo.Foo").Wait();
            }

            originals = Enumerable.Range(0, 500).Select(i => new Foo
            {
                Id = Guid.NewGuid(),
                Int = i,
                IntNull = i,
                DateTime2 = DateTime.UtcNow,
                DateTime2Null = DateTime.UtcNow,
                DateTimeOffset = DateTimeOffset.Now,
                DateTimeOffsetNull = DateTimeOffset.Now,
                Bit = true,
                BitNull = true,
                Float = 1,
                FloatNull = 1,
                Decimal = 1,
                DecimalNull = 1,
                Guid = Guid.NewGuid(),
                GuidNull = Guid.NewGuid(),
                Nvarchar = "Created",
                Varbinary = new byte[] { 0, 1, 2, 3 },
                Fi = new Fi { Name = "I'm a fi you're a foo.", Number = 1 },
                Fees = new[]
                    {
                        new Fee { Name = "I'm a fee you're a foo.", Number = 2 },
                        new Fee { Name = "I'm another fee you're a foo.", Number = 3 }
                    },
            }).OrderBy(o => o.Id).ToList();
            db.Foos.AddRangeAsync(originals).Wait();
            db.Commit();
        }

        public void Dispose()
        {
            db.Dispose();
        }


        [TestMethod]
        public async Task Scalar()
        {
            int count = await db.ScalarAsync("select count(*) from dbo.Foo");

            Assert.AreEqual(500, count);
        }

        [TestMethod]
        public async Task ScalarWithFilter()
        {
            int count = await db.ScalarAsync("select count(*) from dbo.Foo where Id in (@Guid1, @Guid2)", new
            {
                Guid1 = originals[0].Id,
                Guid2 = originals[1].Id
            });

            Assert.AreEqual(2, count);
        }
    }
}
