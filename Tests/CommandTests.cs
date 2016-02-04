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
    public class CommandTests
    {
        private readonly SqlConnection con;

        public CommandTests()
        {
            con = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            con.Open();

            con.ExecuteAsync("truncate table dbo.Foo").Wait();
            con.ExecuteAsync("truncate table dbo.Secure").Wait();
        }

        public void Dispose()
        {
            con.Dispose();
        }

        [TestMethod]
        public async Task ExecuteInsertFoos()
        {
            var cmd = new Command<Foo>();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Foo ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 500).Select(i => new Foo
                {
                    Id = Guid.NewGuid(),
                    Int = i,
                    DateTime2 = DateTime.UtcNow,
                    DateTimeOffset = DateTimeOffset.Now,
                })
            );
        }

        [TestMethod]
        public async Task ListAllFoos()
        {
            var cmd = new Command<Foo>();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Foo ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 500).Select(i => new Foo
                {
                    Id = Guid.NewGuid(),
                    Int = i,
                    DateTime2 = DateTime.UtcNow,
                    DateTimeOffset = DateTimeOffset.Now,
                })
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Foo");

            Assert.AreEqual(500, results.Count);
        }

        [TestMethod]
        public async Task InsertEmptySecure()
        {
            var cmd = new Command<Secure>()
                .ConfigureAllFields(f => f.Secure(), f => f.Name != "Id");

            var originals = Enumerable.Range(0, 1).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
            }).OrderBy(o => o.Id).ToList();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                originals
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");
            Assert.AreEqual(originals.Count, results.Count);

            var actuals = results.OrderBy(o => o.Id).ToList();
            for (int i = 0; i < originals.Count; i++)
            {
                Assert.AreEqual(originals[i].Id, actuals[i].Id);
                Assert.AreEqual(originals[i].Bit, actuals[i].Bit);
                Assert.AreEqual(originals[i].BitNull, actuals[i].BitNull);
                Assert.AreEqual(originals[i].DateTime2, actuals[i].DateTime2);
                Assert.AreEqual(originals[i].DateTime2Null, actuals[i].DateTime2Null);
                Assert.AreEqual(originals[i].DateTimeOffset, actuals[i].DateTimeOffset);
                Assert.AreEqual(originals[i].DateTimeOffsetNull, actuals[i].DateTimeOffsetNull);
                Assert.AreEqual(originals[i].Decimal, actuals[i].Decimal);
                Assert.AreEqual(originals[i].DecimalNull, actuals[i].DecimalNull);
                Assert.AreEqual(originals[i].Float, actuals[i].Float);
                Assert.AreEqual(originals[i].FloatNull, actuals[i].FloatNull);
                Assert.AreEqual(originals[i].Guid, actuals[i].Guid);
                Assert.AreEqual(originals[i].GuidNull, actuals[i].GuidNull);
                Assert.AreEqual(originals[i].Int, actuals[i].Int);
                Assert.AreEqual(originals[i].IntNull, actuals[i].IntNull);
                Assert.AreEqual(originals[i].Nvarchar, actuals[i].Nvarchar);
            }
        }

        [TestMethod]
        public async Task ExecuteInsertEmptySecures()
        {
            var cmd = new Command<Secure>()
                .ConfigureAllFields(f => f.Secure(), f => f.Name != "Id");

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 500).Select(i => new Secure
                {
                    Id = Guid.NewGuid(),
                })
            );
        }

        [TestMethod]
        public async Task ListEmptySecures()
        {
            var cmd = new Command<Secure>()
                .ConfigureAllFields(f => f.Secure(), f => f.Name != "Id");

            var originals = Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
            }).ToList();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                originals
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");
            Assert.AreEqual(originals.Count, results.Count);
        }
    }
}
