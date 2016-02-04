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
    public class CommandSecureTests
    {
        private readonly Command<Secure> cmd;
        private readonly SqlConnection con;

        public CommandSecureTests()
        {
            con = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            con.Open();

            con.ExecuteAsync("truncate table dbo.Secure").Wait();
            cmd = new Command<Secure>()
                .ConfigureAllFields(c => c.Secure(), c => c.Name != "Id");
        }

        public void Dispose()
        {
            con.Dispose();
        }

        [TestMethod]
        public async Task InsertEmptySecures()
        {

            var originals = Enumerable.Range(0, 500).Select(i => new Secure
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

                // By default null is automatically converted to "" for Nvarchar fields.
                Assert.AreEqual("", actuals[i].Nvarchar);

                Assert.IsNull(actuals[i].Fi);

                Assert.IsNull(actuals[i].Fees);
            }
        }

        [TestMethod]
        public async Task InsertSecures()
        {
            var originals = Enumerable.Range(0, 500).Select(i => new Secure
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
                Fi = new SecureFi { Name = "I'm a fi you're a secure.", Number = 1 },
                Fees = new[]
                {
                    new SecureFee { Name = "I'm a fee you're a secure.", Number = 2 },
                    new SecureFee { Name = "I'm another fee you're a secure.", Number = 3 }
                },
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
                Assert.AreEqual(0, (originals[i].DateTime2 - actuals[i].DateTime2).Ticks);
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

                Assert.IsNotNull(actuals[i].Fi);
                Assert.AreEqual(originals[i].Fi.Name, actuals[i].Fi.Name);
                Assert.AreEqual(originals[i].Fi.Number, actuals[i].Fi.Number);

                Assert.IsNotNull(actuals[i].Fees);
                Assert.AreEqual(originals[i].Fees.First().Name, actuals[i].Fees.First().Name);
                Assert.AreEqual(originals[i].Fees.First().Number, actuals[i].Fees.First().Number);
                Assert.AreEqual(originals[i].Fees.Last().Name, actuals[i].Fees.Last().Name);
                Assert.AreEqual(originals[i].Fees.Last().Number, actuals[i].Fees.Last().Number);
            }
        }

        [TestMethod]
        public async Task ListAllSecures()
        {
            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 500).Select(i => new Secure
                {
                    Id = Guid.NewGuid(),
                    Int = i,
                    DateTime2 = DateTime.UtcNow,
                    DateTimeOffset = DateTimeOffset.Now,
                })
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");

            Assert.AreEqual(500, results.Count);
        }
    }
}
