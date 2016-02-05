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
    public class TomSecureReadTests : IDisposable
    {
        private readonly Db db;
        private readonly List<Secure> originals;

        public TomSecureReadTests()
        {
            db = new Db("Db");

            using (var cx = new SqlConnection(db.ConnectionString))
            {
                cx.Open();
                cx.ExecuteAsync("truncate table dbo.Secure").Wait();
            }

            originals = Enumerable.Range(0, 500).Select(i => new Secure
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
                Fi = new SecureFi { Name = "I'm a fi you're a foo.", Number = 1 },
                Fees = new[]
                {
                    new SecureFee { Name = "I'm a fee you're a foo.", Number = 2 },
                    new SecureFee { Name = "I'm another fee you're a foo.", Number = 3 }
                },
            }).OrderBy(o => o.Id).ToList();
            db.Secure.AddRangeAsync(originals).Wait();
            db.Commit();
        }

        public void Dispose()
        {
            db.Dispose();
        }


        [TestMethod]
        public async Task ListAll()
        {
            var results = await db.Secure.ListAsync();

            Assert.AreEqual(500, results.Count);
        }

        [TestMethod]
        public async Task Filter()
        {

            var results = await db.Secure.ListAsync("Id in (@Guid1, @Guid2)", new
            {
                Guid1 = originals[0].Id,
                Guid2 = originals[1].Id
            });

            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public async Task Page()
        {

            var results = await db.Secure.ListAsync(page: 2, pageSize: 100);

            Assert.AreEqual(100, results.Count);
        }
    }
}
